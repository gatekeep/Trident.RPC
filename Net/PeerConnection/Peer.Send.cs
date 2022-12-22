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
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;

using TridentFramework.RPC.Net.Channel;
using TridentFramework.RPC.Net.Encryption;
using TridentFramework.RPC.Net.Message;

using TridentFramework.Cryptography.DiffieHellman;
using TridentFramework.Cryptography.DiffieHellman.Agreement;
using TridentFramework.Cryptography.DiffieHellman.Parameters;
using TridentFramework.RPC.Utility;

namespace TridentFramework.RPC.Net.PeerConnection
{
    /// <summary>
    /// Represents a local peer capable of holding zero, one or more connections to remote peers
    /// </summary>
    public partial class Peer
    {
        /*
        ** Methods
        */

        /// <summary>
        /// Helper to encrypt the given message.
        /// </summary>
        /// <param name="msg">The message to send</param>
        /// <param name="recipient"></param>
        public void EncryptMessage(OutgoingMessage msg, Connection recipient)
        {
            byte messageType = (byte)msg.MessageType;
            if ((messageType > 0) && (messageType < 90))
            {
                if (Configuration.EnableEncryption)
                {
                    IMessageEncryption msgEnc = Configuration.EncryptionProvider;
                    byte[] key = null;

                    if (recipient != null && Configuration.NegotiateEncryption)
                    {
                        if (recipient.AgreementKey == null)
                        {
                            // display a warning if the connection has completed its handshake
                            if (recipient.CompletedDiffieHellmanHandshake)
                                RPCLogger.WriteWarning("cowardly refusing to encrypt -- no agreement key?");
                            return;
                        }

                        RPCLogger.Trace("Encrypting Message " + msg.ToString() + " in negotiated mode");

                        // in negotiated encryption -- use the DH derived key for encryption
                        key = recipient.AgreementKey.ToByteArray();
                        msgEnc.SetKey(key, 0, key.Length);

                        msg.Encrypt(msgEnc);
                        return;
                    }

                    RPCLogger.Trace("Encrypting Message " + msg.ToString() + " in key mode");

                    // in key encryption -- use the configured static key for encryption
                    key = Encoding.ASCII.GetBytes(Configuration.EncryptionKey);
                    msgEnc.SetKey(key, 0, key.Length);

                    msg.Encrypt(msgEnc);
                }
            }
        }

        /// <summary>
        /// Helper to compress the given message.
        /// </summary>
        /// <param name="msg"></param>
        public void CompressMessage(OutgoingMessage msg)
        {
            byte messageType = (byte)msg.MessageType;
            if ((messageType > 0) && (messageType < 90))
            {
                if (Configuration.EnableCompression)
                {
                    switch (Configuration.CompressionType)
                    {
                        case CompressionType.ZLIB:
                            msg.CompressZlib();
                            break;

                        case CompressionType.LZMA:
                            msg.CompressLzma();
                            break;

                        default:
                            throw new InvalidOperationException("unsupported compression type while trying to compress message");
                    }
                }
            }
        }

        /// <summary>
        /// Send a message to a specific connection
        /// </summary>
        /// <param name="msg">The message to send</param>
        /// <param name="recipient">The recipient connection</param>
        /// <param name="method">How to deliver the message</param>
        /// <returns></returns>
        public SendResult SendMessage(OutgoingMessage msg, Connection recipient, DeliveryMethod method)
        {
            return SendMessage(msg, recipient, method, 0);
        }

        /// <summary>
        /// Send a message to a specific connection
        /// </summary>
        /// <param name="msg">The message to send</param>
        /// <param name="recipient">The recipient connection</param>
        /// <param name="method">How to deliver the message</param>
        /// <param name="sequenceChannel">Sequence channel within the delivery method</param>
        /// <returns></returns>
        public SendResult SendMessage(OutgoingMessage msg, Connection recipient, DeliveryMethod method, int sequenceChannel)
        {
            if (msg == null)
                throw new ArgumentNullException("msg");
            if (recipient == null)
                throw new ArgumentNullException("recipient");
            if (sequenceChannel >= NetUtility.ChannelsPerDeliveryMethod)
                throw new ArgumentOutOfRangeException("sequenceChannel");

            NetworkException.Assert(
                ((method != DeliveryMethod.Unreliable && method != DeliveryMethod.ReliableUnordered) ||
                ((method == DeliveryMethod.Unreliable || method == DeliveryMethod.ReliableUnordered) && sequenceChannel == 0)),
                "Delivery method " + method + " cannot use sequence channels other than 0!");

            NetworkException.Assert(method != DeliveryMethod.Unknown, "Bad delivery method!");

            if (msg.IsSent)
                throw new NetworkException("This message has already been sent! Use NetPeer.SendMessage() to send to multiple recipients efficiently");
            msg.IsSent = true;

            bool suppressFragmentation = (method == DeliveryMethod.Unreliable || method == DeliveryMethod.UnreliableSequenced) && Configuration.UnreliableSizeBehavior != UnreliableSizeBehavior.NormalFragmentation;

            int len = NetUtility.UnfragmentedMessageHeaderSize + msg.LengthBytes; // headers + length, faster than calling msg.GetEncodedSize
            if (len <= recipient.currentMTU || suppressFragmentation)
            {
                EncryptMessage(msg, recipient);
                CompressMessage(msg);

                Interlocked.Increment(ref msg.recyclingCount);
                return recipient.EnqueueMessage(msg, method, sequenceChannel);
            }
            else
            {
                // message must be fragmented!
                if ((recipient.Status != ConnectionStatus.Connected) &&
                    (recipient.Status != ConnectionStatus.ConnectedSecured))
                    return SendResult.Failed;
                return SendFragmentedMessage(msg, new Connection[] { recipient }, method, sequenceChannel, false);
            }
        }

