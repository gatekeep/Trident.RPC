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
using System.IO;
using System.Diagnostics;

using TridentFramework.RPC.Net.Encryption;

using TridentFramework.Compression.zlib;
using TridentFramework.Compression.LZMA;
using TridentFramework.RPC.Utility;

namespace TridentFramework.RPC.Net.Message
{
    /// <summary>
    /// Outgoing message used to send data to remote peer(s)
    /// </summary>
    [DebuggerDisplay("LengthBits={LengthBits}")]
    public sealed partial class OutgoingMessage : MessageBuffer
    {
        internal int recyclingCount;

        internal int fragmentGroup;             // which group of fragments ths belongs to
        internal int fragmentGroupTotalBits;    // total number of bits in this group
        internal int fragmentChunkByteSize;     // size, in bytes, of every chunk but the last one
        internal int fragmentChunkNumber;       // which number chunk this is, starting with 0

        /*
        ** Properties
        */

        /// <summary>
        /// Gets or sets this messages type
        /// </summary>
        public MessageType MessageType
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not this message was sent
        /// </summary>
        public bool IsSent
        {
            get;
            set;
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="OutgoingMessage"/> class.
        /// </summary>
        internal OutgoingMessage()
        {
            // stub
        }

        /// <summary>
        /// Resets this outgoing message to default.
        /// </summary>
        internal void Reset()
        {
            MessageType = MessageType.InternalError;
            bitLength = 0;
            IsSent = false;
            recyclingCount = 0;
            fragmentGroup = 0;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="intoBuffer"></param>
        /// <param name="ptr"></param>
        /// <param name="sequenceNumber"></param>
        /// <returns></returns>
        internal int Encode(byte[] intoBuffer, int ptr, int sequenceNumber)
        {
            //  8 bits - NetMessageType
            //  1 bit  - Fragment?
            // 15 bits - Sequence number
            // 16 bits - Payload length in bits

            intoBuffer[ptr++] = (byte)MessageType;

            byte low = (byte)((sequenceNumber << 1) | (fragmentGroup == 0 ? 0 : 1));
            intoBuffer[ptr++] = low;
            intoBuffer[ptr++] = (byte)(sequenceNumber >> 7);

            if (fragmentGroup == 0)
            {
                intoBuffer[ptr++] = (byte)bitLength;
                intoBuffer[ptr++] = (byte)(bitLength >> 8);

                int byteLen = NetUtility.BytesToHoldBits(bitLength);
                if (byteLen > 0)
                {
                    Buffer.BlockCopy(Data, 0, intoBuffer, ptr, byteLen);
                    ptr += byteLen;
                }
            }
            else
            {
                int wasPtr = ptr;
                intoBuffer[ptr++] = (byte)bitLength;
                intoBuffer[ptr++] = (byte)(bitLength >> 8);

                //
                // write fragmentation header
                //
                ptr = FragmentationHelper.WriteHeader(intoBuffer, ptr, fragmentGroup, fragmentGroupTotalBits, fragmentChunkByteSize, fragmentChunkNumber);
                int hdrLen = ptr - wasPtr - 2;

                // update length
                int realBitLength = bitLength + (hdrLen * 8);
                intoBuffer[wasPtr] = (byte)realBitLength;
                intoBuffer[wasPtr + 1] = (byte)(realBitLength >> 8);

                int byteLen = NetUtility.BytesToHoldBits(bitLength);
                if (byteLen > 0)
                {
                    Buffer.BlockCopy(Data, (int)(fragmentChunkNumber * fragmentChunkByteSize), intoBuffer, ptr, byteLen);
                    ptr += byteLen;
                }
            }

            NetworkException.Assert(ptr > 0);
            return ptr;
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        internal int GetEncodedSize()
        {
            int retval = NetUtility.UnfragmentedMessageHeaderSize; // regular headers
            if (fragmentGroup != 0)
                retval += FragmentationHelper.GetFragmentationHeaderSize(fragmentGroup, fragmentGroupTotalBits / 8, fragmentChunkByteSize, fragmentChunkNumber);
            retval += this.LengthBytes;

            return retval;
        }

        /// <summary>
        /// Encrypt this message using the provided algorithm; no more writing can be done before sending it or the message will be corrupt!
        /// </summary>
        /// <returns></returns>
        public bool Encrypt(IMessageEncryption encryption)
        {
            bool ret = false;
            ret = encryption.Encrypt(this);
#if DEBUG
            if (!ret)
                RPCLogger.Trace("DEBUG: Encryption failed " + ToString());
#endif
            return ret;
        }

        /// <summary>
        /// Compress a message using zlib compression.
        /// </summary>
        /// <returns></returns>
        public bool CompressZlib()
        {
            bool ret = false;
            RPCLogger.Trace("Compressing zlib " + ToString());
            try
            {
                using (MemoryStream ms = new MemoryStream())
                using (DeflaterOutputStream zlib = new DeflaterOutputStream(ms, new Deflater(Deflater.BEST_COMPRESSION, false)))
                {
                    zlib.IsStreamOwner = false;

                    zlib.Write(Data, 0, Data.Length);
                    zlib.Finish();

                    Data = ms.ToArray();
                }
            }
            catch (Exception e)
            {
                RPCLogger.WriteError("Failed to compress message!");
                RPCLogger.StackTrace(e, false);
                ret = false;
            }
            ret = true;
#if DEBUG
            if (!ret)
                RPCLogger.WriteError("DEBUG: Compression failed " + ToString());
#endif
            return ret;
        }

        /// <summary>
        /// Compress a message using LZMA compression.
        /// </summary>
        /// <returns></returns>
        public bool CompressLzma()
        {
            bool ret = false;
            RPCLogger.Trace("Compressing LZMA " + ToString());
            try
            {
                Data = Lzma.Compress(Data);
            }
            catch (Exception e)
            {
                RPCLogger.WriteError("Failed to compress message!");
                RPCLogger.StackTrace(e, false);
                ret = false;
            }
            ret = true;
#if DEBUG
            if (!ret)
                RPCLogger.WriteError("DEBUG: Compression failed " + ToString());
#endif
            return ret;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return "[OutgoingMessage " + MessageType + " " + this.LengthBytes + " bytes]";
        }
    } // public sealed partial class OutgoingMessage
} // namespace TridentFramework.RPC.Net.Message
