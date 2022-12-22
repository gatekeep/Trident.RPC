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
using System.Net;

using TridentFramework.RPC.Net.PeerConnection;

using TridentFramework.RPC.Utility;

namespace TridentFramework.RPC.Net.Message
{
    /// <summary>
    /// Base class for <see cref="IncomingMessage"/> and <see cref="OutgoingMessage"/>.
    /// </summary>
    public partial class MessageBuffer
    {
        private const string READ_OVERFLOW_ERROR = "Trying to read past the buffer size - likely caused by mismatching Write/Reads, different size or order.";

        /*
        ** Methods
        */

        /// <summary>
        /// Reads a boolean value (stored as a single bit) written using Write(bool)
        /// </summary>
        public bool ReadBoolean()
        {
            NetworkException.Assert(bitLength - readPosition >= 1, READ_OVERFLOW_ERROR);
            byte retval = BitWriter.ReadByte(data, 1, readPosition);
            readPosition += 1;
            return (retval > 0 ? true : false);
        }

        /// <summary>
        /// Reads a byte
        /// </summary>
        public byte ReadByte()
        {
            NetworkException.Assert(bitLength - readPosition >= 8, READ_OVERFLOW_ERROR);
            byte retval = BitWriter.ReadByte(data, 8, readPosition);
            readPosition += 8;
            return retval;
        }

        /// <summary>
        /// Reads a byte and returns true or false for success
        /// </summary>
        public bool ReadByte(out byte result)
        {
            if (bitLength - readPosition < 8)
            {
                result = 0;
                return false;
            }
            result = BitWriter.ReadByte(data, 8, readPosition);
            readPosition += 8;
            return true;
        }

        /// <summary>
        /// Read encapsulation for <see cref="MessageToTransmit"/>.
        /// </summary>
        /// <remarks>Reads a byte from the message and converts it to <see cref="MessageToTransmit"/></remarks>
        /// <returns></returns>
        public MessageToTransmit ReadMessageToTransmit()
        {
            return (MessageToTransmit)ReadByte();
        }

        /// <summary>
        /// Reads a signed byte
        /// </summary>
        public sbyte ReadSByte()
        {
            NetworkException.Assert(bitLength - readPosition >= 8, READ_OVERFLOW_ERROR);
            byte retval = BitWriter.ReadByte(data, 8, readPosition);
            readPosition += 8;
            return (sbyte)retval;
        }

        /// <summary>
        /// Reads 1 to 8 bits into a byte
        /// </summary>
        public byte ReadByte(int numberOfBits)
        {
            NetworkException.Assert(numberOfBits > 0 && numberOfBits <= 8, "ReadByte(bits) can only read between 1 and 8 bits");
            byte retval = BitWriter.ReadByte(data, numberOfBits, readPosition);
            readPosition += numberOfBits;
            return retval;
        }

        /// <summary>
        /// Reads bytes written with WriteBytes(byte[], true).
        /// </summary>
        /// <remarks>Will not work for byte arrays larger then 131072 elements.</remarks>
        /// <returns></returns>
        public byte[] ReadBytes()
        {
            int size = ReadInt32();
            if (size < 0)
            {
                RPCLogger.Trace("discarding read -- illegal size");
                return null;
            }
            if (size > 131072)
            {
                RPCLogger.Trace("discarding read -- too large");
                return null;
            }

            byte[] raw = ReadBytes(size);
            if (raw != null)
                return raw;

            return null;
        }

        /// <summary>
        /// Reads the specified number of bytes
        /// </summary>
        public byte[] ReadBytes(int numberOfBytes)
        {
            NetworkException.Assert(bitLength - readPosition + 7 >= (numberOfBytes * 8), READ_OVERFLOW_ERROR);

            byte[] retval = new byte[numberOfBytes];
            BitWriter.ReadBytes(data, numberOfBytes, readPosition, retval, 0);
            readPosition += (8 * numberOfBytes);
            return retval;
        }

        /// <summary>
        /// Reads the specified number of bytes and returns true for success
        /// </summary>
        public bool ReadBytes(int numberOfBytes, out byte[] result)
        {
            if (bitLength - readPosition + 7 < (numberOfBytes * 8))
            {
                result = null;
                return false;
            }

            result = new byte[numberOfBytes];
            BitWriter.ReadBytes(data, numberOfBytes, readPosition, result, 0);
            readPosition += (8 * numberOfBytes);
            return true;
        }