        /// <summary>
        /// Get MTU size
        /// </summary>
        /// <param name="recipients">Connection recipients</param>
        /// <returns>MTU size</returns>
        internal int GetMTU(IList<Connection> recipients)
        {
            int count = recipients.Count;

            int mtu = int.MaxValue;
            if (count < 1)
            {
#if DEBUG
                throw new NetworkException("GetMTU called with no recipients");
#else
				// we don't have access to the particular peer, so just use default MTU
				return PeerConfiguration.DEFAULT_MTU;
#endif
            }

            for (int i = 0; i < count; i++)
            {
                var conn = recipients[i];
                int cmtu = conn.currentMTU;
                if (cmtu < mtu)
                    mtu = cmtu;
            }
            return mtu;
        }

        /// <summary>
        /// Send a message to a list of connections
        /// </summary>
        /// <param name="msg">The message to send</param>
        /// <param name="recipients">The list of recipients to send to</param>
        /// <param name="method">How to deliver the message</param>
        /// <param name="sequenceChannel">Sequence channel within the delivery method</param>
        public void SendMessage(OutgoingMessage msg, IList<Connection> recipients, DeliveryMethod method, int sequenceChannel)
        {
            if (msg == null)
                throw new ArgumentNullException("msg");
            if (recipients == null)
            {
                if (msg.IsSent == false)
                    Recycle(msg);
                throw new ArgumentNullException("recipients");
            }
            if (recipients.Count < 1)
            {
                if (msg.IsSent == false)
                    Recycle(msg);
                throw new NetworkException("recipients must contain at least one item");
            }
            if (method == DeliveryMethod.Unreliable || method == DeliveryMethod.ReliableUnordered)
                NetworkException.Assert(sequenceChannel == 0, "Delivery method " + method + " cannot use sequence channels other than 0!");

            if (Configuration.EnableEncryption && !Configuration.NegotiateEncryption)
                EncryptMessage(msg, null);

            if (Configuration.EnableCompression)
                CompressMessage(msg);

            if (msg.IsSent)
                throw new NetworkException("This message has already been sent! Use NetPeer.SendMessage() to send to multiple recipients efficiently");
            msg.IsSent = true;

            RPCLogger.Trace("Sending " + msg.ToString() + "[" + recipients.Count + " recipient(s)]");
            int mtu = GetMTU(recipients);

            int len = msg.GetEncodedSize();
            if (len <= mtu)
            {
                bool forceFragment = false;
                Interlocked.Add(ref msg.recyclingCount, recipients.Count);
                foreach (Connection conn in recipients)
                {
                    if (conn == null)
                    {
                        Interlocked.Decrement(ref msg.recyclingCount);
                        continue;
                    }

                    OutgoingMessage om = msg;

                    if (Configuration.EnableEncryption && Configuration.NegotiateEncryption)
                    {
                        om = CreateMessage(msg);
                        EncryptMessage(om, conn);
                    }

                    if (Configuration.EnableCompression)
                        CompressMessage(om);

                    if (om.GetEncodedSize() > Configuration.MaximumTransmissionUnit)
                    {
                        forceFragment = true;
                        break;
                    }

                    SendResult res = conn.EnqueueMessage(om, method, sequenceChannel);
                    if (res == SendResult.Dropped)
                    {
                        RPCLogger.Trace(msg + " dropped immediately due to full queues");
                        Interlocked.Decrement(ref msg.recyclingCount);
                    }
                }

                if (forceFragment)
                {
                    // message must be fragmented!
                    SendFragmentedMessage(msg, recipients, method, sequenceChannel, true);
                }
            }
            else
            {
                // message must be fragmented!
                SendFragmentedMessage(msg, recipients, method, sequenceChannel, true);
            }

            return;
        }

