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
using System.Diagnostics;
using System.Net;

namespace TridentFramework.RPC.Net.Message
{
    /// <summary>
    /// Base class for <see cref="IncomingMessage"/> and <see cref="OutgoingMessage"/>.
    /// </summary>
    public partial class MessageBuffer
    {
        /*
        ** Methods
        */

        /// <summary>
        /// Gets the internal data buffer
        /// </summary>
        public byte[] PeekDataBuffer() { return data; }

        //
        // 1 bit
        //
        /// <summary>
        /// Reads a 1-bit Boolean without advancing the read pointer
        /// </summary>
        public bool PeekBoolean()
        {
            NetworkException.Assert(bitLength - readPosition >= 1, READ_OVERFLOW_ERROR);
            byte retval = BitWriter.ReadByte(data, 1, readPosition);
            return (retval > 0 ? true : false);
        }

        //
        // 8 bit
        //
        /// <summary>
        /// Reads a Byte without advancing the read pointer
        /// </summary>
        public byte PeekByte()
        {
            NetworkException.Assert(bitLength - readPosition >= 8, READ_OVERFLOW_ERROR);
            byte retval = BitWriter.ReadByte(data, 8, readPosition);
            return retval;
        }

        /// <summary>
        /// Reads an SByte without advancing the read pointer
        /// </summary>
        public sbyte PeekSByte()
        {
            NetworkException.Assert(bitLength - readPosition >= 8, READ_OVERFLOW_ERROR);
            byte retval = BitWriter.ReadByte(data, 8, readPosition);
            return (sbyte)retval;
        }

        /// <summary>
        /// Reads the specified number of bits into a Byte without advancing the read pointer
        /// </summary>
        public byte PeekByte(int numberOfBits)
        {
            byte retval = BitWriter.ReadByte(data, numberOfBits, readPosition);
            return retval;
        }

        /// <summary>
        /// Reads the specified number of bytes without advancing the read pointer
        /// </summary>
        public byte[] PeekBytes(int numberOfBytes)
        {
            NetworkException.Assert(bitLength - readPosition >= (numberOfBytes * 8), READ_OVERFLOW_ERROR);

            byte[] retval = new byte[numberOfBytes];
            BitWriter.ReadBytes(data, numberOfBytes, readPosition, retval, 0);
            return retval;
        }

        /// <summary>
        /// Reads the specified number of bytes without advancing the read pointer
        /// </summary>
        public void PeekBytes(byte[] into, int offset, int numberOfBytes)
        {
            NetworkException.Assert(bitLength - readPosition >= (numberOfBytes * 8), READ_OVERFLOW_ERROR);
            NetworkException.Assert(offset + numberOfBytes <= into.Length);

            BitWriter.ReadBytes(data, numberOfBytes, readPosition, into, offset);
            return;
        }

        //
        // 16 bit
        //
        /// <summary>
        /// Reads an Int16 without advancing the read pointer
        /// </summary>
        public Int16 PeekInt16()
        {
            NetworkException.Assert(bitLength - readPosition >= 16, READ_OVERFLOW_ERROR);
            uint retval = BitWriter.ReadUInt16(data, 16, readPosition);
            return (short)retval;
        }

        /// <summary>
        /// Reads a UInt16 without advancing the read pointer
        /// </summary>
        public UInt16 PeekUInt16()
        {
            NetworkException.Assert(bitLength - readPosition >= 16, READ_OVERFLOW_ERROR);
            uint retval = BitWriter.ReadUInt16(data, 16, readPosition);
            return (ushort)retval;
        }

        //
        // 32 bit
        //
        /// <summary>
        /// Reads an Int32 without advancing the read pointer
        /// </summary>
        public Int32 PeekInt32()
        {
            NetworkException.Assert(bitLength - readPosition >= 32, READ_OVERFLOW_ERROR);
            uint retval = BitWriter.ReadUInt32(data, 32, readPosition);
            return (Int32)retval;
        }

        /// <summary>
        /// Reads the specified number of bits into an Int32 without advancing the read pointer
        /// </summary>
        public Int32 PeekInt32(int numberOfBits)
        {
            NetworkException.Assert((numberOfBits > 0 && numberOfBits <= 32), "ReadInt() can only read between 1 and 32 bits");
            NetworkException.Assert(bitLength - readPosition >= numberOfBits, READ_OVERFLOW_ERROR);

            uint retval = BitWriter.ReadUInt32(data, numberOfBits, readPosition);

            if (numberOfBits == 32)
                return (int)retval;

            int signBit = 1 << (numberOfBits - 1);
            if ((retval & signBit) == 0)
                return (int)retval; // positive

            // negative
            unchecked
            {
                uint mask = ((uint)-1) >> (33 - numberOfBits);
                uint tmp = (retval & mask) + 1;
                return -((int)tmp);
            }
        }