        /// <summary>
        /// Reads the specified number of bytes into a preallocated array
        /// </summary>
        /// <param name="into">The destination array</param>
        /// <param name="offset">The offset where to start writing in the destination array</param>
        /// <param name="numberOfBytes">The number of bytes to read</param>
        public void ReadBytes(byte[] into, int offset, int numberOfBytes)
        {
            NetworkException.Assert(bitLength - readPosition + 7 >= (numberOfBytes * 8), READ_OVERFLOW_ERROR);
            NetworkException.Assert(offset + numberOfBytes <= into.Length);

            BitWriter.ReadBytes(data, numberOfBytes, readPosition, into, offset);
            readPosition += (8 * numberOfBytes);
            return;
        }

        /// <summary>
        /// Reads the specified number of bits into a preallocated array
        /// </summary>
        /// <param name="into">The destination array</param>
        /// <param name="offset">The offset where to start writing in the destination array</param>
        /// <param name="numberOfBits">The number of bits to read</param>
        public void ReadBits(byte[] into, int offset, int numberOfBits)
        {
            NetworkException.Assert(bitLength - readPosition >= numberOfBits, READ_OVERFLOW_ERROR);
            NetworkException.Assert(offset + NetUtility.BytesToHoldBits(numberOfBits) <= into.Length);

            int numberOfWholeBytes = numberOfBits / 8;
            int extraBits = numberOfBits - (numberOfWholeBytes * 8);

            BitWriter.ReadBytes(data, numberOfWholeBytes, readPosition, into, offset);
            readPosition += (8 * numberOfWholeBytes);

            if (extraBits > 0)
                into[offset + numberOfWholeBytes] = ReadByte(extraBits);

            return;
        }

        /// <summary>
        /// Reads a 16 bit signed integer written using Write(Int16)
        /// </summary>
        public Int16 ReadInt16()
        {
            NetworkException.Assert(bitLength - readPosition >= 16, READ_OVERFLOW_ERROR);
            uint retval = BitWriter.ReadUInt16(data, 16, readPosition);
            readPosition += 16;
            return (short)retval;
        }

        /// <summary>
        /// Reads a 16 bit unsigned integer written using Write(UInt16)
        /// </summary>
        public UInt16 ReadUInt16()
        {
            NetworkException.Assert(bitLength - readPosition >= 16, READ_OVERFLOW_ERROR);
            uint retval = BitWriter.ReadUInt16(data, 16, readPosition);
            readPosition += 16;
            return (ushort)retval;
        }

        /// <summary>
        /// Reads a 32 bit signed integer written using Write(Int32)
        /// </summary>
        public Int32 ReadInt32()
        {
            NetworkException.Assert(bitLength - readPosition >= 32, READ_OVERFLOW_ERROR);
            uint retval = BitWriter.ReadUInt32(data, 32, readPosition);
            readPosition += 32;
            return (Int32)retval;
        }

        /// <summary>
        /// Reads a 32 bit signed integer written using Write(Int32)
        /// </summary>
        public bool ReadInt32(out Int32 result)
        {
            if (bitLength - readPosition < 32)
            {
                result = 0;
                return false;
            }

            result = (Int32)BitWriter.ReadUInt32(data, 32, readPosition);
            readPosition += 32;
            return true;
        }

