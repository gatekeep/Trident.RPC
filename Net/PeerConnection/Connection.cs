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
/*
 * Based on code from Lidgren Network v3
 * Copyright (C) 2010 Michael Lidgren., All Rights Reserved.
 * Licensed under MIT License.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;

using TridentFramework.RPC.Net.Channel;
using TridentFramework.RPC.Net.Message;

using TridentFramework.Cryptography.DiffieHellman;
using TridentFramework.Cryptography.DiffieHellman.Agreement;
using TridentFramework.Cryptography.DiffieHellman.Parameters;
using TridentFramework.RPC.Utility;

namespace TridentFramework.RPC.Net.PeerConnection
{
    /// <summary>
    /// Represents a connection to a remote peer
    /// </summary>
    [DebuggerDisplay("RemoteUniqueIdentifier={RemoteUniqueIdentifier} RemoteEndpoint={RemoteEndpoint}")]
    public partial class Connection
    {
        private ConnectionStatus status;

        private ISenderChannel[] sendChannels;
        private IReceiverChannel[] receiveChannels;

        internal ThreadSafeQueue<Tuple<MessageType, int>> queuedOutgoingAcks;
        internal ThreadSafeQueue<Tuple<MessageType, int>> queuedIncomingAcks;

        private long remoteUniqueId;

        private int sendBufferWritePtr;
        private int sendBufferNumMessages;

        private bool completedDhHandshake = false;
        private DHParameters importDhParams;
        private DHPublicKeyParameters peerPublic;
        private AsymmetricCipherKeyPair dhKP;
        private BigInteger agreementKey;

        /*
        ** Properties
        */

        /// <summary>
        /// Gets the sender channels.
        /// </summary>
        public ISenderChannel[] SendChannels
        {
            get { return sendChannels; }
        }

        /// <summary>
        /// Gets the receiver channels.
        /// </summary>
        public IReceiverChannel[] ReceiveChannels
        {
            get { return receiveChannels; }
        }

        /// <summary>
        /// Gets or sets the application defined object containing data about the connection
        /// </summary>
        public object Tag
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the peer which holds this connection
        /// </summary>
        public Peer Peer
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the current status of the connection (synced to the last status message read)
        /// </summary>
        public ConnectionStatus Status
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets various statistics for this connection
        /// </summary>
        public ConnectionStatistics Statistics
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the remote endpoint for the connection
        /// </summary>
        public IPEndPoint RemoteEndpoint
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the unique identifier of the remote peer for this connection
        /// </summary>
        public long RemoteUniqueId
        {
            get { return remoteUniqueId; }
        }

        /// <summary>
        /// Gets the unique identifier (GUID format) of the remote peer for this connection
        /// </summary>
        public Guid RemoteGuid
        {
            get { return NetUtility.LongToGuid(remoteUniqueId); }
        }

        /// <summary>
        /// Gets the local hail message that was sent as part of the handshake
        /// </summary>
        public OutgoingMessage LocalHailMessage
        {
            get;
            internal set;
        }

        /// <summary>
        /// Flag indicating wether or not this connection completed the Diffie-Hellman handshake.
        /// </summary>
        public bool CompletedDiffieHellmanHandshake
        {
            get { return completedDhHandshake; }
        }

        /// <summary>
        /// Gets the generated Diffie-Hellman parameters.
        /// </summary>
        public DHParameters DiffieHellmanParameters
        {
            get { return importDhParams; }
        }

        /// <summary>
        /// Gets this connection peer's public key.
        /// </summary>
        public DHPublicKeyParameters PeerPublicKey
        {
            get { return peerPublic; }
        }

        /// <summary>
        /// Gets the generated asymmetric cipher keypair.
        /// </summary>
        public AsymmetricCipherKeyPair CipherKeyPair
        {
            get { return dhKP; }
        }

        /// <summary>
        /// Gets the Diffie-Hellman negotiated agreement key.
        /// </summary>
        public BigInteger AgreementKey
        {
            get { return agreementKey; }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the Connection class.
        /// </summary>
        /// <param name="peer">Network peer that owns this connection</param>
        /// <param name="remoteEndpoint">Remote end point</param>
        internal Connection(Peer peer, IPEndPoint remoteEndpoint)
        {
            this.Peer = peer;

            status = ConnectionStatus.None;
            this.Status = ConnectionStatus.None;
            this.RemoteEndpoint = remoteEndpoint;
            this.remoteUniqueId = 0L;

            sendChannels = new ISenderChannel[NetUtility.NumTotalChannels];
            receiveChannels = new IReceiverChannel[NetUtility.NumTotalChannels];

            queuedOutgoingAcks = new ThreadSafeQueue<Tuple<MessageType, int>>(4);
            queuedIncomingAcks = new ThreadSafeQueue<Tuple<MessageType, int>>(4);

            Statistics = new ConnectionStatistics(this);

            AverageRoundTripTime = -1.0f;
            currentMTU = Peer.Configuration.MaximumTransmissionUnit;
        }

        /// <summary>
        /// Gets the time before automatically resending an unacked message.
        /// </summary>
        /// <returns></returns>
        internal float GetResendDelay()
        {
            float avgRtt = AverageRoundTripTime;
            if (avgRtt <= 0)
                avgRtt = 0.1f; // "default" resend is based on 100 ms roundtrip time
            return 0.02f + (avgRtt * 2.0f); // 20 ms + double rtt
        }

        /// <summary>
        /// Change the internal endpoint to this new one. Used when, during handshake, a switch in port is detected (due to NAT)
        /// </summary>
        internal void MutateEndpoint(IPEndPoint endpoint)
        {
            RemoteEndpoint = endpoint;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="now"></param>
        internal void ResetTimeout(double now)
        {
            timeoutDeadline = now + Peer.Configuration.ConnectionTimeout;
        }

        /// <summary>
        /// Change the internal connection status.
        /// </summary>
        /// <param name="status">New stauts</param>
        /// <param name="reason">Reason for change</param>
        private void SetStatus(ConnectionStatus status, string reason)
        {
            if (this.status == status)
                return;

            this.status = status;
            if (reason == null)
                reason = string.Empty;

            if ((status == ConnectionStatus.Connected) || (status == ConnectionStatus.ConnectedSecured))
            {
                timeoutDeadline = (float)NetTime.Now + Peer.Configuration.ConnectionTimeout;
                //Messages.Trace("Timeout deadline initialized to  " + timeoutDeadline);
            }

            if (Peer.Configuration.IsMessageTypeEnabled(IncomingMessageType.StatusChanged))
            {
                IncomingMessage info = Peer.CreateIncomingMessage(IncomingMessageType.StatusChanged, 4 +
                    reason.Length + (reason.Length > 126 ? 2 : 1));
                info.SenderConnection = this;
                info.SenderEndpoint = RemoteEndpoint;
                info.Write((byte)status);
                info.Write(reason);
                Peer.ReleaseMessage(info);
            }
            else
            {
                // app dont want those messages, update visible status immediately
                this.Status = status;
            }
        }

        /// <summary>
        /// Get a heartbeat from the peer to ensure they are alive.
        /// </summary>
        /// <param name="now"></param>
        /// <param name="frameCounter"></param>
        internal void Heartbeat(float now, uint frameCounter)
        {
            Peer.VerifyNetworkThread();
            NetworkException.Assert(status != ConnectionStatus.InitiatedConnect &&
                status != ConnectionStatus.RespondedConnect);

            if ((frameCounter % 5) == 0)
            {
                if (now > timeoutDeadline)
                {
                    // connection timed out
                    RPCLogger.Trace("Connection timed out at " + now + " deadline was " + timeoutDeadline);
                    ExecuteDisconnect("Connection timed out", true);
                }

                // send ping?
                if ((status == ConnectionStatus.Connected) || (status == ConnectionStatus.ConnectedSecured))
                {
                    if (now > sentPingTime + Peer.Configuration.PingInterval)
                        SendPing();

                    // handle expand mtu
                    MTUExpansionHeartbeat(now);
                }

                if (DisconnectRequested)
                {
                    ExecuteDisconnect(disconnectMessage, disconnectReqSendBye);
                    return;
                }
            }

            bool connectionReset; // TODO: handle connection reset

            //
            // Note: at this point sendBufferWritePtr and sendBufferNumMessages may be non-null; resends may already be queued up
            //

            byte[] sendBuffer = Peer.sendBuffer;
            int mtu = currentMTU;

            // coalesce a few frames
            if ((frameCounter % 3) == 0)
            {
                // send ack messages
                while (queuedOutgoingAcks.Count > 0)
                {
                    int acks = (mtu - (sendBufferWritePtr + 5)) / 3; // 3 bytes per actual ack
                    if (acks > queuedOutgoingAcks.Count)
                        acks = queuedOutgoingAcks.Count;

                    NetworkException.Assert(acks > 0);

                    sendBufferNumMessages++;

                    // write acks header
                    sendBuffer[sendBufferWritePtr++] = (byte)MessageType.Acknowledge;
                    sendBuffer[sendBufferWritePtr++] = 0; // no sequence number
                    sendBuffer[sendBufferWritePtr++] = 0; // no sequence number
                    int len = (acks * 3) * 8; // bits
                    sendBuffer[sendBufferWritePtr++] = (byte)len;
                    sendBuffer[sendBufferWritePtr++] = (byte)(len >> 8);

                    // write acks
                    for (int i = 0; i < acks; i++)
                    {
                        Tuple<MessageType, int> tuple;
                        queuedOutgoingAcks.TryDequeue(out tuple);

                        //peer.LogVerbose("Sending ack for " + tuple.Item1 + "#" + tuple.Item2);

                        sendBuffer[sendBufferWritePtr++] = (byte)tuple.Item1;
                        sendBuffer[sendBufferWritePtr++] = (byte)tuple.Item2;
                        sendBuffer[sendBufferWritePtr++] = (byte)(tuple.Item2 >> 8);
                    }

                    if (queuedOutgoingAcks.Count > 0)
                    {
                        // send packet and go for another round of acks
                        NetworkException.Assert(sendBufferWritePtr > 0 && sendBufferNumMessages > 0);
                        Peer.SendPacket(sendBufferWritePtr, RemoteEndpoint, sendBufferNumMessages, out connectionReset);
                        Statistics.PacketSent(sendBufferWritePtr, 1);
                        sendBufferWritePtr = 0;
                        sendBufferNumMessages = 0;
                    }
                }

                //
                // Parse incoming acks (may trigger resends)
                //
                Tuple<MessageType, int> incAck;
                while (queuedIncomingAcks.TryDequeue(out incAck))
                {
                    //peer.LogVerbose("Received ack for " + acktp + "#" + seqNr);
                    ISenderChannel chan = sendChannels[(int)incAck.Item1 - 1];

                    // If we haven't sent a message on this channel there is no reason to ack it
                    if (chan == null)
                        continue;

                    chan.ReceiveAcknowledge(now, incAck.Item2);
                }
            }

            // send queued messages
            if (Peer.ExecuteFlushSendQueue)
            {
                // reverse order so reliable messages are sent first
                for (int i = sendChannels.Length - 1; i >= 0; i--)
                {
                    var channel = sendChannels[i];
                    NetworkException.Assert(sendBufferWritePtr < 1 || sendBufferNumMessages > 0);
                    if (channel != null)
                    {
                        channel.SendQueuedMessages(now);
                        if (channel.NeedToSendMessages())
                            Peer.NeedFlushSendQueue = true; // failed to send all queued sends; likely a full window - need to try again
                    }
                    NetworkException.Assert(sendBufferWritePtr < 1 || sendBufferNumMessages > 0);
                }
            }

            // Put on wire data has been written to send buffer but not yet sent
            if (sendBufferWritePtr > 0)
            {
                Peer.VerifyNetworkThread();
                NetworkException.Assert(sendBufferWritePtr > 0 && sendBufferNumMessages > 0);
                Peer.SendPacket(sendBufferWritePtr, RemoteEndpoint, sendBufferNumMessages, out connectionReset);
                Statistics.PacketSent(sendBufferWritePtr, sendBufferNumMessages);
                sendBufferWritePtr = 0;
                sendBufferNumMessages = 0;
            }
        }

        /// <summary>
        /// Queue an item for immediate sending on the wire.
        /// </summary>
        /// <param name="om">Message to queue</param>
        /// <param name="seqNr">Sequence Number</param>
        internal void QueueSendMessage(OutgoingMessage om, int seqNr)
        {
            Peer.VerifyNetworkThread();

            int sz = om.GetEncodedSize();

            RPCLogger.Trace("Outgoing Message: " + om.ToString() + " " + sz + " bytes");

            bool connReset; // TODO: handle connection reset

            // can fit this message together with previously written to buffer?
            if (sendBufferWritePtr + sz > currentMTU)
            {
                if (sendBufferWritePtr > 0 && sendBufferNumMessages > 0)
                {
                    // previous message in buffer; send these first
                    Peer.SendPacket(sendBufferWritePtr, RemoteEndpoint, sendBufferNumMessages, out connReset);
                    Statistics.PacketSent(sendBufferWritePtr, sendBufferNumMessages);
                    sendBufferWritePtr = 0;
                    sendBufferNumMessages = 0;
                }
            }

            // encode it into buffer regardless if it (now) fits within MTU or not
            sendBufferWritePtr = om.Encode(Peer.sendBuffer, sendBufferWritePtr, seqNr);
            sendBufferNumMessages++;

            if (sendBufferWritePtr > currentMTU)
            {
                // send immediately; we're already over MTU
                Peer.SendPacket(sendBufferWritePtr, RemoteEndpoint, sendBufferNumMessages, out connReset);
                Statistics.PacketSent(sendBufferWritePtr, sendBufferNumMessages);
                sendBufferWritePtr = 0;
                sendBufferNumMessages = 0;
            }

            if (sendBufferWritePtr > 0)
                Peer.NeedFlushSendQueue = true; // flush in heartbeat

            Interlocked.Decrement(ref om.recyclingCount);
        }

        /// <summary>
        /// Send a message to this remote connection
        /// </summary>
        /// <param name="msg">The message to send</param>
        /// <param name="method">How to deliver the message</param>
        /// <param name="sequenceChannel">Sequence channel within the delivery method</param>
        /// <returns></returns>
        public SendResult SendMessage(OutgoingMessage msg, DeliveryMethod method, int sequenceChannel)
        {
            return Peer.SendMessage(msg, this, method, sequenceChannel);
        }

        /// <summary>
        /// Called internally by SendMessage()
        /// </summary>
        /// <param name="msg">Message to enqueue</param>
        /// <param name="method">Delivery Method</param>
        /// <param name="sequenceChannel"></param>
        /// <returns></returns>
        internal SendResult EnqueueMessage(OutgoingMessage msg, DeliveryMethod method, int sequenceChannel)
        {
            MessageType tp = (MessageType)((int)method + sequenceChannel);
            msg.MessageType = tp;

            int channelSlot = (int)method - 1 + sequenceChannel;
            ISenderChannel chan = sendChannels[channelSlot];
            if (chan == null)
                chan = CreateSenderChannel(tp);

            if (msg.GetEncodedSize() > currentMTU)
                throw new NetworkException("Message too large! Fragmentation failure?");

            return chan.Enqueue(msg);
        }

        /// <summary>
        /// Creates a channel to send a message
        /// </summary>
        /// <param name="tp">Message Type</param>
        /// <returns>Sender channel</returns>
        private ISenderChannel CreateSenderChannel(MessageType tp)
        {
            ISenderChannel chan;
            lock (sendChannels)
            {
                DeliveryMethod method = NetUtility.GetDeliveryMethod(tp);
                int sequenceChannel = (int)tp - (int)method;

                int channelSlot = (int)method - 1 + sequenceChannel;
                if (sendChannels[channelSlot] != null)
                {
                    // we were pre-empted by another call to this method
                    chan = sendChannels[channelSlot];
                }
                else
                {
                    switch (method)
                    {
                        case DeliveryMethod.Unreliable:
                        case DeliveryMethod.UnreliableSequenced:
                            chan = new UnreliableSenderChannel(this, NetUtility.GetWindowSize(method), method);
                            break;

                        case DeliveryMethod.ReliableOrdered:
                            chan = new ReliableSenderChannel(this, NetUtility.GetWindowSize(method));
                            break;

                        case DeliveryMethod.ReliableSequenced:
                        case DeliveryMethod.ReliableUnordered:
                        default:
                            chan = new ReliableSenderChannel(this, NetUtility.GetWindowSize(method));
                            break;
                    }
                    sendChannels[channelSlot] = chan;
                }
            }

            return chan;
        }

        /// <summary>
        /// Received an internal message while connected.
        /// </summary>
        /// <param name="tp">Message Type</param>
        /// <param name="ptr"></param>
        /// <param name="payloadLength"></param>
        internal void ReceivedInternalMessage(MessageType tp, int ptr, int payloadLength)
        {
            Peer.VerifyNetworkThread();

            float now = (float)NetTime.Now;

            switch (tp)
            {
                case MessageType.Connect:
                    //peer.LogDebug("Received handshake message (" + tp + ") despite connection being in place");
                    break;

                case MessageType.ConnectResponse:
                    // handshake message must have been lost
                    HandleConnectResponse(now, tp, ptr, payloadLength);
                    break;

                case MessageType.ConnectionEstablished:
                    // do nothing, all's well
                    break;

                case MessageType.DiffieHellmanRequest:
                    {
                        if (Peer.Configuration.AcceptIncomingConnections)
                        {
                            RPCLogger.WriteWarning("Received DH request, but we're accepting incoming connections?");
                            return;
                        }

                        RPCLogger.Trace("Initiating DH handshake");

                        IncomingMessage msg = Peer.SetupReadHelperMessage(ptr, payloadLength);
                        byte[] dhP = msg.ReadBytes();
                        byte[] dhG = msg.ReadBytes();
                        byte[] dhPub = msg.ReadBytes();
#if DH_DEBUG_TRACE
                        Messages.Trace("conn rx dhP   : " + new BigInteger(dhP).ToString());
                        Messages.Trace("conn rx dhG   : " + new BigInteger(dhG).ToString());
                        Messages.Trace("conn rx dhPub : " + new BigInteger(dhPub).ToString());
#endif
                        // setup diffie-hellman parameters
                        importDhParams = new DHParameters(new BigInteger(dhP), new BigInteger(dhG));
                        peerPublic = new DHPublicKeyParameters(new BigInteger(dhPub), importDhParams);
                        dhKP = Peer.GenerateKeys(importDhParams);

                        // generate key agreement
                        IBasicAgreement keyAgree = new DHBasicAgreement();
                        keyAgree.Init(dhKP.Private);

                        this.agreementKey = keyAgree.CalculateAgreement(peerPublic);
#if DH_DEBUG_TRACE
                        Messages.Trace("conn rx agreementKey : " + agreementKey.ToString());
#endif
                        byte[] connDhPub = Peer.GetPublicKey(dhKP);

                        OutgoingMessage om = Peer.CreateMessage(0);
                        om.MessageType = MessageType.DiffieHellmanResponse;
                        om.Write(connDhPub, true);
#if DH_DEBUG_TRACE
                        byte[] dhAgree = agreementKey.ToByteArray();
                        om.Write(dhAgree, true);
#endif
                        Peer.SendInternal(om, RemoteEndpoint);
                    }
                    break;

                case MessageType.DiffieHellmanResponse:
                    {
                        if (!Peer.Configuration.AcceptIncomingConnections)
                        {
                            RPCLogger.Trace("Completed DH handshake");

                            IncomingMessage response = Peer.SetupReadHelperMessage(ptr, payloadLength);
                            float remoteNow = response.ReadFloat();
                            bool enableEnc = response.ReadBoolean();
                            bool negoEnc = response.ReadBoolean();

                            if (Peer.Configuration.EncryptionProvider != null)
                            {
                                if (enableEnc)
                                {
                                    RPCLogger.Trace("Remote wants encryption, switching communication encrypted");
                                    Peer.Configuration.EnableEncryption = enableEnc;
                                    Peer.Configuration.NegotiateEncryption = negoEnc;
                                }
                            }

                            // enable encryption -- automatically
                            if (status != ConnectionStatus.ConnectedSecured)
                                SetStatus(ConnectionStatus.ConnectedSecured, "Connected and secured to " + RemoteUniqueId);
                            return;
                        }

                        RPCLogger.Trace("Completed DH handshake");
                        completedDhHandshake = true;

                        IncomingMessage msg = Peer.SetupReadHelperMessage(ptr, payloadLength);
                        byte[] dhPub = msg.ReadBytes();

                        peerPublic = new DHPublicKeyParameters(new BigInteger(dhPub), Peer.DiffieHellmanParameters);
                        IBasicAgreement keyAgree = new DHBasicAgreement();
                        keyAgree.Init(Peer.CipherKeyPair.Private);

                        this.agreementKey = keyAgree.CalculateAgreement(peerPublic);
#if DH_DEBUG_TRACE
                        Messages.Trace("server agreementKey : " + agreementKey.ToString());

                        byte[] dhAgree = msg.ReadBytes();
                        if (!agreementKey.Equals(new BigInteger(dhAgree)))
                            Messages.WriteWarning("DH key exchange failed -- no agreement");
                        else
                            Messages.WriteWarning("DH key exchange suceeded -- key agreement");
#endif
                        if (status != ConnectionStatus.ConnectedSecured)
                            SetStatus(ConnectionStatus.ConnectedSecured, "Connected and secured to " + RemoteUniqueId);

                        OutgoingMessage om = Peer.CreateMessage(0);
                        om.MessageType = MessageType.DiffieHellmanResponse;
                        om.Write(now);
                        om.Write(Peer.Configuration.EnableEncryption);
                        om.Write(Peer.Configuration.NegotiateEncryption);
                        Peer.SendInternal(om, RemoteEndpoint);
                    }
                    break;

                case MessageType.Disconnect:
                    {
                        IncomingMessage msg = Peer.SetupReadHelperMessage(ptr, payloadLength);

                        DisconnectRequested = true;
                        disconnectMessage = msg.ReadString();
                        disconnectReqSendBye = false;
                        //ExecuteDisconnect(msg.ReadString(), false);
                    }
                    break;

                case MessageType.Acknowledge:
                    for (int i = 0; i < payloadLength; i += 3)
                    {
                        MessageType acktp = (MessageType)Peer.receiveBuffer[ptr++]; // netmessagetype
                        int seqNr = Peer.receiveBuffer[ptr++];
                        seqNr |= (Peer.receiveBuffer[ptr++] << 8);

                        // need to enqueue this and handle it in the netconnection heartbeat; so be able to send resends together with normal sends
                        queuedIncomingAcks.Enqueue(new Tuple<MessageType, int>(acktp, seqNr));
                    }
                    break;

                case MessageType.Ping:
                    int pingNr = Peer.receiveBuffer[ptr++];
                    SendPong(pingNr);
                    break;

                case MessageType.Pong:
                    IncomingMessage pmsg = Peer.SetupReadHelperMessage(ptr, payloadLength);
                    int pongNr = pmsg.ReadByte();
                    float remoteSendTime = pmsg.ReadSingle();
                    ReceivedPong(now, pongNr, remoteSendTime);
                    break;

                case MessageType.ExpandMTURequest:
                    SendMTUSuccess(payloadLength);
                    break;

                case MessageType.ExpandMTUSuccess:
                    IncomingMessage emsg = Peer.SetupReadHelperMessage(ptr, payloadLength);
                    int size = emsg.ReadInt32();
                    HandleExpandMTUSuccess(now, size);
                    break;

                default:
                    RPCLogger.Trace("Connection received unhandled library message: " + tp);
                    break;
            }
        }

        /// <summary>
        /// Received a message over the wire
        /// </summary>
        /// <param name="msg">Incoming message</param>
        internal void ReceivedMessage(IncomingMessage msg)
        {
            Peer.VerifyNetworkThread();

            MessageType tp = msg.ReceivedMessageType;

            int channelSlot = (int)tp - 1;
            IReceiverChannel chan = receiveChannels[channelSlot];
            if (chan == null)
                chan = CreateReceiverChannel(tp);

            chan.ReceiveMessage(msg);
        }

        /// <summary>
        /// Create a channel to receive messages
        /// </summary>
        /// <param name="tp">Message Type</param>
        /// <returns>Receiver Channel</returns>
        private IReceiverChannel CreateReceiverChannel(MessageType tp)
        {
            Peer.VerifyNetworkThread();

            // create receiver channel
            IReceiverChannel chan;
            DeliveryMethod method = NetUtility.GetDeliveryMethod(tp);
            switch (method)
            {
                case DeliveryMethod.Unreliable:
                    chan = new UnreliableUnorderedReceiver(this);
                    break;

                case DeliveryMethod.ReliableOrdered:
                    chan = new ReliableOrderedReceiver(this, NetUtility.ReliableOrderedWindowSize);
                    break;

                case DeliveryMethod.UnreliableSequenced:
                    chan = new UnreliableSequencedReceiver(this);
                    break;

                case DeliveryMethod.ReliableUnordered:
                    chan = new ReliableUnorderedReceiver(this, NetUtility.ReliableOrderedWindowSize);
                    break;

                case DeliveryMethod.ReliableSequenced:
                    chan = new ReliableSequencedReceiver(this, NetUtility.ReliableSequencedWindowSize);
                    break;

                default:
                    throw new NetworkException("Unhandled DeliveryMethod!");
            }

            int channelSlot = (int)tp - 1;
            NetworkException.Assert(receiveChannels[channelSlot] == null);
            receiveChannels[channelSlot] = chan;

            return chan;
        }

        /// <summary>
        /// Queue acknowledgment
        /// </summary>
        /// <param name="tp">Message Type</param>
        /// <param name="sequenceNumber"></param>
        internal void QueueAck(MessageType tp, int sequenceNumber)
        {
            queuedOutgoingAcks.Enqueue(new Tuple<MessageType, int>(tp, sequenceNumber));
        }

        /// <summary>
        /// Zero windowSize indicates that the channel is not yet instantiated (used)
        /// Negative freeWindowSlots means this amount of messages are currently queued but delayed due to closed window
        /// </summary>
        public void GetSendQueueInfo(DeliveryMethod method, int sequenceChannel, out int windowSize, out int freeWindowSlots)
        {
            int channelSlot = (int)method - 1 + sequenceChannel;
            var chan = sendChannels[channelSlot];
            if (chan == null)
            {
                windowSize = NetUtility.GetWindowSize(method);
                freeWindowSlots = windowSize;
                return;
            }

            windowSize = chan.WindowSize;
            freeWindowSlots = chan.GetAllowedSends() - chan.queuedSends.Count;
            return;
        }

        /// <summary>
        /// Shutdown this connection
        /// </summary>
        /// <param name="reason">Reason for shutting down connection</param>
        internal void Shutdown(string reason)
        {
            ExecuteDisconnect(reason, true);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return "[Connection to " + RemoteEndpoint + "]";
        }
    } // public partial class Connection
} // namespace TridentFramework.RPC.Net.PeerConnection