        /// <summary>
        /// Send a message to an unconnected host
        /// </summary>
        public void SendUnconnectedMessage(OutgoingMessage msg, string host, int port)
        {
            if (msg == null)
                throw new ArgumentNullException("msg");
            if (host == null)
                throw new ArgumentNullException("host");
            if (msg.IsSent)
                throw new NetworkException("This message has already been sent! Use NetPeer.SendMessage() to send to multiple recipients efficiently");
            if (msg.LengthBytes > Configuration.MaximumTransmissionUnit)
                throw new NetworkException("Unconnected messages too long! Must be shorter than NetConfiguration.MaximumTransmissionUnit (currently " + Configuration.MaximumTransmissionUnit + ")");

            RPCLogger.Trace("Sending " + msg.ToString() + " -> [" + host + ":" + port + "]");

            EncryptMessage(msg, null);
            CompressMessage(msg);

            msg.IsSent = true;
            msg.MessageType = MessageType.Unconnected;

            IPAddress adr = NetUtility.Resolve(host);
            if (adr == null)
                throw new NetworkException("Failed to resolve " + host);

            Interlocked.Increment(ref msg.recyclingCount);
            unsentUnconnectedMessages.Enqueue(new Tuple<IPEndPoint, OutgoingMessage>(new IPEndPoint(adr, port), msg));
        }

        /// <summary>
        /// Send a message to an unconnected host
        /// </summary>
        public void SendUnconnectedMessage(OutgoingMessage msg, IPEndPoint recipient)
        {
            if (msg == null)
                throw new ArgumentNullException("msg");
            if (recipient == null)
                throw new ArgumentNullException("recipient");
            if (msg.IsSent)
                throw new NetworkException("This message has already been sent! Use NetPeer.SendMessage() to send to multiple recipients efficiently");
            if (msg.LengthBytes > Configuration.MaximumTransmissionUnit)
                throw new NetworkException("Unconnected messages too long! Must be shorter than NetConfiguration.MaximumTransmissionUnit (currently " + Configuration.MaximumTransmissionUnit + ")");

            RPCLogger.Trace("Sending " + msg.ToString() + " -> [" + recipient.Address.ToString() + ":" + recipient.Port + "]");

            EncryptMessage(msg, null);
            CompressMessage(msg);

            msg.MessageType = MessageType.Unconnected;
            msg.IsSent = true;

            Interlocked.Increment(ref msg.recyclingCount);
            unsentUnconnectedMessages.Enqueue(new Tuple<IPEndPoint, OutgoingMessage>(recipient, msg));
        }

        /// <summary>
        /// Send a message to an unconnected host
        /// </summary>
        public void SendUnconnectedMessage(OutgoingMessage msg, IList<IPEndPoint> recipients)
        {
            if (msg == null)
                throw new ArgumentNullException("msg");
            if (recipients == null)
                throw new ArgumentNullException("recipients");
            if (msg.IsSent)
                throw new NetworkException("This message has already been sent! Use NetPeer.SendMessage() to send to multiple recipients efficiently");
            if (msg.LengthBytes > Configuration.MaximumTransmissionUnit)
                throw new NetworkException("Unconnected messages too long! Must be shorter than NetConfiguration.MaximumTransmissionUnit (currently " + Configuration.MaximumTransmissionUnit + ")");

            RPCLogger.Trace("Sending " + msg.ToString() + "[" + recipients.Count + " recipient(s)]");

            EncryptMessage(msg, null);
            CompressMessage(msg);

            msg.MessageType = MessageType.Unconnected;
            msg.IsSent = true;

            Interlocked.Add(ref msg.recyclingCount, recipients.Count);
            foreach (IPEndPoint ep in recipients)
                unsentUnconnectedMessages.Enqueue(new Tuple<IPEndPoint, OutgoingMessage>(ep, msg));
        }

