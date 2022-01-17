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

using System;
using System.Collections;

using TridentFramework.RPC.Net.Message;
using TridentFramework.RPC.Net.PeerConnection;

using TridentFramework.RPC.Utility;

namespace TridentFramework.RPC.Net
{
    /// <summary>
    /// Defines options for network packet transmission.
    /// </summary>
    [Flags]
    public enum DataTransferOptions
    {
        /// <summary>
        /// Sends the data with no guarantees. Packets of this type may be delivered in any order, with occasional packet loss. This is the most efficient
        /// option in terms of network bandwidth and machine resource usage. However, it is recommended only in situations where your application can
        /// recover from occasional packet loss.
        /// </summary>
        None,

        /// <summary>
        /// Sends the data with reliable delivery, but no special ordering. Packets of this type are resent until arrival at the destination.
        /// They may arrive out of order.
        /// </summary>
        Reliable,

        /// <summary>
        /// Sends the data with no guarantees, but automatically dropping late messages.
        /// </summary>
        Sequenced,

        /// <summary>
        /// Sends the data with reliable delivery, but automatically dropping late messages.
        /// </summary>
        ReliableSequenced,

        /// <summary>
        /// Sends the data with reliability and arrival in the order originally sent. Packets of this type are resent until arrival and ordered
        /// internally. This means they arrive in the same order in which they were sent. In terms of network bandwidth usage, this is the strongest
        /// and most expensive option. Use this only when arrival and ordering are essential. Commonly, a application uses this option for a small percentage
        /// of packets. The majority of application data is sent using None or Reliable.
        /// </summary>
        ReliableInOrder
    } // public enum DataTransferOptions

    /// <summary>
    /// Enumeration of message types to transmit.
    /// </summary>
    public enum MessageToTransmit : byte
    {
        /// <summary>
        /// Special message used to indicate the connection of a new client.
        /// </summary>
        CLIENT_CONNECTED = 0x01,

        /// <summary>
        /// Special message used to indicate a client has disconnected.
        /// </summary>
        CLIENT_DISCONNECTED = 0x02,

        /// <summary>
        /// Raw Byte Array.
        /// </summary>
        RAW_BYTES = 0xF0,

        /// <summary>
        /// User-defined Data.
        /// </summary>
        USER_DEFINED = 0xFF,
    }; // public enum MessageToTransmit

    /// <summary>
    /// Defines a abstract network service base.
    /// </summary>
    public abstract class IClientServerBase : IDisposable
    {
        private bool disposed = false;

        protected PeerConfiguration netConfig;
        protected Peer peer;
        protected long connectionId = 0L;

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="IClientServerBase"/> class.
        /// </summary>
        /// <param name="peerConfiguration">Network Peer Configuration</param>
        public IClientServerBase(PeerConfiguration peerConfiguration)
        {
            this.netConfig = peerConfiguration;
            this.peer = null;
        }

        /// <summary>
        /// Prepares user-defined data message to send.
        /// </summary>
        /// <param name="transferOptions"></param>
        /// <param name="seqCh"></param>
        /// <returns></returns>
        public virtual OutgoingMessage PrepareMessage(DataTransferOptions transferOptions = DataTransferOptions.Reliable, int seqCh = 1)
        {
            if (peer is NetServerPeer)
                return PrepareMessageTo(0L, transferOptions);
            else
            {
                // prepare low-level message to send
                OutgoingMessage outMsg = peer.CreateMessage();
                outMsg.MessageType = ConvertToNetMessageType(transferOptions, seqCh);

                outMsg.Write(MessageToTransmit.USER_DEFINED);

                // pack connection ID
                outMsg.Write(connectionId);

                return outMsg;
            }
        }

        /// <summary>
        /// Prepares user-defined data message to send to a specific endpoint.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="transferOptions"></param>
        /// <param name="seqCh"></param>
        /// <returns></returns>
        public virtual OutgoingMessage PrepareMessageTo(long id, DataTransferOptions transferOptions = DataTransferOptions.Reliable, int seqCh = 1)
        {
            if (peer is NetServerPeer)
            {
                // prepare low-level message to send
                OutgoingMessage outMsg = peer.CreateMessage();
                outMsg.MessageType = ConvertToNetMessageType(transferOptions, seqCh);

                outMsg.Write(MessageToTransmit.USER_DEFINED);

                // pack connection ID
                outMsg.Write(id);

                return outMsg;
            }
            else
                return PrepareMessage(transferOptions);
        }