        /// <summary>
        /// Reads a UInt32 without advancing the read pointer
        /// </summary>
        public UInt32 PeekUInt32()
        {
            NetworkException.Assert(bitLength - readPosition >= 32, READ_OVERFLOW_ERROR);
            uint retval = BitWriter.ReadUInt32(data, 32, readPosition);
            return retval;
        }

        /// <summary>
        /// Reads the specified number of bits into a UInt32 without advancing the read pointer
        /// </summary>
        public UInt32 PeekUInt32(int numberOfBits)
        {
            NetworkException.Assert((numberOfBits > 0 && numberOfBits <= 32), "ReadUInt() can only read between 1 and 32 bits");

            UInt32 retval = BitWriter.ReadUInt32(data, numberOfBits, readPosition);
            return retval;
        }

        //
        // 64 bit
        //
        /// <summary>
        /// Reads a UInt64 without advancing the read pointer
        /// </summary>
        public UInt64 PeekUInt64()
        {
            NetworkException.Assert(bitLength - readPosition >= 64, READ_OVERFLOW_ERROR);

            ulong low = BitWriter.ReadUInt32(data, 32, readPosition);
            ulong high = BitWriter.ReadUInt32(data, 32, readPosition + 32);

            ulong retval = low + (high << 32);

            return retval;
        }

        /// <summary>
        /// Reads an Int64 without advancing the read pointer
        /// </summary>
        public Int64 PeekInt64()
        {
            NetworkException.Assert(bitLength - readPosition >= 64, READ_OVERFLOW_ERROR);
            unchecked
            {
                ulong retval = PeekUInt64();
                long longRetval = (long)retval;
                return longRetval;
            }
        }

        /// <summary>
        /// Reads the specified number of bits into an UInt64 without advancing the read pointer
        /// </summary>
        public UInt64 PeekUInt64(int numberOfBits)
        {
            NetworkException.Assert((numberOfBits > 0 && numberOfBits <= 64), "ReadUInt() can only read between 1 and 64 bits");
            NetworkException.Assert(bitLength - readPosition >= numberOfBits, READ_OVERFLOW_ERROR);

            ulong retval;
            if (numberOfBits <= 32)
            {
                retval = (ulong)BitWriter.ReadUInt32(data, numberOfBits, readPosition);
            }
            else
            {
                retval = BitWriter.ReadUInt32(data, 32, readPosition);
                retval |= (UInt64)BitWriter.ReadUInt32(data, numberOfBits - 32, readPosition + 32) << 32;
            }
            return retval;
        }

        /// <summary>
        /// Reads the specified number of bits into an Int64 without advancing the read pointer
        /// </summary>
        public Int64 PeekInt64(int numberOfBits)
        {
            NetworkException.Assert(((numberOfBits > 0) && (numberOfBits < 65)), "ReadInt64(bits) can only read between 1 and 64 bits");
            return (long)PeekUInt64(numberOfBits);
        }

        //
        // Floating point
        //
        /// <summary>
        /// Reads a 32-bit Single without advancing the read pointer
        /// </summary>
        public float PeekFloat()
        {
            return PeekSingle();
        }

        /// <summary>
        /// Reads a 32-bit Single without advancing the read pointer
        /// </summary>
        public float PeekSingle()
        {
            NetworkException.Assert(bitLength - readPosition >= 32, READ_OVERFLOW_ERROR);

            if ((readPosition & 7) == 0) // read directly
            {
                float retval = BitConverter.ToSingle(data, readPosition >> 3);
                return retval;
            }

            byte[] bytes = PeekBytes(4);
            return BitConverter.ToSingle(bytes, 0);
        }

        /// <summary>
        /// Reads a 64-bit Double without advancing the read pointer
        /// </summary>
        public double PeekDouble()
        {
            NetworkException.Assert(bitLength - readPosition >= 64, READ_OVERFLOW_ERROR);

            if ((readPosition & 7) == 0) // read directly
            {
                // read directly
                double retval = BitConverter.ToDouble(data, readPosition >> 3);
                return retval;
            }

            byte[] bytes = PeekBytes(8);
            return BitConverter.ToDouble(bytes, 0);
        }

        /// <summary>
        /// Reads a string without advancing the read pointer
        /// </summary>
        public string PeekString()
        {
            int wasReadPosition = readPosition;
            string retval = ReadString();
            readPosition = wasReadPosition;
            return retval;
        }
    } // public partial class MessageBuffer
}  // namespace TridentFramework.RPC.Net.Message
