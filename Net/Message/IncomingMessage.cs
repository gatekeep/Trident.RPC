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
using System.IO;
using System.Diagnostics;
using System.Net;

using TridentFramework.RPC.Net.Encryption;
using TridentFramework.RPC.Net.PeerConnection;

using TridentFramework.Compression.zlib;
using TridentFramework.Compression.LZMA;
using TridentFramework.RPC.Utility;

namespace TridentFramework.RPC.Net.Message
{
    /// <summary>
    /// Incoming message either sent from a remote peer or generated within the library
    /// </summary>
    [DebuggerDisplay("Type={MessageType} LengthBits={LengthBits}")]
    public partial class IncomingMessage : MessageBuffer
    {
        /*
        ** Properties
        */

        /// <summary>
        /// Gets a value indicating whether this message is a fragment.
        /// </summary>
        public bool IsFragment
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the sequence number of this message.
        /// </summary>
        public int SequenceNumber
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the type of received message
        /// </summary>
        public MessageType ReceivedMessageType
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the type of this incoming message
        /// </summary>
        public IncomingMessageType MessageType
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the delivery method this message was sent with (if user data)
        /// </summary>
        public DeliveryMethod DeliveryMethod
        {
            get { return NetUtility.GetDeliveryMethod(ReceivedMessageType); }
        }

        /// <summary>
        /// Gets the sequence channel this message was sent with (if user data)
        /// </summary>
        public int SequenceChannel
        {
            get { return (int)ReceivedMessageType - (int)NetUtility.GetDeliveryMethod(ReceivedMessageType); }
        }

        /// <summary>
        /// Gets the IPEndPoint of sender, if any
        /// </summary>
        public IPEndPoint SenderEndpoint
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the connection of sender, if any
        /// </summary>
        public Connection SenderConnection
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets what local time the message was received from the network
        /// </summary>
        public double ReceiveTime
        {
            get;
            internal set;
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the IncomingMessage class.
        /// </summary>
        internal IncomingMessage()
        {
            // stub
        }

        /// <summary>
        /// Initializes a new instance of the IncomingMessage class.
        /// </summary>
        /// <param name="type">Incoming message type</param>
        internal IncomingMessage(IncomingMessageType type)
        {
            this.MessageType = type;
        }

        /// <summary>
        /// Resets this incoming message to default.
        /// </summary>
        internal void Reset()
        {
            MessageType = IncomingMessageType.Error;
            readPosition = 0;
            ReceivedMessageType = TridentFramework.RPC.Net.Message.MessageType.InternalError;
            this.SenderConnection = null;
            this.BitLength = 0;
            IsFragment = false;
        }

        /// <summary>
        /// Decrypt a message
        /// </summary>
        /// <param name="encryption">The encryption algorithm used to encrypt the message</param>
        /// <returns>true on success</returns>
        public bool Decrypt(IMessageEncryption encryption)
        {
            bool ret = false;
            ret = encryption.Decrypt(this);
#if DEBUG
            if (!ret)
                RPCLogger.WriteError("DEBUG: Decryption failed " + ToString());
#endif
            return ret;
        }

        /// <summary>
        /// Decompress a message using zlib compression.
        /// </summary>
        /// <returns></returns>
        public bool DecompressZlib()
        {
            bool ret = false;
            RPCLogger.Trace("Decompressing zlib" + ToString());
            try
            {
                using (MemoryStream ms = new MemoryStream(Data, false))
                using (InflaterInputStream zlib = new InflaterInputStream(ms, new Inflater(false)))
                {
                    zlib.IsStreamOwner = false;

                    byte[] inflateData = new byte[Data.Length];
                    zlib.Read(inflateData, 0, inflateData.Length);

                    Data = inflateData;
                }
            }
            catch (Exception e)
            {
                RPCLogger.WriteError("Failed to decompress message!");
                RPCLogger.StackTrace(e, false);
                ret = false;
            }
            ret = true;
#if DEBUG
            if (!ret)
                RPCLogger.WriteError("DEBUG: Decompression failed " + ToString());
#endif
            return ret;
        }

        /// <summary>
        /// Decompress a message using LZMA compression.
        /// </summary>
        /// <returns></returns>
        public bool DecompressLzma()
        {
            bool ret = false;
            RPCLogger.Trace("Decompressing LZMA " + ToString());
            try
            {
                Data = Lzma.Decompress(Data);
            }
            catch (Exception e)
            {
                RPCLogger.WriteError("Failed to decompress message!");
                RPCLogger.StackTrace(e, false);
                ret = false;
            }
            ret = true;
#if DEBUG
            if (!ret)
                RPCLogger.WriteError("DEBUG: Decompression failed " + ToString());
#endif
            return ret;
        }

        /// <summary>
        /// Reads a value, in local time comparable to NetTime.Now, written using WriteTime()
        /// Must have a connected sender
        /// </summary>
        public double ReadTime(bool highPrecision)
        {
            return ReadTime(SenderConnection, highPrecision);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return "[IncomingMessage #" + SequenceNumber + " " + this.LengthBytes + " bytes]";
        }
    } // public partial class IncomingMessage
} // namespace TridentFramework.RPC.Net.Message