        /// <summary>
        /// Sends a user-defined message.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="transferOptions"></param>
        public virtual void SendMessage(OutgoingMessage message, DataTransferOptions transferOptions = DataTransferOptions.Reliable)
        {
            if (message == null)
                throw new NullReferenceException("message");

            if (peer is NetServerPeer)
                SendMessageTo(0, message, false, transferOptions);
            else
            {
                if (peer is NetClientPeer)
                {
                    NetClientPeer client = (NetClientPeer)peer;

                    // transmit
                    client.SendMessage(message, ConvertToNetDeliveryMethod(transferOptions));
                }
                else
                    throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Sends a user-defined message to a specific endpoint.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="message"></param>
        /// <param name="excludeId"></param>
        /// <param name="transferOptions"></param>
        public virtual void SendMessageTo(long id, OutgoingMessage message, bool excludeId = false, DataTransferOptions transferOptions = DataTransferOptions.Reliable)
        {
            if (message == null)
                throw new NullReferenceException("message");

            if (peer is NetServerPeer)
            {
                NetServerPeer server = (NetServerPeer)peer;

                // transmit
                DeliveryMethod deliveryMethod = ConvertToNetDeliveryMethod(transferOptions);
                if (id == 0)
                    server.SendToAll(message, deliveryMethod);
                else
                {
                    if (excludeId)
                        server.SendMessageExcept(message, id, deliveryMethod);
                    else
                        server.SendMessageTo(message, id, deliveryMethod);
                }
            }
            else
                SendMessage(message, transferOptions);
        }

        /// <summary>
        /// Sends a raw data message.
        /// </summary>
        /// <param name="raw"></param>
        /// <param name="transferOptions"></param>
        public virtual void Send(byte[] raw, DataTransferOptions transferOptions = DataTransferOptions.Reliable, int seqCh = 1)
        {
            if (raw == null)
                throw new NullReferenceException("raw");

            if (peer is NetServerPeer)
                SendTo(0, raw, false, transferOptions);
            else
            {
                if (peer is NetClientPeer)
                {
                    NetClientPeer client = (NetClientPeer)peer;

                    // prepare low-level message to send
                    OutgoingMessage outMsg = client.CreateMessage();
                    outMsg.MessageType = ConvertToNetMessageType(transferOptions, seqCh);

                    outMsg.Write(MessageToTransmit.RAW_BYTES);

                    // pack connection GUID
                    outMsg.Write(connectionId);

                    if (raw != null)
                    {
                        // compute and write CRC
                        uint crc = CRC.CalculateDigest(raw, 0, raw.Length);
                        outMsg.Write(crc);

                        // write raw byte data
                        outMsg.Write(raw, true);
                    }

                    // transmit
                    client.SendMessage(outMsg, ConvertToNetDeliveryMethod(transferOptions));
                }
                else
                    throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Sends a raw data message to a specific endpoint.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="raw"></param>
        /// <param name="excludeGuid"></param>
        /// <param name="transferOptions"></param>
        public virtual void SendTo(long id, byte[] raw, bool excludeGuid = false, DataTransferOptions transferOptions = DataTransferOptions.Reliable, int seqCh = 1)
        {
            if (raw == null)
                throw new NullReferenceException("raw");

            if (peer is NetServerPeer)
            {
                NetServerPeer server = (NetServerPeer)peer;

                // prepare low-level message to send
                OutgoingMessage outMsg = server.CreateMessage();
                outMsg.MessageType = ConvertToNetMessageType(transferOptions, seqCh);

                outMsg.Write(MessageToTransmit.RAW_BYTES);

                // pack connection ID
                outMsg.Write(id);

                if (raw != null)
                {
                    // compute and write CRC
                    uint crc = CRC.CalculateDigest(raw, 0, raw.Length);
                    outMsg.Write(crc);

                    // write raw byte data
                    outMsg.Write(raw, true);
                }

                // transmit
                DeliveryMethod deliveryMethod = ConvertToNetDeliveryMethod(transferOptions);
                if (id == 0)
                    server.SendToAll(outMsg, deliveryMethod);
                else
                {
                    if (excludeGuid)
                        server.SendMessageExcept(outMsg, id, deliveryMethod);
                    else
                        server.SendMessageTo(outMsg, id, deliveryMethod);
                }
            }
            else
                Send(raw, transferOptions);
        }

        /// <summary>
        /// Internal helper to process a raw bytes message.
        /// </summary>
        protected byte[] ProcessRawBytes(IncomingMessage msg)
        {
            uint crc = msg.ReadUInt32();
            byte[] raw = msg.ReadBytes();

            if (!CRC.VerifyDigest(crc, raw, 0, raw.Length))
            {
                RPCLogger.WriteWarning("received raw bytes had an invalid CRC!");
                return null;
            }

            return raw;
        }

        /// <summary>
        /// Occurs when this class is being disposed.
        /// </summary>
        protected abstract void OnDispose();

        /// <summary>
        /// Causes the communication object to transition from its current state
        /// into the opened state.
        /// </summary>
        /// <param name="threadName"></param>
        public abstract void Open(string threadName = "");

        /// <summary>
        /// Causes the communication object to transition from its current state
        /// to the closed state.
        /// </summary>
        public abstract void Close();

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose(bool disposing) executes in two distinct scenarios.
        /// If disposing equals true, the method has been called directly
        /// or indirectly by a user's code. Managed and unmanaged resources
        /// can be disposed.
        /// If disposing equals false, the method has been called by the
        /// runtime from inside the finalizer and you should not reference
        /// other objects. Only unmanaged resources can be disposed.
        /// </summary>
        private void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this.disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    // fire on dispose event
                    OnDispose();
                }
            }
            disposed = true;
        }

        /// <summary>
        /// Converts DataTransferOptions to NetDeliveryOptions
        /// </summary>
        /// <param name="dataTransferOptions"></param>
        /// <returns></returns>
        protected static DeliveryMethod ConvertToNetDeliveryMethod(DataTransferOptions dataTransferOptions)
        {
            switch (dataTransferOptions)
            {
                case DataTransferOptions.None:
                    return DeliveryMethod.Unreliable;

                case DataTransferOptions.Reliable:
                    return DeliveryMethod.ReliableUnordered;

                case DataTransferOptions.Sequenced:
                    return DeliveryMethod.UnreliableSequenced;

                case DataTransferOptions.ReliableSequenced:
                    return DeliveryMethod.ReliableSequenced;

                default:
                case DataTransferOptions.ReliableInOrder:
                    return DeliveryMethod.ReliableOrdered;
            }
        }

        /// <summary>
        /// Converts DataTransferOptions to MessageType
        /// </summary>
        /// <param name="dataTransferOptions"></param>
        /// <param name="seqCh"></param>
        /// <returns></returns>
        protected static MessageType ConvertToNetMessageType(DataTransferOptions dataTransferOptions, int seqCh = 1)
        {
            if (seqCh > 16)
                throw new ArgumentOutOfRangeException("seqCh");

            switch (dataTransferOptions)
            {
                case DataTransferOptions.None:
                    return MessageType.Unused;

                case DataTransferOptions.Reliable:
                    return MessageType.UserReliableUnordered;

                case DataTransferOptions.Sequenced:
                    switch (seqCh)
                    {
                        default:
                        case 1:
                            return MessageType.UserSequenced1;

                        case 2:
                            return MessageType.UserSequenced2;

                        case 3:
                            return MessageType.UserSequenced3;

                        case 4:
                            return MessageType.UserSequenced4;

                        case 5:
                            return MessageType.UserSequenced5;

                        case 6:
                            return MessageType.UserSequenced6;

                        case 7:
                            return MessageType.UserSequenced7;

                        case 8:
                            return MessageType.UserSequenced8;

                        case 9:
                            return MessageType.UserSequenced9;

                        case 10:
                            return MessageType.UserSequenced10;

                        case 11:
                            return MessageType.UserSequenced11;

                        case 12:
                            return MessageType.UserSequenced12;

                        case 13:
                            return MessageType.UserSequenced13;

                        case 14:
                            return MessageType.UserSequenced14;

                        case 15:
                            return MessageType.UserSequenced15;

                        case 16:
                            return MessageType.UserSequenced16;
                    }
                case DataTransferOptions.ReliableSequenced:
                    switch (seqCh)
                    {
                        default:
                        case 1:
                            return MessageType.UserReliableSequenced1;

                        case 2:
                            return MessageType.UserReliableSequenced2;

                        case 3:
                            return MessageType.UserReliableSequenced3;

                        case 4:
                            return MessageType.UserReliableSequenced4;

                        case 5:
                            return MessageType.UserReliableSequenced5;

                        case 6:
                            return MessageType.UserReliableSequenced6;

                        case 7:
                            return MessageType.UserReliableSequenced7;

                        case 8:
                            return MessageType.UserReliableSequenced8;

                        case 9:
                            return MessageType.UserReliableSequenced9;

                        case 10:
                            return MessageType.UserReliableSequenced10;

                        case 11:
                            return MessageType.UserReliableSequenced11;

                        case 12:
                            return MessageType.UserReliableSequenced12;

                        case 13:
                            return MessageType.UserReliableSequenced13;

                        case 14:
                            return MessageType.UserReliableSequenced14;

                        case 15:
                            return MessageType.UserReliableSequenced15;

                        case 16:
                            return MessageType.UserReliableSequenced16;
                    }
                default:
                case DataTransferOptions.ReliableInOrder:
                    switch (seqCh)
                    {
                        default:
                        case 1:
                            return MessageType.UserReliableOrdered1;

                        case 2:
                            return MessageType.UserReliableOrdered2;

                        case 3:
                            return MessageType.UserReliableOrdered3;

                        case 4:
                            return MessageType.UserReliableOrdered4;

                        case 5:
                            return MessageType.UserReliableOrdered5;

                        case 6:
                            return MessageType.UserReliableOrdered6;

                        case 7:
                            return MessageType.UserReliableOrdered7;

                        case 8:
                            return MessageType.UserReliableOrdered8;

                        case 9:
                            return MessageType.UserReliableOrdered9;

                        case 10:
                            return MessageType.UserReliableOrdered10;

                        case 11:
                            return MessageType.UserReliableOrdered11;

                        case 12:
                            return MessageType.UserReliableOrdered12;

                        case 13:
                            return MessageType.UserReliableOrdered13;

                        case 14:
                            return MessageType.UserReliableOrdered14;

                        case 15:
                            return MessageType.UserReliableOrdered15;

                        case 16:
                            return MessageType.UserReliableOrdered16;
                    }
            }
        }
    } // public abstract class IClientServerBase
} // namespace TridentFramework.RPC.Net
