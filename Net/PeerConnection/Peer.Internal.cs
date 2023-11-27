/**
 * Copyright (c) 2008-2023 Bryan Biedenkapp., All Rights Reserved.
 * MIT Open Source. Use is subject to license terms.
 * DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
 */
/*
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including 
 * without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject 
 * to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN 
 * NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
/*
 * Based on code from Lidgren Network v3
 * Copyright (C) 2010 Michael Lidgren., All Rights Reserved.
 * Licensed under MIT License.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;

using TridentFramework.RPC.Net.Message;

using TridentFramework.Cryptography.DiffieHellman;
using TridentFramework.RPC.Utility;

namespace TridentFramework.RPC.Net.PeerConnection
{
    /// <summary>
    /// Represents a local peer capable of holding zero, one or more connections to remote peers
    /// </summary>
    public partial class Peer
    {
        public const int PACKET_MAGIC_VERSION = 0x03E8;         // version 1.000

        private readonly ThreadSafeQueue<IncomingMessage> releasedIncomingMessages;
        internal readonly ThreadSafeQueue<Tuple<IPEndPoint, OutgoingMessage>> unsentUnconnectedMessages;

        private Thread networkThread;

        internal byte[] sendBuffer;
        internal byte[] receiveBuffer;

        private IncomingMessage readHelperMessage;
        private EndPoint senderRemote;
        private object initializeLock = new object();
        private uint frameCounter;
        private double lastHeartbeat;
        private double lastSocketBind = float.MinValue;

        private bool needFlushSendQueue;
        private bool executeFlushSendQueue;

        internal Dictionary<IPEndPoint, Connection> handshakes;

        private object messageReceivedEventCreationLock = new object();
        private AutoResetEvent messageReceivedEvent;
        private List<Tuple<SynchronizationContext, SendOrPostCallback>> receiveCallbacks;

        /*
        ** Properties
        */

        /// <summary>
        /// Gets the socket, if Start() has been called
        /// </summary>
        public Socket Socket
        {
            get;
            private set;
        }

        /// <summary>
        /// Flag indicating wether queued packets should be flushed.
        /// </summary>
        public bool ExecuteFlushSendQueue
        {
            get { return executeFlushSendQueue; }
        }

        /// <summary>
        /// Flag indicating wether we need to flush the send queue.
        /// </summary>
        public bool NeedFlushSendQueue
        {
            get { return needFlushSendQueue; }
            internal set { needFlushSendQueue = value; }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Call this to register a callback for when a new message arrives
        /// </summary>
        public void RegisterReceivedCallback(SendOrPostCallback callback, SynchronizationContext syncContext = null)
        {
            if (syncContext == null)
                syncContext = SynchronizationContext.Current;
            if (syncContext == null)
                throw new NetworkException("Need a SynchronizationContext to register callback on correct thread!");
            if (receiveCallbacks == null)
                receiveCallbacks = new List<Tuple<SynchronizationContext, SendOrPostCallback>>();

            receiveCallbacks.Add(new Tuple<SynchronizationContext, SendOrPostCallback>(syncContext, callback));
        }

        /// <summary>
        /// Call this to unregister a callback, but remember to do it in the same synchronization context!
        /// </summary>
        public void UnregisterReceivedCallback(SendOrPostCallback callback)
        {
            if (receiveCallbacks == null)
                return;

            // remove all callbacks regardless of sync context
            receiveCallbacks.RemoveAll(tuple => tuple.Item2.Equals(callback));

            if (receiveCallbacks.Count < 1)
                receiveCallbacks = null;
        }

        /// <summary>
        /// Release message.
        /// </summary>
        /// <param name="msg">Message to release</param>
        internal void ReleaseMessage(IncomingMessage msg)
        {
            NetworkException.Assert(msg.MessageType != IncomingMessageType.Error);

            if (msg.IsFragment)
            {
                HandleReleasedFragment(msg);
                return;
            }

            // queue released message
            releasedIncomingMessages.Enqueue(msg);

            if (messageReceivedEvent != null)
                messageReceivedEvent.Set();

            if (receiveCallbacks != null)
            {
                foreach (var tuple in receiveCallbacks)
                {
                    try
                    {
                        tuple.Item1.Post(tuple.Item2, this);
                    }
                    catch (Exception ex)
                    {
                        RPCLogger.WriteWarning("Receive callback exception:" + ex);
                    }
                }
            }
        }

        /// <summary>
        /// Helper to bind sockets.
        /// </summary>
        /// <param name="reBind"></param>
        private void BindSocket(bool reBind)
        {
            double now = NetTime.Now;
            if (now - lastSocketBind < 1.0)
            {
                RPCLogger.Trace("Suppressed socket rebind; last bound " + (now - lastSocketBind) + " seconds ago");
                return; // only allow rebind once every second
            }
            lastSocketBind = now;

            if (Socket == null)
                Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            if (reBind)
                Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, (int)1);

            Socket.ReceiveBufferSize = Configuration.ReceiveBufferSize;
            Socket.SendBufferSize = Configuration.SendBufferSize;
            Socket.Blocking = false;

            EndPoint ep = (EndPoint)new IPEndPoint(Configuration.LocalAddress, reBind ? Port : Configuration.Port);
            Socket.Bind(ep);

            try
            {
                const uint IOC_IN = 0x80000000;
                const uint IOC_VENDOR = 0x18000000;
                uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;
                Socket.IOControl((int)SIO_UDP_CONNRESET, new byte[] { Convert.ToByte(false) }, null);
            }
            catch
            {
                // ignore; SIO_UDP_CONNRESET not supported on this platform
            }

            var boundEp = Socket.LocalEndPoint as IPEndPoint;
            RPCLogger.Trace("Socket bound to " + boundEp + ": " + Socket.IsBound);
            Port = boundEp.Port;
        }

        /// <summary>
        /// Initialize network layer.
        /// </summary>
        private void InitializeNetwork()
        {
            lock (initializeLock)
            {
                Configuration.Lock();

                if (Status == PeerStatus.Running)
                    return;

                if (Configuration.EnableUPnP)
                    this.UPnP = new UPnP(this);

                InitializePools();

                releasedIncomingMessages.Clear();
                unsentUnconnectedMessages.Clear();
                handshakes.Clear();

                // bind to socket
                BindSocket(false);

                IPEndPoint boundEp = Socket.LocalEndPoint as IPEndPoint;
                SendTestMessage("Version: " + PACKET_MAGIC_VERSION + " - Socket bound to " + boundEp + ": " + Socket.IsBound);

                receiveBuffer = new byte[Configuration.ReceiveBufferSize];
                sendBuffer = new byte[Configuration.SendBufferSize];
                readHelperMessage = new IncomingMessage(IncomingMessageType.Error);
                readHelperMessage.Data = receiveBuffer;

                byte[] macBytes = new byte[8];
                NetUtility.NextBytes(macBytes);

                try
                {
                    PhysicalAddress pa = NetUtility.GetMACAddress();
                    if (pa != null)
                    {
                        macBytes = pa.GetAddressBytes();
                        RPCLogger.Trace("MAC address is " + NetUtility.ToHexString(macBytes));
                    }
                    else
                        RPCLogger.WriteWarning("Failed to get MAC address");
                }
                catch (NotSupportedException)
                {
                    // not supported; lets just keep the random bytes set above
                }

                byte[] epBytes = BitConverter.GetBytes(boundEp.GetHashCode());
                byte[] combined = new byte[epBytes.Length + macBytes.Length];
                Array.Copy(epBytes, 0, combined, 0, epBytes.Length);
                Array.Copy(macBytes, 0, combined, epBytes.Length, macBytes.Length);
                uniqueId = Math.Abs(BitConverter.ToInt64(NetUtility.ComputeSHAHash(combined), 0));

                Status = PeerStatus.Running;
            }
        }

        /// <summary>
        /// Send test message over wire.
        /// </summary>
        /// <param name="message"></param>
        internal void SendTestMessage(string message)
        {
            if (Configuration.IsMessageTypeEnabled(IncomingMessageType.TestMessage))
                ReleaseMessage(CreateIncomingMessage(IncomingMessageType.TestMessage, message));
        }

        /// <summary>
        /// NetworkLoop Thread Proc
        /// </summary>
        private void NetworkLoop()
        {
            VerifyNetworkThread();

            RPCLogger.Trace("Network thread started");

            // Network loop
            do
            {
                try
                {
                    Heartbeat();
                }
                catch (Exception ex)
                {
                    RPCLogger.Trace("Heartbeat triggered an exception");
                    RPCLogger.StackTrace(ex, false);
                }
            }
            while (Status == PeerStatus.Running);

            // perform shutdown
            ExecutePeerShutdown();
        }

        /// <summary>
        /// Execute a shutdown of this peer
        /// </summary>
        private void ExecutePeerShutdown()
        {
            VerifyNetworkThread();

            RPCLogger.Trace("Shutting down...");

            // disconnect and make one final heartbeat
            var list = new List<Connection>(handshakes.Count + connections.Count);
            lock (connections)
            {
                foreach (var conn in connections)
                    if (conn != null)
                        list.Add(conn);
            }

            lock (handshakes)
            {
                foreach (var hs in handshakes.Values)
                    if (hs != null)
                        list.Add(hs);
            }

            // shut down connections
            foreach (Connection conn in list)
                conn.Shutdown(shutdownReason);

            try
            {
                // one final heartbeat, will send stuff and do disconnect
                Heartbeat();
            }
            catch (NetworkException)
            {
                /* ignore exceptions */
            }

            Thread.Sleep(10);

            lock (initializeLock)
            {
                try
                {
                    if (Socket != null)
                    {
                        try
                        {
                            Socket.Shutdown(SocketShutdown.Receive);
                        }
                        catch (Exception ex)
                        {
                            RPCLogger.WriteError("Failed while shutting down socket!");
                            RPCLogger.StackTrace(ex, false);
                        }

                        try
                        {
                            Socket.Close(2); // 2 seconds timeout
                        }
                        catch (Exception ex)
                        {
                            RPCLogger.WriteError("Failed while closing socket!");
                            RPCLogger.StackTrace(ex, false);
                        }
                    }
                }
                finally
                {
                    Socket = null;
                    Status = PeerStatus.NotRunning;
                    RPCLogger.Trace("Shutdown complete");

                    if (messageReceivedEvent != null)
                        messageReceivedEvent.Set();
                }

                lastSocketBind = float.MinValue;
                receiveBuffer = null;
                sendBuffer = null;
                unsentUnconnectedMessages.Clear();
                connections.Clear();
                connectionLookup.Clear();
                uniqueIdLookup.Clear();
                handshakes.Clear();
            }

            return;
        }

        /// <summary>
        /// Network heartbeat function
        /// </summary>
        private void Heartbeat()
        {
            VerifyNetworkThread();

            double dnow = NetTime.Now;
            float now = (float)dnow;

            double delta = dnow - lastHeartbeat;

            int maxCHBpS = 1250 - connections.Count;
            if (maxCHBpS < 250)
                maxCHBpS = 250;

            // max connection heartbeats/second max
            if (delta > (1.0 / (double)maxCHBpS) || delta < 0.0)
            {
                frameCounter++;
                lastHeartbeat = dnow;

                // do handshake heartbeats
                if ((frameCounter % 3) == 0)
                {
                    foreach (var kvp in handshakes)
                    {
                        Connection conn = kvp.Value as Connection;
#if DEBUG
                        // sanity check
                        if (kvp.Key != kvp.Key)
                            RPCLogger.Trace("Sanity fail! Connection in handshake list under wrong key!");
#endif
                        conn.UnconnectedHeartbeat(now);
                        if (conn.Status == ConnectionStatus.Connected || conn.Status == ConnectionStatus.Disconnected ||
                            conn.Status == ConnectionStatus.ConnectedSecured)
                        {
#if DEBUG
                            // sanity check
                            if (conn.Status == ConnectionStatus.Disconnected && handshakes.ContainsKey(conn.RemoteEndpoint))
                            {
                                RPCLogger.Trace("Sanity fail! Handshakes list contained disconnected connection!");
                                handshakes.Remove(conn.RemoteEndpoint);
                            }
#endif
                            break; // collection has been modified
                        }
                    }
                }

                // update executeFlushSendQueue
                if (Configuration.AutoFlushSendQueue && needFlushSendQueue == true)
                {
                    executeFlushSendQueue = true;
                    needFlushSendQueue = false; // a race condition to this variable will simply result in a single superfluous call to FlushSendQueue()
                }

                // do connection heartbeats
                lock (connections)
                {
                    for (int i = connections.Count - 1; i >= 0; i--)
                    {
                        Connection conn = connections[i];
                        conn.Heartbeat(now, frameCounter);
                        if (conn.Status == ConnectionStatus.Disconnected)
                        {
                            // remove connection
                            connections.Remove(conn);
                            connectionLookup.Remove(conn.RemoteEndpoint);
                            uniqueIdLookup.Remove(conn.RemoteUniqueId);

                            if (OnClientDisconnected != null)
                                OnClientDisconnected(this, conn.RemoteUniqueId);
                        }
                    }
                }

                executeFlushSendQueue = false;

                // send unsent unconnected messages
                Tuple<IPEndPoint, OutgoingMessage> unsent;
                while (unsentUnconnectedMessages.TryDequeue(out unsent))
                {
                    OutgoingMessage om = unsent.Item2;
                    RPCLogger.Trace("UC Outgoing Message: " + om.ToString() + " " + om.Data.Length + " bytes");

                    int len = om.Encode(sendBuffer, 0, 0);

                    Interlocked.Decrement(ref om.recyclingCount);
                    if (om.recyclingCount <= 0)
                        Recycle(om);

                    bool connReset;
                    SendPacket(len, unsent.Item1, 1, out connReset);
                }
            }

            if (UPnP != null)
                UPnP.CheckForDiscoveryTimeout();

            // read from socket
            if (Socket == null)
                return;

            if (!Socket.Poll(1000, SelectMode.SelectRead)) // wait up to 1 ms for data to arrive
                return;

            do
            {
                int bytesReceived = 0;
                try
                {
                    bytesReceived = Socket.ReceiveFrom(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, ref senderRemote);
                }
                catch (SocketException sx)
                {
                    if (sx.SocketErrorCode == SocketError.ConnectionReset)
                    {
                        // connection reset by peer, aka connection forcibly closed aka "ICMP port unreachable"
                        // we should shut down the connection; but senderRemote seemingly cannot be trusted, so which connection should we shut down?!
                        // So, what to do?
                        return;
                    }

                    RPCLogger.WriteError("SocketException caught while trying to receive");
                    RPCLogger.StackTrace(sx, false);
                    return;
                }

                if (bytesReceived < NetUtility.HeaderByteSize)
                    return;

                IPEndPoint ipsender = (IPEndPoint)senderRemote;

                if (UPnP != null && now < UPnP.discoveryResponseDeadline && bytesReceived > 32)
                {
                    // is this an UPnP response?
                    string resp = System.Text.Encoding.UTF8.GetString(receiveBuffer, 0, bytesReceived);
                    if (resp.Contains("upnp:rootdevice") || resp.Contains("UPnP/1.0"))
                    {
                        try
                        {
                            resp = resp.Substring(resp.ToLower().IndexOf("location:") + 9);
                            resp = resp.Substring(0, resp.IndexOf("\r")).Trim();
                            UPnP.ExtractServiceUrl(resp);
                            return;
                        }
                        catch (Exception ex)
                        {
                            RPCLogger.Trace("Failed to parse UPnP response: " + ex.ToString());

                            // don't try to parse this packet further
                            return;
                        }
                    }
                }

                Connection sender = null;
                connectionLookup.TryGetValue(ipsender, out sender);

                double receiveTime = NetTime.Now;

                // parse packet into messages
                int numMessages = 0;
                int numFragements = 0;
                int ptr = 0;
                while ((bytesReceived - ptr) >= NetUtility.HeaderByteSize)
                {
                    // decode header
                    //  8 bits - NetMessageType
                    //  1 bit  - Fragment?
                    // 15 bits - Sequence number
                    // 16 bits - Payload length in bits

                    numMessages++;

                    MessageType tp = (MessageType)receiveBuffer[ptr++];

                    byte low = receiveBuffer[ptr++];
                    byte high = receiveBuffer[ptr++];

                    bool isFragment = (low & 1) == 1;
                    ushort sequenceNumber = (ushort)((low >> 1) | (((int)high) << 7));

                    if (isFragment)
                        numFragements++;

                    ushort payloadBitLength = (ushort)(receiveBuffer[ptr++] | (receiveBuffer[ptr++] << 8));
                    int payloadByteLength = NetUtility.BytesToHoldBits(payloadBitLength);

                    if (bytesReceived - ptr < payloadByteLength)
                    {
                        RPCLogger.WriteWarning("Malformed packet; stated payload length " + payloadByteLength + ", remaining bytes " + (bytesReceived - ptr));
                        return;
                    }

                    try
                    {
                        RPCLogger.Trace("Incoming Message: " + tp.ToString() + " " + bytesReceived + " bytes");
                        if (tp >= MessageType.InternalError)
                        {
                            if (sender != null)
                                sender.ReceivedInternalMessage(tp, ptr, payloadByteLength);
                            else
                                ReceivedUnconnectedInternalMessage(receiveTime, ipsender, tp, ptr, payloadByteLength);
                        }
                        else
                        {
                            if (sender == null && !Configuration.IsMessageTypeEnabled(IncomingMessageType.UnconnectedData))
                                return; // dropping unconnected message since it's not enabled

                            IncomingMessage msg = CreateIncomingMessage(IncomingMessageType.Data, payloadByteLength);
                            msg.IsFragment = isFragment;
                            msg.ReceiveTime = receiveTime;
                            msg.SequenceNumber = sequenceNumber;
                            msg.ReceivedMessageType = tp;
                            msg.SenderConnection = sender;
                            msg.SenderEndpoint = ipsender;
                            msg.BitLength = payloadBitLength;

                            Buffer.BlockCopy(receiveBuffer, ptr, msg.Data, 0, payloadByteLength);
                            if (sender != null)
                            {
                                if (tp == MessageType.Unconnected)
                                {
                                    // We're connected; but we can still send unconnected messages to this peer
                                    msg.MessageType = IncomingMessageType.UnconnectedData;
                                    ReleaseMessage(msg);
                                }
                                else
                                {
                                    // connected application (non-library) message
                                    sender.ReceivedMessage(msg);
                                }
                            }
                            else
                            {
                                // at this point we know the message type is enabled
                                // unconnected application (non-library) message
                                msg.MessageType = IncomingMessageType.UnconnectedData;
                                ReleaseMessage(msg);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        RPCLogger.WriteError("Packet parsing error from " + ipsender);
                        RPCLogger.StackTrace(ex, false);
                    }
                    ptr += payloadByteLength;
                }

                Statistics.PacketReceived(bytesReceived, numMessages, numFragements);
                if (sender != null)
                    sender.Statistics.PacketReceived(bytesReceived, numMessages, numFragements);
            }
            while (Socket.Available > 0);
        }

        /// <summary>
        /// If PeerConfiguration.AutoFlushSendQueue is false; you need to call
        /// this to send all messages queued using SendMessage()
        /// </summary>
        public void FlushSendQueue()
        {
            executeFlushSendQueue = true;
        }

        /// <summary>
        /// Handle incoming discovery request.
        /// </summary>
        /// <param name="now"></param>
        /// <param name="senderEndpoint">IP Endpoint</param>
        /// <param name="ptr"></param>
        /// <param name="payloadByteLength"></param>
        internal void HandleIncomingDiscoveryRequest(double now, IPEndPoint senderEndpoint, int ptr, int payloadByteLength)
        {
            if (Configuration.IsMessageTypeEnabled(IncomingMessageType.DiscoveryRequest))
            {
                IncomingMessage dm = CreateIncomingMessage(IncomingMessageType.DiscoveryRequest, payloadByteLength);
                if (payloadByteLength > 0)
                    Buffer.BlockCopy(receiveBuffer, ptr, dm.Data, 0, payloadByteLength);
                dm.ReceiveTime = now;
                dm.BitLength = payloadByteLength * 8;
                dm.SenderEndpoint = senderEndpoint;
                ReleaseMessage(dm);
            }
        }

        /// <summary>
        /// Handle incoming discovery response.
        /// </summary>
        /// <param name="now"></param>
        /// <param name="senderEndpoint"></param>
        /// <param name="ptr"></param>
        /// <param name="payloadByteLength"></param>
        internal void HandleIncomingDiscoveryResponse(double now, IPEndPoint senderEndpoint, int ptr, int payloadByteLength)
        {
            if (Configuration.IsMessageTypeEnabled(IncomingMessageType.DiscoveryResponse))
            {
                IncomingMessage dr = CreateIncomingMessage(IncomingMessageType.DiscoveryResponse, payloadByteLength);
                if (payloadByteLength > 0)
                    Buffer.BlockCopy(receiveBuffer, ptr, dr.Data, 0, payloadByteLength);
                dr.ReceiveTime = now;
                dr.BitLength = payloadByteLength * 8;
                dr.SenderEndpoint = senderEndpoint;
                ReleaseMessage(dr);
            }
        }

        /// <summary>
        /// Receive an unconnected internal message.
        /// </summary>
        /// <param name="now"></param>
        /// <param name="senderEndpoint">IP Endpoint</param>
        /// <param name="tp">Message Type</param>
        /// <param name="ptr"></param>
        /// <param name="payloadByteLength"></param>
        private void ReceivedUnconnectedInternalMessage(double now, IPEndPoint senderEndpoint, MessageType tp, int ptr, int payloadByteLength)
        {
            Connection shake;
            if (handshakes.TryGetValue(senderEndpoint, out shake))
            {
                shake.ReceivedHandshake(now, tp, ptr, payloadByteLength);
                return;
            }

            //
            // Library message from a completely unknown sender; lets just accept Connect
            //
            switch (tp)
            {
                case MessageType.Discovery:
                    HandleIncomingDiscoveryRequest(now, senderEndpoint, ptr, payloadByteLength);
                    return;

                case MessageType.DiscoveryResponse:
                    HandleIncomingDiscoveryResponse(now, senderEndpoint, ptr, payloadByteLength);
                    return;
                /*
                case MessageType.NatIntroduction:
                    HandleNatIntroduction(ptr);
                    return;

                case MessageType.NatPunchMessage:
                    HandleNatPunch(ptr, senderEndpoint);
                    return;
                */

                case MessageType.ConnectResponse:

                    lock (handshakes)
                    {
                        foreach (var hs in handshakes)
                        {
                            if (hs.Key.Address.Equals(senderEndpoint.Address))
                            {
                                if (hs.Value.ConnectionInitiator)
                                {
                                    //
                                    // We are currently trying to connection to XX.XX.XX.XX:Y
                                    // ... but we just received a ConnectResponse from XX.XX.XX.XX:Z
                                    // Lets just assume the router decided to use this port instead
                                    //
                                    var hsconn = hs.Value;
                                    connectionLookup.Remove(hs.Key);
                                    uniqueIdLookup.Remove(hsconn.RemoteUniqueId);
                                    handshakes.Remove(hs.Key);

                                    RPCLogger.Trace("Detected host port change; rerouting connection to " + senderEndpoint);
                                    hsconn.MutateEndpoint(senderEndpoint);

                                    connectionLookup.Add(senderEndpoint, hsconn);
                                    uniqueIdLookup.Add(hsconn.RemoteUniqueId, hsconn);
                                    handshakes.Add(senderEndpoint, hsconn);

                                    hsconn.ReceivedHandshake(now, tp, ptr, payloadByteLength);
                                    return;
                                }
                            }
                        }
                    }

                    RPCLogger.WriteWarning("Received unhandled internal message " + tp + " from " + senderEndpoint);
                    return;

                case MessageType.Connect:
                    if (Configuration.AcceptIncomingConnections == false)
                    {
                        RPCLogger.WriteWarning("Received Connect, but we're not accepting incoming connections!");
                        return;
                    }

                    // it's someone wanting to shake hands with us!
                    int reservedSlots = handshakes.Count + connections.Count;
                    if (reservedSlots >= Configuration.MaximumConnections)
                    {
                        // server full
                        OutgoingMessage full = CreateMessage("Server full");
                        full.MessageType = MessageType.Disconnect;
                        SendInternal(full, senderEndpoint);
                        return;
                    }

                    // Ok, start handshake!
                    Connection conn = new Connection(this, senderEndpoint);
                    handshakes.Add(senderEndpoint, conn);
                    conn.ReceivedHandshake(now, tp, ptr, payloadByteLength);
                    return;

                case MessageType.Disconnect:
                    // this is probably ok
                    RPCLogger.Trace("Received Disconnect from unconnected source: " + senderEndpoint);
                    return;

                default:
                    RPCLogger.WriteWarning("Received unhandled library message " + tp + " from " + senderEndpoint);
                    return;
            }
        }

        /// <summary>
        /// Accept an incoming connection.
        /// </summary>
        /// <param name="conn">Network connection</param>
        internal void AcceptConnection(Connection conn)
        {
            // LogDebug("Accepted connection " + conn);
            conn.InitExpandMTU(NetTime.Now);

            if (handshakes.Remove(conn.RemoteEndpoint) == false)
                RPCLogger.WriteWarning("AcceptConnection called but handshakes did not contain it!");

            lock (connections)
            {
                if (connections.Contains(conn))
                    RPCLogger.WriteWarning("AcceptConnection called but connection already contains it!");
                else
                {
                    connections.Add(conn);
                    connectionLookup.Add(conn.RemoteEndpoint, conn);
                    uniqueIdLookup.Add(conn.RemoteUniqueId, conn);

                    if (Configuration.NegotiateEncryption)
                    {
#if DH_DEBUG_TRACE
                        Messages.WriteWarning("Diffie-Hellman DEBUG TRACE is on ... security isn't so secure anymore...");
#endif
                        // send diffie-hellman parameter exchange
                        byte[] dhP = dhParams.P.ToByteArray();
                        byte[] dhG = dhParams.G.ToByteArray();
                        byte[] dhPub = Peer.GetPublicKey(dhKP);
#if DH_DEBUG_TRACE
                        Messages.Trace("server dhP   : " + DiffieHellmanParameters.P.ToString());
                        Messages.Trace("server dhG   : " + DiffieHellmanParameters.G.ToString());
                        Messages.Trace("server dhPub : " + new BigInteger(dhPub).ToString());
#endif
                        OutgoingMessage om = CreateMessage(0);
                        om.MessageType = MessageType.DiffieHellmanRequest;
                        om.Write(dhP, true);
                        om.Write(dhG, true);
                        om.Write(dhPub, true);
                        SendInternal(om, conn.RemoteEndpoint);
                    }

                    if (OnClientAccepted != null)
                        OnClientAccepted(this, conn.RemoteUniqueId);
                }
            }
        }

        /// <summary>
        /// Verify the network thread is healthy and running
        /// </summary>
        [Conditional("DEBUG")]
        internal void VerifyNetworkThread()
        {
            Thread ct = Thread.CurrentThread;
            if (Thread.CurrentThread != networkThread)
                throw new NetworkException("Executing on wrong thread! Should be library system thread (is " + ct.Name + " mId " + ct.ManagedThreadId + ")");
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="ptr"></param>
        /// <param name="payloadLength"></param>
        /// <returns></returns>
        internal IncomingMessage SetupReadHelperMessage(int ptr, int payloadLength)
        {
            VerifyNetworkThread();

            readHelperMessage.BitLength = (ptr + payloadLength) * 8;
            readHelperMessage.readPosition = ptr * 8;
            return readHelperMessage;
        }
    } // public partial class Peer
} // namespace TridentFramework.RPC.Net.PeerConnection