        /// <summary>
        /// Reads a signed integer stored in 1 to 32 bits, written using Write(Int32, Int32)
        /// </summary>
        public Int32 ReadInt32(int numberOfBits)
        {
            NetworkException.Assert(numberOfBits > 0 && numberOfBits <= 32, "ReadInt32(bits) can only read between 1 and 32 bits");
            NetworkException.Assert(bitLength - readPosition >= numberOfBits, READ_OVERFLOW_ERROR);

            uint retval = BitWriter.ReadUInt32(data, numberOfBits, readPosition);
            readPosition += numberOfBits;

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
        /// Reads an 32 bit unsigned integer written using Write(UInt32)
        /// </summary>
        public UInt32 ReadUInt32()
        {
            NetworkException.Assert(bitLength - readPosition >= 32, READ_OVERFLOW_ERROR);
            uint retval = BitWriter.ReadUInt32(data, 32, readPosition);
            readPosition += 32;
            return retval;
        }

        /// <summary>
        /// Reads an 32 bit unsigned integer written using Write(UInt32) and returns true for success
        /// </summary>
        public bool ReadUInt32(out UInt32 result)
        {
            if (bitLength - readPosition < 32)
            {
                result = 0;
                return false;
            }
            result = BitWriter.ReadUInt32(data, 32, readPosition);
            readPosition += 32;
            return true;
        }

        /// <summary>
        /// Reads an unsigned integer stored in 1 to 32 bits, written using Write(UInt32, Int32)
        /// </summary>
        public UInt32 ReadUInt32(int numberOfBits)
        {
            NetworkException.Assert(numberOfBits > 0 && numberOfBits <= 32, "ReadUInt32(bits) can only read between 1 and 32 bits");

            UInt32 retval = BitWriter.ReadUInt32(data, numberOfBits, readPosition);
            readPosition += numberOfBits;
            return retval;
        }

        /// <summary>
        /// Reads a 64 bit unsigned integer written using Write(UInt64)
        /// </summary>
        public UInt64 ReadUInt64()
        {
            NetworkException.Assert(bitLength - readPosition >= 64, READ_OVERFLOW_ERROR);

            ulong low = BitWriter.ReadUInt32(data, 32, readPosition);
            readPosition += 32;
            ulong high = BitWriter.ReadUInt32(data, 32, readPosition);

            ulong retval = low + (high << 32);

            readPosition += 32;
            return retval;
        }

        /// <summary>
        /// Reads a 64 bit signed integer written using Write(Int64)
        /// </summary>
        public Int64 ReadInt64()
        {
            NetworkException.Assert(bitLength - readPosition >= 64, READ_OVERFLOW_ERROR);
            unchecked
            {
                ulong retval = ReadUInt64();
                long longRetval = (long)retval;
                return longRetval;
            }
        }

        /// <summary>
        /// Reads an unsigned integer stored in 1 to 64 bits, written using Write(UInt64, Int32)
        /// </summary>
        public UInt64 ReadUInt64(int numberOfBits)
        {
            NetworkException.Assert(numberOfBits > 0 && numberOfBits <= 64, "ReadUInt64(bits) can only read between 1 and 64 bits");
            NetworkException.Assert(bitLength - readPosition >= numberOfBits, READ_OVERFLOW_ERROR);

            ulong retval;
            if (numberOfBits <= 32)
                retval = (ulong)BitWriter.ReadUInt32(data, numberOfBits, readPosition);
            else
            {
                retval = BitWriter.ReadUInt32(data, 32, readPosition);
                retval |= (UInt64)BitWriter.ReadUInt32(data, numberOfBits - 32, readPosition + 32) << 32;
            }
            readPosition += numberOfBits;
            return retval;
        }

        /// <summary>
        /// Reads a signed integer stored in 1 to 64 bits, written using Write(Int64, Int32)
        /// </summary>
        public Int64 ReadInt64(int numberOfBits)
        {
            NetworkException.Assert(((numberOfBits > 0) && (numberOfBits <= 64)), "ReadInt64(bits) can only read between 1 and 64 bits");
            return (long)ReadUInt64(numberOfBits);
        }

        /// <summary>
        /// Reads a 32 bit floating point value written using Write(Single)
        /// </summary>
        public float ReadFloat()
        {
            return ReadSingle();
        }

        /// <summary>
        /// Reads a 32 bit floating point value written using Write(Single)
        /// </summary>
        public float ReadSingle()
        {
            NetworkException.Assert(bitLength - readPosition >= 32, READ_OVERFLOW_ERROR);

            if ((readPosition & 7) == 0) // read directly
            {
                float retval = BitConverter.ToSingle(data, readPosition >> 3);
                readPosition += 32;
                return retval;
            }

            byte[] bytes = ReadBytes(4);
            return BitConverter.ToSingle(bytes, 0);
        }

        /// <summary>
        /// Reads a 32 bit floating point value written using Write(Single)
        /// </summary>
        public bool ReadSingle(out float result)
        {
            if (bitLength - readPosition < 32)
            {
                result = 0.0f;
                return false;
            }

            if ((readPosition & 7) == 0) // read directly
            {
                result = BitConverter.ToSingle(data, readPosition >> 3);
                readPosition += 32;
                return true;
            }

            byte[] bytes = ReadBytes(4);
            result = BitConverter.ToSingle(bytes, 0);
            return true;
        }

        /// <summary>
        /// Reads a 64 bit floating point value written using Write(Double)
        /// </summary>
        public double ReadDouble()
        {
            NetworkException.Assert(bitLength - readPosition >= 64, READ_OVERFLOW_ERROR);

            if ((readPosition & 7) == 0) // read directly
            {
                // read directly
                double retval = BitConverter.ToDouble(data, readPosition >> 3);
                readPosition += 64;
                return retval;
            }

            byte[] bytes = ReadBytes(8);
            return BitConverter.ToDouble(bytes, 0);
        }

        //
        // Variable Bit Count
        //
        /// <summary>
        /// Reads a variable sized UInt32 written using WriteVariableUInt32()
        /// </summary>
        public uint ReadVariableUInt32()
        {
            int num1 = 0;
            int num2 = 0;
            while (bitLength - readPosition >= 8)
            {
                byte num3 = this.ReadByte();
                num1 |= (num3 & 0x7f) << num2;
                num2 += 7;
                if ((num3 & 0x80) == 0)
                    return (uint)num1;
            }

            // ouch; failed to find enough bytes; malformed variable length number?
            return (uint)num1;
        }

        /// <summary>
        /// Reads a variable sized UInt32 written using WriteVariableUInt32() and returns true for success
        /// </summary>
        public bool ReadVariableUInt32(out uint result)
        {
            int num1 = 0;
            int num2 = 0;
            while (bitLength - readPosition >= 8)
            {
                byte num3;
                if (ReadByte(out num3) == false)
                {
                    result = 0;
                    return false;
                }
                num1 |= (num3 & 0x7f) << num2;
                num2 += 7;
                if ((num3 & 0x80) == 0)
                {
                    result = (uint)num1;
                    return true;
                }
            }
            result = (uint)num1;
            return false;
        }

        /// <summary>
        /// Reads a variable sized Int32 written using WriteVariableInt32()
        /// </summary>
        public int ReadVariableInt32()
        {
            uint n = ReadVariableUInt32();
            return (int)(n >> 1) ^ -(int)(n & 1); // decode zigzag
        }

        /// <summary>
        /// Reads a variable sized Int64 written using WriteVariableInt64()
        /// </summary>
        public Int64 ReadVariableInt64()
        {
            UInt64 n = ReadVariableUInt64();
            return (Int64)(n >> 1) ^ -(long)(n & 1); // decode zigzag
        }

        /// <summary>
        /// Reads a variable sized UInt32 written using WriteVariableInt64()
        /// </summary>
        public UInt64 ReadVariableUInt64()
        {
            UInt64 num1 = 0;
            int num2 = 0;
            while (bitLength - readPosition >= 8)
            {
                //if (num2 == 0x23)
                //	throw new FormatException("Bad 7-bit encoded integer");

                byte num3 = this.ReadByte();
                num1 |= ((UInt64)num3 & 0x7f) << num2;
                num2 += 7;
                if ((num3 & 0x80) == 0)
                    return num1;
            }

            // ouch; failed to find enough bytes; malformed variable length number?
            return num1;
        }

        /// <summary>
        /// Reads a 32 bit floating point value written using WriteSignedSingle()
        /// </summary>
        /// <param name="numberOfBits">The number of bits used when writing the value</param>
        /// <returns>A floating point value larger or equal to -1 and smaller or equal to 1</returns>
        public float ReadSignedSingle(int numberOfBits)
        {
            uint encodedVal = ReadUInt32(numberOfBits);
            int maxVal = (1 << numberOfBits) - 1;
            return ((float)(encodedVal + 1) / (float)(maxVal + 1) - 0.5f) * 2.0f;
        }

        /// <summary>
        /// Reads a 32 bit floating point value written using WriteUnitSingle()
        /// </summary>
        /// <param name="numberOfBits">The number of bits used when writing the value</param>
        /// <returns>A floating point value larger or equal to 0 and smaller or equal to 1</returns>
        public float ReadUnitSingle(int numberOfBits)
        {
            uint encodedVal = ReadUInt32(numberOfBits);
            int maxVal = (1 << numberOfBits) - 1;
            return (float)(encodedVal + 1) / (float)(maxVal + 1);
        }

        /// <summary>
        /// Reads a 32 bit floating point value written using WriteRangedSingle()
        /// </summary>
        /// <param name="min">The minimum value used when writing the value</param>
        /// <param name="max">The maximum value used when writing the value</param>
        /// <param name="numberOfBits">The number of bits used when writing the value</param>
        /// <returns>A floating point value larger or equal to MIN and smaller or equal to MAX</returns>
        public float ReadRangedSingle(float min, float max, int numberOfBits)
        {
            float range = max - min;
            int maxVal = (1 << numberOfBits) - 1;
            float encodedVal = (float)ReadUInt32(numberOfBits);
            float unit = encodedVal / (float)maxVal;
            return min + (unit * range);
        }

        /// <summary>
        /// Reads a 32 bit integer value written using WriteRangedInteger()
        /// </summary>
        /// <param name="min">The minimum value used when writing the value</param>
        /// <param name="max">The maximum value used when writing the value</param>
        /// <returns>A signed integer value larger or equal to MIN and smaller or equal to MAX</returns>
        public int ReadRangedInteger(int min, int max)
        {
            uint range = (uint)(max - min);
            int numBits = NetUtility.BitsToHoldUInt(range);

            uint rvalue = ReadUInt32(numBits);
            return (int)(min + rvalue);
        }

        /// <summary>
        /// Reads a 64 bit integer value written using WriteRangedInteger() (64 version)
        /// </summary>
        /// <param name="min">The minimum value used when writing the value</param>
        /// <param name="max">The maximum value used when writing the value</param>
        /// <returns>A signed integer value larger or equal to MIN and smaller or equal to MAX</returns>
        public long ReadRangedInteger(long min, long max)
        {
            ulong range = (ulong)(max - min);
            int numBits = NetUtility.BitsToHoldUInt64(range);

            ulong rvalue = ReadUInt64(numBits);
            return min + (long)rvalue;
        }

        /// <summary>
        /// Reads a string written using Write(string)
        /// </summary>
        public string ReadString()
        {
            int byteLen = (int)ReadVariableUInt32();

            if (byteLen <= 0)
                return String.Empty;

            if ((ulong)(bitLength - readPosition) < ((ulong)byteLen * 8))
            {
                // not enough data
#if DEBUG
                throw new NetworkException(READ_OVERFLOW_ERROR);
#else
                readPosition = bitLength;
				return null; // unfortunate; but we need to protect against DDOS
#endif
            }

            if ((readPosition & 7) == 0)
            {
                // read directly
                string retval = System.Text.Encoding.UTF8.GetString(data, readPosition >> 3, byteLen);
                readPosition += (8 * byteLen);
                return retval;
            }

            byte[] bytes = ReadBytes(byteLen);
            return System.Text.Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// Reads a string written using Write(string) and returns true for success
        /// </summary>
        public bool ReadString(out string result)
        {
            uint byteLen;
            if (ReadVariableUInt32(out byteLen) == false)
            {
                result = String.Empty;
                return false;
            }

            if (byteLen <= 0)
            {
                result = String.Empty;
                return true;
            }

            if (bitLength - readPosition < (byteLen * 8))
            {
                result = String.Empty;
                return false;
            }

            if ((readPosition & 7) == 0)
            {
                // read directly
                result = System.Text.Encoding.UTF8.GetString(data, readPosition >> 3, (int)byteLen);
                readPosition += (8 * (int)byteLen);
                return true;
            }

            byte[] bytes;
            if (ReadBytes((int)byteLen, out bytes) == false)
            {
                result = String.Empty;
                return false;
            }

            result = System.Text.Encoding.UTF8.GetString(bytes, 0, bytes.Length);
            return true;
        }

        /// <summary>
        /// Reads a Guid written using Write(Guid)
        /// </summary>
        /// <returns></returns>
        public Guid ReadGuid()
        {
            byte len = ReadByte();
            byte[] guidBytes = ReadBytes(len);

            return new Guid(guidBytes);
        }

        /// <summary>
        /// Reads a value, in local time comparable to NetTime.Now, written using WriteTime() for the connection supplied
        /// </summary>
        public double ReadTime(Connection connection, bool highPrecision)
        {
            double remoteTime = (highPrecision ? ReadDouble() : (double)ReadSingle());

            if (connection == null)
                throw new NetworkException("Cannot call ReadTime() on message without a connected sender (ie. unconnected messages)");

            // lets bypass NetConnection.GetLocalTime for speed
            return remoteTime - connection.remoteTimeOffset;
        }

        /// <summary>
        /// Reads a stored IPv4 endpoint description
        /// </summary>
        public IPEndPoint ReadIPEndPoint()
        {
            byte len = ReadByte();
            byte[] addressBytes = ReadBytes(len);
            int port = (int)ReadUInt16();

            IPAddress address = new IPAddress(addressBytes);
            return new IPEndPoint(address, port);
        }

        /// <summary>
        /// Pads data with enough bits to reach a full byte. Decreases cpu usage for subsequent byte writes.
        /// </summary>
        public void SkipPadBits()
        {
            readPosition = ((readPosition + 7) >> 3) * 8;
        }

        /// <summary>
        /// Pads data with enough bits to reach a full byte. Decreases cpu usage for subsequent byte writes.
        /// </summary>
        public void ReadPadBits()
        {
            readPosition = ((readPosition + 7) >> 3) * 8;
        }

        /// <summary>
        /// Pads data with the specified number of bits.
        /// </summary>
        public void SkipPadBits(int numberOfBits)
        {
            readPosition += numberOfBits;
        }
    } // public partial class MessageBuffer
} // namespace TridentFramework.RPC.Net.Message
