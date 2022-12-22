/*
 * Copyright (c) 2008-2020 Bryan Biedenkapp., All Rights Reserved.
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

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;

using TridentFramework.RPC.Net.Message;
using TridentFramework.RPC.Net.PeerConnection;

using TridentFramework.RPC.Utility;

namespace TridentFramework.RPC.Net
{
    /// <summary>
    /// Defines a abstract server service.
    /// </summary>
    public abstract class INetServer : IClientServerBase
    {
        private const int MAX_SUPPORTED_CLIENTS = 128;
        private const int LOCAL_DISCOVERY_TIMEOUT = 1000;

        /// <summary>
        /// Time since epoch (1/1/1970)
        /// </summary>
        public static readonly DateTime Epoch = new DateTime(1970, 1, 1);

        private Thread processMessages;
        private bool haltMessageProcessing;
        private bool isOpened = false;

        protected int privateReservedSlots;

        protected Dictionary<long, IPEndPoint> connIpEndPoints;

        /*
        ** Properties
        */

        /// <summary>
        /// Gets the instance of the <see cref="NetServerPeer"/>.
        /// </summary>
        public NetServerPeer Peer
        {
            get { return (NetServerPeer)peer; }
        }

        /// <summary>
        /// Flag indicating whether the network connection has been opened.
        /// </summary>
        public bool IsOpened
        {
            get { return isOpened; }
        }

        /*
        ** Events
        */

        /// <summary>
        /// Occurs when a new client connects to the server.
        /// </summary>
        public event Action<object, long> OnClientConnected;

        /// <summary>
        /// Occurs when a client disconnects from the server.
        /// </summary>
        public event Action<object, long> OnClientDisconnected;

        /// <summary>
        /// Occurs when the server receives client network traffic.
        /// </summary>
        public event Action<object, long> OnReceiveClientTraffic;

        /// <summary>
        /// Occurs when a raw byte-array network message is received.
        /// </summary>
        public event Action<object, long, byte[]> OnRawNetworkData;

        /// <summary>
        /// Occurs when a user-defined network message is received.
        /// </summary>
        public event Action<object, long, IncomingMessage> OnUserDefinedNetworkData;

        /// <summary>
        /// Occurs when the client completes Diffie-Hellman negotiation.
        /// </summary>
        public event EventHandler OnClientSecured;

        /// <summary>
        /// Occurs during ListenMessages loop.
        /// </summary>
        protected event EventHandler OnListenTick;

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="INetServer"/> class.
        /// </summary>
        /// <param name="peerConfiguration">Network Peer Configuration</param>
        public INetServer(PeerConfiguration peerConfiguration) : base(peerConfiguration)
        {
            this.connIpEndPoints = new Dictionary<long, IPEndPoint>();

            this.peer = new NetServerPeer(netConfig);
            this.peer.OnClientAccepted += Network_OnClientAccepted;
        }

        /// <summary>
        /// Occurs when a new connection is accepted.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="connId"></param>
        private void Network_OnClientAccepted(object sender, long connId)
        {
            // get the end point information
            Connection netConn = Peer.GetConnection(connId);
            connIpEndPoints.Add(connId, netConn.RemoteEndpoint);

            RPCLogger.Trace("*** client connection " + connId.ToString() + " accepted");
        }

        /// <inheritdoc />
        protected override void OnDispose()
        {
            if (isOpened)
                Close();

            // Close the network
            if (peer != null)
            {
                peer.Shutdown();
                peer = null;
            }

            this.connIpEndPoints.Clear();
            this.connIpEndPoints = null;
        }

        /// <inheritdoc />
        public override void Open(string threadName = "")
        {
            if (isOpened)
                throw new InvalidOperationException("Network server is already opened");

            // start networking
            this.Peer.Start();

            if (processMessages == null)
            {
                processMessages = new Thread(ListenMessages);
                processMessages.Name = "INetServer-ListenMessages" + ((threadName != string.Empty) ? "-" + threadName : string.Empty);
                processMessages.IsBackground = true;
                haltMessageProcessing = false;
                processMessages.Start();
            }

            isOpened = true;
        }

        /// <inheritdoc />
        public override void Close()
        {
            if (!isOpened)
                throw new InvalidOperationException("Network server is already closed");

            try
            {
                if (processMessages != null)
                {
                    while (processMessages.IsAlive)
                    {
                        haltMessageProcessing = true;
                        processMessages.Join();
                        processMessages.Abort();
                    }
                    processMessages = null;
                }
            }
            catch (Exception) { }

            // if the network isn't null, try to disconnect
            if (Peer != null)
                Peer.Shutdown();

            isOpened = false;
        }

        /// <summary>
        /// Internal function to listen for server-side raw network messages.
        /// </summary>
        private void ListenMessages()
        {
            do
            {
                IncomingMessage msg;
                while ((msg = Peer.WaitMessage(1)) != null)
                {
                    switch (msg.MessageType)
                    {
                        // Handle: Network status changed
                        case IncomingMessageType.StatusChanged:
                            {
                                ConnectionStatus status = (ConnectionStatus)msg.ReadByte();
                                switch (status)
                                {
                                    // Handle: Connection established
                                    case ConnectionStatus.Connected:
                                    case ConnectionStatus.RespondedConnect:
                                        break;

                                    // Handle: Secured
                                    case ConnectionStatus.ConnectedSecured:
                                        if (OnClientSecured != null)
                                            OnClientSecured(this, new EventArgs());
                                        break;

                                    // Handle: Disconnected network status
                                    case ConnectionStatus.Disconnected:
                                        {
                                            IPEndPoint lostConnectionIpEndPoint = msg.SenderEndpoint;
                                            RPCLogger.Trace("handling connections disconnection [" + lostConnectionIpEndPoint.ToString() + "]");

                                            // build list of connections to remove
                                            List<long> clientsToRemove = new List<long>(1);
                                            foreach (KeyValuePair<long, IPEndPoint> connIpEndPoint in connIpEndPoints)
                                                if (connIpEndPoint.Value.Equals(lostConnectionIpEndPoint))
                                                {
                                                    clientsToRemove.Add(connIpEndPoint.Key);

                                                    if (OnClientDisconnected != null)
                                                        OnClientDisconnected(this, connIpEndPoint.Key);
                                                }

                                            // iterate through list and remove connections
                                            foreach (long connId in clientsToRemove)
                                            {
                                                // make sure underlying connection is removed
                                                Connection netConn = Peer.GetConnection(connId);
                                                if (netConn != null)
                                                {
                                                    if (netConn.Status != ConnectionStatus.Disconnected)
                                                        netConn.Shutdown("Connection disconnect");
                                                }

                                                // remove connection from our lists
                                                connIpEndPoints.Remove(connId);
                                            }
                                            break;
                                        }

                                    default:
                                        {
                                            RPCLogger.Trace("unhandled status change: " + status + " " + msg.LengthBytes + " bytes");
                                            break;
                                        }
                                }
                                break;
                            }

                        // Handle: Raw incoming message data
                        case IncomingMessageType.Data:
                            {
                                MessageToTransmit mtt = msg.ReadMessageToTransmit();

                                // get the connection ID
                                long connId = 0L;
                                try
                                {
                                    connId = msg.ReadInt64();
                                }
                                catch (Exception)
                                {
                                    // stub
                                }

                                if (connId == 0)
                                    RPCLogger.WriteWarning("incoming packet [" + msg.ToString() + "] contained an empty client identifier!");

                                // only fire on receive traffic even if this isn't a CLIENT_CONNECTED packet
                                if (mtt != MessageToTransmit.CLIENT_CONNECTED)
                                    if (OnReceiveClientTraffic != null)
                                        OnReceiveClientTraffic(this, connId);

                                // handle message to transmit type
                                switch (mtt)
                                {
                                    case MessageToTransmit.CLIENT_CONNECTED:
                                        {
                                            // prepare a message to send the current local client to the server
                                            OutgoingMessage outMsg = Peer.CreateMessage();
                                            outMsg.MessageType = MessageType.UserReliableOrdered1;
                                            outMsg.Write(MessageToTransmit.CLIENT_CONNECTED);

                                            // pack the connection GUID
                                            outMsg.Write(msg.SenderConnection.RemoteUniqueId);

                                            // transmit message
                                            Peer.SendMessageTo(outMsg, msg.SenderConnection.RemoteUniqueId, DeliveryMethod.ReliableOrdered);
                                            if (!Peer.Configuration.AutoFlushSendQueue)
                                                Peer.FlushSendQueue();

                                            if (OnClientConnected != null)
                                                OnClientConnected(this, connId);
                                            break;
                                        }

                                    case MessageToTransmit.RAW_BYTES:
                                        {
                                            byte[] raw = ProcessRawBytes(msg);
                                            if (OnRawNetworkData != null)
                                                OnRawNetworkData(this, connId, raw);
                                            break;
                                        }

                                    case MessageToTransmit.USER_DEFINED:
                                        {
                                            if (OnUserDefinedNetworkData != null)
                                                OnUserDefinedNetworkData(this, connId, msg);
                                            break;
                                        }

                                    default:
                                        break;
                                }
                                break;
                            }

                        // Handle: Network session discovery request
                        case IncomingMessageType.DiscoveryRequest:
                            {
                                OutgoingMessage outMsg = Peer.CreateMessage();

                                outMsg.WriteVariableInt32(connIpEndPoints.Count);
                                outMsg.WriteVariableInt32(privateReservedSlots);
                                outMsg.WriteVariableInt32(MAX_SUPPORTED_CLIENTS - connIpEndPoints.Count - privateReservedSlots);

                                Peer.SendDiscoveryResponse(outMsg, msg.SenderEndpoint);
                                if (!Peer.Configuration.AutoFlushSendQueue)
                                    Peer.FlushSendQueue();
                                break;
                            }

                        case IncomingMessageType.TestMessage:
                            RPCLogger.Trace("network test message received");
                            break;

                        default:
                            {
                                RPCLogger.Trace("unhandled type: " + msg.MessageType + " " + msg.LengthBytes + " bytes");
                                break;
                            }
                    }
                }

                if (OnListenTick != null)
                    OnListenTick(this, new EventArgs());
            } while (!haltMessageProcessing);
        }
    } // public abstract class INetServer : IClientServerBase
} // namespace TridentFramework.RPC.Net
