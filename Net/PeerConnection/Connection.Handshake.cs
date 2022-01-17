/**
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
/*
 * Based on code from Lidgren Network v3
 * Copyright (C) 2010 Michael Lidgren., All Rights Reserved.
 * Licensed under MIT License.
 */

using System;

using TridentFramework.RPC.Net.Channel;
using TridentFramework.RPC.Net.Message;

using TridentFramework.RPC.Utility;

namespace TridentFramework.RPC.Net.PeerConnection
{
    /// <summary>
    /// Represents a connection to a remote peer
    /// </summary>
    public partial class Connection
    {
        private string disconnectMessage;
        private bool disconnectReqSendBye;

        private float lastHandshakeSendTime;
        private int handshakeAttempts;

        /*
        ** Properties
        */

        /// <summary>
        /// Gets the message that the remote part specified via Connect() or Approve() - can be null.
        /// </summary>
        public IncomingMessage RemoteHailMessage
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a value indicating whether or not a connect was requested
        /// </summary>
        public bool ConnectRequested
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets a value indicating whether or not we're the connection initiator
        /// </summary>
        public bool ConnectionInitiator
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets a value indicating whether or not a disconnect was requested
        /// </summary>
        public bool DisconnectRequested
        {
            get;
            internal set;
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Heartbeat called, when connection is still handshaking
        /// </summary>
        /// <param name="now"></param>
        internal void UnconnectedHeartbeat(float now)
        {
            Peer.VerifyNetworkThread();

            if (DisconnectRequested)
                ExecuteDisconnect(disconnectMessage, true);

            if (ConnectRequested)
            {
                switch (status)
                {
                    case ConnectionStatus.Connected:
                    case ConnectionStatus.RespondedConnect:
                        // reconnect
                        ExecuteDisconnect("Reconnecting", true);
                        break;

                    case ConnectionStatus.InitiatedConnect:
                        // send another connect attempt
                        SendConnect(now);
                        break;

                    case ConnectionStatus.Disconnected:
                        throw new NetworkException("This connection is Disconnected; spent. A new one should have been created");

                    case ConnectionStatus.Disconnecting:
                        // let disconnect finish first
                        break;

                    case ConnectionStatus.None:
                    default:
                        SendConnect(now);
                        break;
                }
                return;
            }

            if (now - lastHandshakeSendTime > Peer.Configuration.ResendHandshakeInterval)
            {
                if (handshakeAttempts >= Peer.Configuration.MaximumHandshakeAttempts)
                {
                    // failed to connect
                    ExecuteDisconnect("Failed to establish connection - no response from remote host", true);
                    return;
                }

                // resend handshake
                switch (status)
                {
                    case ConnectionStatus.InitiatedConnect:
                        SendConnect(now);
                        break;

                    case ConnectionStatus.RespondedConnect:
                        SendConnectResponse(now, true);
                        break;

                    case ConnectionStatus.None:
                        if (Peer.Configuration.IsMessageTypeEnabled(IncomingMessageType.ConnectionApproval))
                            break; // we're probably waiting for connection approval here
                        RPCLogger.Trace("Time to resend handshake, but status is " + status);
                        break;

                    default:
                        RPCLogger.Trace("Time to resend handshake, but status is " + status);
                        break;
                }
            }
        }

        /// <summary>
        /// Execute a disconnect from the remote peer
        /// </summary>
        /// <param name="reason">Reason for disconnect</param>
        /// <param name="sendByeMessage"></param>
        internal void ExecuteDisconnect(string reason, bool sendByeMessage)
        {
            Peer.VerifyNetworkThread();

            // clear send queues
            for (int i = 0; i < sendChannels.Length; i++)
            {
                ISenderChannel channel = sendChannels[i];
                if (channel != null)
                    channel.Reset();
            }

            RPCLogger.Trace("Disconnect being executed: " + reason);

            if (sendByeMessage)
                SendDisconnect(reason, true);

            SetStatus(ConnectionStatus.Disconnected, reason);

            // in case we're still in handshake
            lock (Peer.handshakes)
                Peer.handshakes.Remove(RemoteEndpoint);

            DisconnectRequested = false;
            ConnectRequested = false;
            handshakeAttempts = 0;
        }

        /// <summary>
        /// Send a connect request.
        /// </summary>
        /// <param name="now">Time message was sent</param>
        internal void SendConnect(float now)
        {
            Peer.VerifyNetworkThread();

            OutgoingMessage om = Peer.CreateMessage(Peer.Configuration.AppIdentifier.Length + 1 + 4);
            om.MessageType = MessageType.Connect;
            om.Write(Peer.Configuration.AppIdentifier);
            om.Write(Peer.UniqueId);
            om.Write(now);

            WriteLocalHail(om);

            Peer.SendInternal(om, RemoteEndpoint);

            ConnectRequested = false;
            lastHandshakeSendTime = now;
            handshakeAttempts++;

            if (handshakeAttempts > 1)
                RPCLogger.Trace("Resending Connect...");
            SetStatus(ConnectionStatus.InitiatedConnect, "Locally requested connect");
        }

        /// <summary>
        /// Send a connection request response.
        /// </summary>
        /// <param name="now">Time message was sent</param>
        /// <param name="onLibraryThread"></param>
        internal void SendConnectResponse(float now, bool onLibraryThread)
        {
            if (onLibraryThread)
                Peer.VerifyNetworkThread();

            OutgoingMessage om = Peer.CreateMessage(Peer.Configuration.AppIdentifier.Length + 1 + 4);
            om.MessageType = MessageType.ConnectResponse;
            om.Write(Peer.Configuration.AppIdentifier);
            om.Write(Peer.UniqueId);
            om.Write(now);

            WriteLocalHail(om);

            if (onLibraryThread)
                Peer.SendInternal(om, RemoteEndpoint);
            else
                Peer.unsentUnconnectedMessages.Enqueue(new Tuple<System.Net.IPEndPoint, OutgoingMessage>(RemoteEndpoint, om));

            lastHandshakeSendTime = now;
            handshakeAttempts++;

            if (handshakeAttempts > 1)
                RPCLogger.Trace("Resending ConnectResponse...");

            SetStatus(ConnectionStatus.RespondedConnect, "Remotely requested connect");
        }

        /// <summary>
        /// Send a disconnect request
        /// </summary>
        /// <param name="reason">Reason for disconnect</param>
        /// <param name="onLibraryThread"></param>
        internal void SendDisconnect(string reason, bool onLibraryThread)
        {
            if (onLibraryThread)
                Peer.VerifyNetworkThread();

            OutgoingMessage om = Peer.CreateMessage(reason);
            om.MessageType = MessageType.Disconnect;
            if (onLibraryThread)
                Peer.SendInternal(om, RemoteEndpoint);
            else
                Peer.unsentUnconnectedMessages.Enqueue(new Tuple<System.Net.IPEndPoint, OutgoingMessage>(RemoteEndpoint, om));
        }

        /// <summary>
        /// Write hail message
        /// </summary>
        /// <param name="om">Outgoing Message</param>
        private void WriteLocalHail(OutgoingMessage om)
        {
            if (LocalHailMessage != null)
            {
                byte[] hi = LocalHailMessage.PeekDataBuffer();
                if (hi != null && hi.Length >= LocalHailMessage.LengthBytes)
                {
                    if (om.LengthBytes + LocalHailMessage.LengthBytes > Peer.Configuration.MaximumTransmissionUnit - 10)
                        throw new NetworkException("Hail message too large; can maximally be " + (Peer.Configuration.MaximumTransmissionUnit - 10 - om.LengthBytes));
                    om.Write(LocalHailMessage.PeekDataBuffer(), 0, LocalHailMessage.LengthBytes);
                }
            }
        }

        /// <summary>
        /// Send a connection established message
        /// </summary>
        internal void SendConnectionEstablished()
        {
            OutgoingMessage om = Peer.CreateMessage(0);
            om.MessageType = MessageType.ConnectionEstablished;
            om.Write((float)NetTime.Now);
            Peer.SendInternal(om, RemoteEndpoint);

            handshakeAttempts = 0;

            InitializePing();
            if (status != ConnectionStatus.Connected)
                SetStatus(ConnectionStatus.Connected, "Connected to " + RemoteUniqueId);
        }

        /// <summary>
        /// Approves this connection; sending a connection response to the remote host
        /// </summary>
        public void Approve()
        {
            LocalHailMessage = null;
            handshakeAttempts = 0;
            SendConnectResponse((float)NetTime.Now, false);
        }

        /// <summary>
        /// Approves this connection; sending a connection response to the remote host
        /// </summary>
        /// <param name="localHail">The local hail message that will be set as RemoteHailMessage on the remote host</param>
        public void Approve(OutgoingMessage localHail)
        {
            LocalHailMessage = localHail;
            handshakeAttempts = 0;
            SendConnectResponse((float)NetTime.Now, false);
        }

        /// <summary>
        /// Denies this connection; disconnecting it
        /// </summary>
        public void Deny()
        {
            Deny(string.Empty);
        }

        /// <summary>
        /// Denies this connection; disconnecting it
        /// </summary>
        /// <param name="reason">The stated reason for the disconnect, readable as a string in the StatusChanged message on the remote host</param>
        public void Deny(string reason)
        {
            // send disconnect; remove from handshakes
            SendDisconnect(reason, false);

            // remove from handshakes
            Peer.handshakes.Remove(RemoteEndpoint); // TODO: make this more thread safe? we're on user thread
        }

        /// <summary>
        /// Handle received handshake.
        /// </summary>
        /// <param name="now"></param>
        /// <param name="tp">Message Type</param>
        /// <param name="ptr"></param>
        /// <param name="payloadLength"></param>
        internal void ReceivedHandshake(double now, MessageType tp, int ptr, int payloadLength)
        {
            Peer.VerifyNetworkThread();

            byte[] hail;
            switch (tp)
            {
                case MessageType.Connect:
                    if (status == ConnectionStatus.None)
                    {
                        // Whee! Server full has already been checked
                        bool ok = ValidateHandshakeData(ptr, payloadLength, out hail);
                        if (ok)
                        {
                            if (hail != null)
                            {
                                RemoteHailMessage = Peer.CreateIncomingMessage(IncomingMessageType.Data, hail);
                                RemoteHailMessage.BitLength = hail.Length * 8;
                            }
                            else
                                RemoteHailMessage = null;

                            if (Peer.Configuration.IsMessageTypeEnabled(IncomingMessageType.ConnectionApproval))
                            {
                                // ok, let's not add connection just yet
                                IncomingMessage appMsg = Peer.CreateIncomingMessage(IncomingMessageType.ConnectionApproval, (RemoteHailMessage == null ? 0 : RemoteHailMessage.LengthBytes));
                                appMsg.ReceiveTime = now;
                                appMsg.SenderConnection = this;
                                appMsg.SenderEndpoint = this.RemoteEndpoint;
                                if (RemoteHailMessage != null)
                                    appMsg.Write(RemoteHailMessage.Data, 0, RemoteHailMessage.LengthBytes);
                                Peer.ReleaseMessage(appMsg);
                                return;
                            }

                            SendConnectResponse((float)now, true);
                        }
                        return;
                    }
                    if (status == ConnectionStatus.RespondedConnect)
                    {
                        // our ConnectResponse must have been lost
                        SendConnectResponse((float)now, true);
                        return;
                    }
                    RPCLogger.Trace("Unhandled Connect: " + tp + ", status is " + status + " length: " + payloadLength);
                    break;

                case MessageType.ConnectResponse:
                    HandleConnectResponse(now, tp, ptr, payloadLength);
                    break;

                case MessageType.ConnectionEstablished:
                    switch (status)
                    {
                        case ConnectionStatus.Connected:
                        case ConnectionStatus.ConnectedSecured:
                            // ok...
                            break;

                        case ConnectionStatus.Disconnected:
                        case ConnectionStatus.Disconnecting:
                        case ConnectionStatus.None:
                            // too bad, almost made it
                            break;

                        case ConnectionStatus.InitiatedConnect:
                            // weird, should have been ConnectResponse...
                            break;

                        case ConnectionStatus.RespondedConnect:
                            // awesome

                            IncomingMessage msg = Peer.SetupReadHelperMessage(ptr, payloadLength);
                            InitializeRemoteTimeOffset(msg.ReadSingle());

                            Peer.AcceptConnection(this);
                            InitializePing();
                            SetStatus(ConnectionStatus.Connected, "Connected to " + RemoteUniqueId);
                            return;
                    }
                    break;

                case MessageType.Disconnect:
                    // ouch
                    string reason = "Ouch";
                    try
                    {
                        IncomingMessage inc = Peer.SetupReadHelperMessage(ptr, payloadLength);
                        reason = inc.ReadString();
                    }
                    catch
                    {
                        // stub
                    }
                    ExecuteDisconnect(reason, false);
                    break;

                case MessageType.Discovery:
                    Peer.HandleIncomingDiscoveryRequest(now, RemoteEndpoint, ptr, payloadLength);
                    return;

                case MessageType.DiscoveryResponse:
                    Peer.HandleIncomingDiscoveryResponse(now, RemoteEndpoint, ptr, payloadLength);
                    return;

                case MessageType.DiffieHellmanRequest:
                case MessageType.Ping:
                    // silently ignore
                    return;

                default:
                    RPCLogger.Trace("Unhandled type during handshake: " + tp + " length: " + payloadLength);
                    break;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="now"></param>
        /// <param name="tp"></param>
        /// <param name="ptr"></param>
        /// <param name="payloadLength"></param>
        private void HandleConnectResponse(double now, MessageType tp, int ptr, int payloadLength)
        {
            byte[] hail;
            switch (status)
            {
                case ConnectionStatus.InitiatedConnect:
                    // awesome
                    bool ok = ValidateHandshakeData(ptr, payloadLength, out hail);
                    if (ok)
                    {
                        if (hail != null)
                        {
                            RemoteHailMessage = Peer.CreateIncomingMessage(IncomingMessageType.Data, hail);
                            RemoteHailMessage.BitLength = (hail.Length * 8);
                        }
                        else
                            RemoteHailMessage = null;

                        Peer.AcceptConnection(this);
                        SendConnectionEstablished();
                        return;
                    }
                    break;

                case ConnectionStatus.RespondedConnect:
                    // hello, wtf?
                    break;

                case ConnectionStatus.Disconnecting:
                case ConnectionStatus.Disconnected:
                case ConnectionStatus.None:
                    // wtf? anyway, bye!
                    break;

                case ConnectionStatus.Connected:
                    // my ConnectionEstablished must have been lost, send another one
                    SendConnectionEstablished();
                    return;
            }
        }

        /// <summary>
        /// Validate incoming handshake data
        /// </summary>
        /// <param name="ptr"></param>
        /// <param name="payloadLength"></param>
        /// <param name="hail"></param>
        /// <returns></returns>
        private bool ValidateHandshakeData(int ptr, int payloadLength, out byte[] hail)
        {
            hail = null;

            RPCLogger.Trace("Validating Handshake data...");

            // create temporary incoming message
            IncomingMessage msg = Peer.SetupReadHelperMessage(ptr, payloadLength);
            try
            {
                string remoteAppIdentifier = msg.ReadString();
                remoteUniqueId = msg.ReadInt64();

                InitializeRemoteTimeOffset(msg.ReadSingle());

                int remainingBytes = payloadLength - (msg.PositionInBytes - ptr);
                if (remainingBytes > 0)
                    hail = msg.ReadBytes(remainingBytes);

                if (remoteAppIdentifier != Peer.Configuration.AppIdentifier)
                {
                    // wrong app identifier
                    RPCLogger.Trace("Got " + remoteAppIdentifier + " supposed to be " + Peer.Configuration.AppIdentifier + " for " + NetUtility.LongToGuid(remoteUniqueId).ToString());
                    ExecuteDisconnect("Wrong application identifier!", true);
                    return false;
                }
            }
            catch (Exception ex)
            {
                // whatever; we failed
                ExecuteDisconnect("Handshake data validation failed", true);
                RPCLogger.WriteError("ReadRemoteHandshakeData failed!");
                RPCLogger.StackTrace(ex, false);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Disconnect from the remote peer
        /// </summary>
        /// <param name="byeMessage">the message to send with the disconnect message</param>
        public void Disconnect(string byeMessage)
        {
            // user or library thread
            if (status == ConnectionStatus.None || status == ConnectionStatus.Disconnected)
                return;

            RPCLogger.Trace("Disconnect requested for " + this);
            disconnectMessage = byeMessage;

            if (status != ConnectionStatus.Disconnected && status != ConnectionStatus.None)
                SetStatus(ConnectionStatus.Disconnecting, byeMessage);

            handshakeAttempts = 0;
            DisconnectRequested = true;
            disconnectReqSendBye = true;
        }
    } // public partial class Connection
} // namespace TridentFramework.RPC.Net.PeerConnection