        /// <summary>
        /// Send a message to this exact same peer (loopback)
        /// </summary>
        public void SendUnconnectedToSelf(OutgoingMessage om)
        {
            if (om == null)
                throw new ArgumentNullException("msg");
            if (om.IsSent)
                throw new NetworkException("This message has already been sent! Use NetPeer.SendMessage() to send to multiple recipients efficiently");

            if (Configuration.EnableEncryption)
            {
                if (!Configuration.NegotiateEncryption)
                    EncryptMessage(om, null);
                else
                {
                    // in negotiated encryption -- use the DH derived key for encryption
                    IMessageEncryption msgEnc = Configuration.EncryptionProvider;
                    IBasicAgreement keyAgree = new DHBasicAgreement();
                    keyAgree.Init(dhKP.Private);

                    BigInteger agreementKey = keyAgree.CalculateAgreement(dhKP.Public);
                    byte[] key = agreementKey.ToByteArray();
                    msgEnc.SetKey(key, 0, key.Length);

                    om.Encrypt(msgEnc);
                }
            }
            CompressMessage(om);

            om.MessageType = MessageType.Unconnected;
            om.IsSent = true;

            if (Configuration.IsMessageTypeEnabled(IncomingMessageType.UnconnectedData) == false)
            {
                Interlocked.Decrement(ref om.recyclingCount);
                return; // dropping unconnected message since it's not enabled for receiving
            }

            // convert outgoing to incoming
            IncomingMessage im = CreateIncomingMessage(IncomingMessageType.UnconnectedData, om.LengthBytes);
            im.Write(om);
            im.IsFragment = false;
            im.ReceiveTime = NetTime.Now;
            im.SenderConnection = null;
            im.SenderEndpoint = Socket.LocalEndPoint as IPEndPoint;
            NetworkException.Assert(im.BitLength == om.BitLength);

            // recycle outgoing message
            Recycle(om);

            ReleaseMessage(im);
        }

        /// <summary>
        /// Send MTU packet.
        /// </summary>
        /// <param name="numBytes">Number of bytes</param>
        /// <param name="target">IP Endpoint</param>
        /// <returns></returns>
        internal bool SendMTUPacket(int numBytes, IPEndPoint target)
        {
            try
            {
                Socket.DontFragment = true;
                int bytesSent = Socket.SendTo(sendBuffer, 0, numBytes, SocketFlags.None, target);
                if (numBytes != bytesSent)
                    RPCLogger.Trace("Failed to send the full " + numBytes + "; only " + bytesSent + " bytes sent in packet!");
            }
            catch (SocketException sx)
            {
                if (sx.SocketErrorCode == SocketError.MessageSize)
                    return false;
                if (sx.SocketErrorCode == SocketError.WouldBlock)
                {
                    // send buffer full?
                    RPCLogger.Trace("Socket threw exception; would block - send buffer full?");
                    return true;
                }
                if (sx.SocketErrorCode == SocketError.ConnectionReset)
                    return true;

                RPCLogger.WriteError("Failed to send packet (" + sx.SocketErrorCode + ")");
                RPCLogger.StackTrace(sx, false);
            }
            catch (Exception ex)
            {
                RPCLogger.WriteError("Unknown exception occurred while sending packet");
                RPCLogger.StackTrace(ex, false);
            }
            finally
            {
                Socket.DontFragment = false;
            }
            return true;
        }

        /// <summary>
        /// Send packet down the wire.
        /// </summary>
        /// <param name="numBytes">Number of bytes to send</param>
        /// <param name="target">Endpoint to send bytes to</param>
        /// <param name="numMessages">Number of messages</param>
        /// <param name="connectionReset"></param>
        internal void SendPacket(int numBytes, IPEndPoint target, int numMessages, out bool connectionReset)
        {
            connectionReset = false;
            IPAddress ba = default(IPAddress);
            try
            {
                ba = NetUtility.GetCachedBroadcastAddress();

                // TODO: refactor this check outta here
                if (target.Address == ba)
                {
                    // Some networks do not allow
                    // a global broadcast so we use the BroadcastAddress from the configuration
                    // this can be resolved to a local broadcast addresss e.g 192.168.x.255
                    target.Address = Configuration.BroadcastAddress;
                    Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
                }

                int bytesSent = Socket.SendTo(sendBuffer, 0, numBytes, SocketFlags.None, target);
                if (numBytes != bytesSent)
                    RPCLogger.Trace("Failed to send the full " + numBytes + "; only " + bytesSent + " bytes sent in packet!");
            }
            catch (SocketException sx)
            {
                if (sx.SocketErrorCode == SocketError.WouldBlock)
                {
                    // send buffer full?
                    RPCLogger.Trace("Socket threw exception; would block - send buffer full?");
                    return;
                }

                if (sx.SocketErrorCode == SocketError.ConnectionReset)
                {
                    // connection reset by peer, aka connection forcibly closed aka "ICMP port unreachable"
                    connectionReset = true;
                    return;
                }

                RPCLogger.WriteError("Failed to send packet");
                RPCLogger.StackTrace(sx, false);
            }
            catch (Exception ex)
            {
                RPCLogger.WriteError("Failed to send packet");
                RPCLogger.StackTrace(ex, false);
            }
            finally
            {
                if (target.Address == IPAddress.Broadcast)
                    Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, false);
            }
            Statistics.PacketSent(numBytes, 1);
            return;
        }
    } // public partial class Peer
} // namespace TridentFramework.RPC.Net.PeerConnection
