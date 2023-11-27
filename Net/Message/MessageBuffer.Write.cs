//#define UNSAFE
//#define BIGENDIAN
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
using System.Net;
using System.Text;
using System.Runtime.InteropServices;

using TridentFramework.RPC.Net.PeerConnection;

namespace TridentFramework.RPC.Net.Message
{
    /// <summary>
    /// Utility struct for writing Singles
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct SingleUIntUnion
    {
        /// <summary>
        /// Value as a 32 bit float
        /// </summary>
        [FieldOffset(0)]
        public float SingleValue;

        /// <summary>
        /// Value as an unsigned 32 bit integer
        /// </summary>
        [FieldOffset(0)]
        public uint UIntValue;
    } // public struct SingleUIntUnion

    /// <summary>
    /// Base class for <see cref="IncomingMessage"/> and <see cref="OutgoingMessage"/>.
    /// </summary>
    public partial class MessageBuffer
    {
        /*
        ** Methods
        */

        /// <summary>
        /// Ensures the buffer can hold this number of bits
        /// </summary>
        public void EnsureBufferSize(int numberOfBits)
        {
            int byteLen = ((numberOfBits + 7) >> 3);
            if (data == null)
            {
                data = new byte[byteLen + OverAllocateAmount];
                return;
            }
            if (data.Length < byteLen)
                Array.Resize<byte>(ref data, byteLen + OverAllocateAmount);
            return;
        }

        /// <summary>
        /// Ensures the buffer can hold this number of bits
        /// </summary>
        internal void InternalEnsureBufferSize(int numberOfBits)
        {
            int byteLen = ((numberOfBits + 7) >> 3);
            if (data == null)
            {
                data = new byte[byteLen];
                return;
            }
            if (data.Length < byteLen)
                Array.Resize<byte>(ref data, byteLen);
            return;
        }

        /// <summary>
        /// Writes a boolean value using 1 bit
        /// </summary>
        public void Write(bool value)
        {
            EnsureBufferSize(bitLength + 1);
            BitWriter.WriteByte((value ? (byte)1 : (byte)0), 1, data, bitLength);
            bitLength += 1;
        }

        /// <summary>
        /// Write a byte
        /// </summary>
        public void Write(byte source)
        {
            EnsureBufferSize(bitLength + 8);
            BitWriter.WriteByte(source, 8, data, bitLength);
            bitLength += 8;
        }

        /// <summary>
        /// Writes encapsulation for <see cref="MessageToTransmit"/> byte.
        /// </summary>
        /// <param name="mtt"></param>
        public void Write(MessageToTransmit mtt)
        {
            Write((byte)mtt);
        }

        /// <summary>
        /// Writes a byte at a given offset in the buffer
        /// </summary>
        public void WriteAt(Int32 offset, byte source)
        {
            int newBitLength = Math.Max(bitLength, offset + 8);
            EnsureBufferSize(newBitLength);
            BitWriter.WriteByte((byte)source, 8, data, offset);
            bitLength = newBitLength;
        }

        /// <summary>
        /// Writes a signed byte
        /// </summary>
        public void Write(sbyte source)
        {
            EnsureBufferSize(bitLength + 8);
            BitWriter.WriteByte((byte)source, 8, data, bitLength);
            bitLength += 8;
        }

        /// <summary>
        /// Writes 1 to 8 bits of a byte
        /// </summary>
        public void Write(byte source, int numberOfBits)
        {
            NetworkException.Assert((numberOfBits > 0 && numberOfBits <= 8), "Write(byte, numberOfBits) can only write between 1 and 8 bits");
            EnsureBufferSize(bitLength + numberOfBits);
            BitWriter.WriteByte(source, numberOfBits, data, bitLength);
            bitLength += numberOfBits;
        }

        /// <summary>
        /// Writes all bytes in an array
        /// </summary>
        public void Write(byte[] source)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            int bits = source.Length * 8;
            EnsureBufferSize(bitLength + bits);
            BitWriter.WriteBytes(source, 0, source.Length, data, bitLength);
            bitLength += bits;
        }

        /// <summary>
        /// Writes all bytes in an array
        /// </summary>
        public void Write(byte[] source, bool includeLength = false)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            if (includeLength)
                this.Write(source.Length);

            int bits = source.Length * 8;
            EnsureBufferSize(bitLength + bits);
            BitWriter.WriteBytes(source, 0, source.Length, Data, bitLength);
            bitLength += bits;
        }

        /// <summary>
        /// Writes the specified number of bytes from an array
        /// </summary>
        public void Write(byte[] source, int offsetInBytes, int numberOfBytes)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            int bits = numberOfBytes * 8;
            EnsureBufferSize(bitLength + bits);
            BitWriter.WriteBytes(source, offsetInBytes, numberOfBytes, data, bitLength);
            bitLength += bits;
        }

        /// <summary>
        /// Writes an unsigned 16 bit integer
        /// </summary>
        /// <param name="source"></param>
        public void Write(UInt16 source)
        {
            EnsureBufferSize(bitLength + 16);
            BitWriter.WriteUInt16(source, 16, data, bitLength);
            bitLength += 16;
        }

        /// <summary>
        /// Writes a 16 bit unsigned integer at a given offset in the buffer
        /// </summary>
        public void WriteAt(Int32 offset, UInt16 source)
        {
            int newBitLength = Math.Max(bitLength, offset + 16);
            EnsureBufferSize(newBitLength);
            BitWriter.WriteUInt16(source, 16, data, offset);
            bitLength = newBitLength;
        }

        /// <summary>
        /// Writes an unsigned integer using 1 to 16 bits
        /// </summary>
        public void Write(UInt16 source, int numberOfBits)
        {
            NetworkException.Assert((numberOfBits > 0 && numberOfBits <= 16), "Write(ushort, numberOfBits) can only write between 1 and 16 bits");
            EnsureBufferSize(bitLength + numberOfBits);
            BitWriter.WriteUInt16(source, numberOfBits, data, bitLength);
            bitLength += numberOfBits;
        }

        /// <summary>
        /// Writes a signed 16 bit integer
        /// </summary>
        public void Write(Int16 source)
        {
            EnsureBufferSize(bitLength + 16);
            BitWriter.WriteUInt16((ushort)source, 16, data, bitLength);
            bitLength += 16;
        }

        /// <summary>
        /// Writes a 16 bit signed integer at a given offset in the buffer
        /// </summary>
        public void WriteAt(Int32 offset, Int16 source)
        {
            int newBitLength = Math.Max(bitLength, offset + 16);
            EnsureBufferSize(newBitLength);
            BitWriter.WriteUInt16((ushort)source, 16, data, offset);
            bitLength = newBitLength;
        }

#if UNSAFE
		/// <summary>
		/// Writes a 32 bit signed integer
		/// </summary>
		public unsafe void Write(Int32 source)
		{
			EnsureBufferSize(bitLength + 32);

			// can write fast?
			if (bitLength % 8 == 0)
			{
				fixed (byte* numRef = &Data[bitLength / 8])
				{
					*((int*)numRef) = source;
				}
			}
			else
				BitWriter.WriteUInt32((UInt32)source, 32, Data, bitLength);
			bitLength += 32;
		}
#else

        /// <summary>
        /// Writes a 32 bit signed integer
        /// </summary>
        public void Write(Int32 source)
        {
            EnsureBufferSize(bitLength + 32);
            BitWriter.WriteUInt32((UInt32)source, 32, data, bitLength);
            bitLength += 32;
        }

#endif

        /// <summary>
        /// Writes a 32 bit signed integer at a given offset in the buffer
        /// </summary>
        public void WriteAt(Int32 offset, Int32 source)
        {
            int newBitLength = Math.Max(bitLength, offset + 32);
            EnsureBufferSize(newBitLength);
            BitWriter.WriteUInt32((UInt32)source, 32, data, offset);
            bitLength = newBitLength;
        }

#if UNSAFE
		/// <summary>
		/// Writes a 32 bit unsigned integer
		/// </summary>
		public unsafe void Write(UInt32 source)
		{
			EnsureBufferSize(bitLength + 32);

			// can write fast?
			if (bitLength % 8 == 0)
			{
				fixed (byte* numRef = &Data[bitLength / 8])
				{
					*((uint*)numRef) = source;
				}
			}
			else
				BitWriter.WriteUInt32(source, 32, Data, bitLength);

			bitLength += 32;
		}
#else

        /// <summary>
        /// Writes a 32 bit unsigned integer
        /// </summary>
		public void Write(UInt32 source)
        {
            EnsureBufferSize(bitLength + 32);
            BitWriter.WriteUInt32(source, 32, data, bitLength);
            bitLength += 32;
        }

#endif

        /// <summary>
        /// Writes a 32 bit unsigned integer at a given offset in the buffer
        /// </summary>
        public void WriteAt(Int32 offset, UInt32 source)
        {
            int newBitLength = Math.Max(bitLength, offset + 32);
            EnsureBufferSize(newBitLength);
            BitWriter.WriteUInt32(source, 32, data, offset);
            bitLength = newBitLength;
        }

        /// <summary>
        /// Writes a 32 bit signed integer
        /// </summary>
        public void Write(UInt32 source, int numberOfBits)
        {
            NetworkException.Assert((numberOfBits > 0 && numberOfBits <= 32), "Write(uint, numberOfBits) can only write between 1 and 32 bits");
            EnsureBufferSize(bitLength + numberOfBits);
            BitWriter.WriteUInt32(source, numberOfBits, data, bitLength);
            bitLength += numberOfBits;
        }

        /// <summary>
        /// Writes a signed integer using 1 to 32 bits
        /// </summary>
        public void Write(Int32 source, int numberOfBits)
        {
            NetworkException.Assert((numberOfBits > 0 && numberOfBits <= 32), "Write(int, numberOfBits) can only write between 1 and 32 bits");
            EnsureBufferSize(bitLength + numberOfBits);

            if (numberOfBits != 32)
            {
                // make first bit sign
                int signBit = 1 << (numberOfBits - 1);
                if (source < 0)
                    source = (-source - 1) | signBit;
                else
                    source &= (~signBit);
            }

            BitWriter.WriteUInt32((uint)source, numberOfBits, data, bitLength);

            bitLength += numberOfBits;
        }

        /// <summary>
        /// Writes a 64 bit unsigned integer
        /// </summary>
        public void Write(UInt64 source)
        {
            EnsureBufferSize(bitLength + 64);
            BitWriter.WriteUInt64(source, 64, data, bitLength);
            bitLength += 64;
        }

        /// <summary>
        /// Writes a 64 bit unsigned integer at a given offset in the buffer
        /// </summary>
        public void WriteAt(Int32 offset, UInt64 source)
        {
            int newBitLength = Math.Max(bitLength, offset + 64);
            EnsureBufferSize(newBitLength);
            BitWriter.WriteUInt64(source, 64, data, offset);
            bitLength = newBitLength;
        }

        /// <summary>
        /// Writes an unsigned integer using 1 to 64 bits
        /// </summary>
        public void Write(UInt64 source, int numberOfBits)
        {
            EnsureBufferSize(bitLength + numberOfBits);
            BitWriter.WriteUInt64(source, numberOfBits, data, bitLength);
            bitLength += numberOfBits;
        }

        /// <summary>
        /// Writes a 64 bit signed integer
        /// </summary>
        public void Write(Int64 source)
        {
            EnsureBufferSize(bitLength + 64);
            ulong usource = (ulong)source;
            BitWriter.WriteUInt64(usource, 64, data, bitLength);
            bitLength += 64;
        }

        /// <summary>
        /// Writes a signed integer using 1 to 64 bits
        /// </summary>
        public void Write(Int64 source, int numberOfBits)
        {
            EnsureBufferSize(bitLength + numberOfBits);
            ulong usource = (ulong)source;
            BitWriter.WriteUInt64(usource, numberOfBits, data, bitLength);
            bitLength += numberOfBits;
        }

        //
        // Floating point
        //
#if UNSAFE
		/// <summary>
		/// Writes a 32 bit floating point value
		/// </summary>
		public unsafe void Write(float source)
		{
			uint val = *((uint*)&source);
#if BIGENDIAN
				val = NetUtility.SwapByteOrder(val);
#endif
			Write(val);
		}
#else

        /// <summary>
        /// Writes a 32 bit floating point value
        /// </summary>
        public void Write(float source)
        {
            // Use union to avoid BitConverter.GetBytes() which allocates memory on the heap
            SingleUIntUnion su;
            su.UIntValue = 0; // must initialize every member of the union to avoid warning
            su.SingleValue = source;

#if BIGENDIAN
			// swap byte order
			su.UIntValue = NetUtility.SwapByteOrder(su.UIntValue);
#endif
            Write(su.UIntValue);
        }

#endif

#if UNSAFE
		/// <summary>
		/// Writes a 64 bit floating point value
		/// </summary>
		public unsafe void Write(double source)
		{
			ulong val = *((ulong*)&source);
#if BIGENDIAN
			val = NetUtility.SwapByteOrder(val);
#endif
			Write(val);
		}
#else

        /// <summary>
        /// Writes a 64 bit floating point value
        /// </summary>
        public void Write(double source)
        {
            byte[] val = BitConverter.GetBytes(source);
#if BIGENDIAN
			// 0 1 2 3   4 5 6 7

			// swap byte order
			byte tmp = val[7];
			val[7] = val[0];
			val[0] = tmp;

			tmp = val[6];
			val[6] = val[1];
			val[1] = tmp;

			tmp = val[5];
			val[5] = val[2];
			val[2] = tmp;

			tmp = val[4];
			val[4] = val[3];
			val[3] = tmp;
#endif
            Write(val);
        }

#endif

        //
        // Variable Bit Count
        //
        /// <summary>
        /// Write Base128 encoded variable sized unsigned integer of up to 32 bits
        /// </summary>
        /// <returns>number of bytes written</returns>
        public int WriteVariableUInt32(uint value)
        {
            int retval = 1;
            uint num1 = (uint)value;
            while (num1 >= 0x80)
            {
                this.Write((byte)(num1 | 0x80));
                num1 = num1 >> 7;
                retval++;
            }
            this.Write((byte)num1);
            return retval;
        }

        /// <summary>
        /// Write Base128 encoded variable sized signed integer of up to 32 bits
        /// </summary>
        /// <returns>number of bytes written</returns>
        public int WriteVariableInt32(int value)
        {
            uint zigzag = (uint)(value << 1) ^ (uint)(value >> 31);
            return WriteVariableUInt32(zigzag);
        }

        /// <summary>
        /// Write Base128 encoded variable sized signed integer of up to 64 bits
        /// </summary>
        /// <returns>number of bytes written</returns>
        public int WriteVariableInt64(Int64 value)
        {
            ulong zigzag = (ulong)(value << 1) ^ (ulong)(value >> 63);
            return WriteVariableUInt64(zigzag);
        }

        /// <summary>
        /// Write Base128 encoded variable sized unsigned integer of up to 64 bits
        /// </summary>
        /// <returns>number of bytes written</returns>
        public int WriteVariableUInt64(UInt64 value)
        {
            int retval = 1;
            UInt64 num1 = (UInt64)value;
            while (num1 >= 0x80)
            {
                this.Write((byte)(num1 | 0x80));
                num1 = num1 >> 7;
                retval++;
            }
            this.Write((byte)num1);
            return retval;
        }

        /// <summary>
        /// Compress (lossy) a float in the range -1..1 using numberOfBits bits
        /// </summary>
        public void WriteSignedSingle(float value, int numberOfBits)
        {
            NetworkException.Assert(((value >= -1.0) && (value <= 1.0)), " WriteSignedSingle() must be passed a float in the range -1 to 1; val is " + value);

            float unit = (value + 1.0f) * 0.5f;
            int maxVal = (1 << numberOfBits) - 1;
            uint writeVal = (uint)(unit * (float)maxVal);

            Write(writeVal, numberOfBits);
        }

        /// <summary>
        /// Compress (lossy) a float in the range 0..1 using numberOfBits bits
        /// </summary>
        public void WriteUnitSingle(float value, int numberOfBits)
        {
            NetworkException.Assert(((value >= 0.0) && (value <= 1.0)), " WriteUnitSingle() must be passed a float in the range 0 to 1; val is " + value);

            int maxValue = (1 << numberOfBits) - 1;
            uint writeVal = (uint)(value * (float)maxValue);

            Write(writeVal, numberOfBits);
        }

        /// <summary>
        /// Compress a float within a specified range using a certain number of bits
        /// </summary>
        public void WriteRangedSingle(float value, float min, float max, int numberOfBits)
        {
            NetworkException.Assert(((value >= min) && (value <= max)), " WriteRangedSingle() must be passed a float in the range MIN to MAX; val is " + value);

            float range = max - min;
            float unit = ((value - min) / range);
            int maxVal = (1 << numberOfBits) - 1;
            Write((UInt32)((float)maxVal * unit), numberOfBits);
        }

        /// <summary>
        /// Writes an integer with the least amount of bits need for the specified range
        /// Returns number of bits written
        /// </summary>
        public int WriteRangedInteger(int min, int max, int value)
        {
            NetworkException.Assert(value >= min && value <= max, "Value not within min/max range!");

            uint range = (uint)(max - min);
            int numBits = NetUtility.BitsToHoldUInt(range);

            uint rvalue = (uint)(value - min);
            Write(rvalue, numBits);

            return numBits;
        }

        /// <summary>
        /// Writes an integer with the least amount of bits need for the specified range
        /// Returns number of bits written
        /// </summary>
        public int WriteRangedInteger(long min, long max, long value)
        {
            NetworkException.Assert(value >= min && value <= max, "Value not within min/max range!");

            ulong range = (ulong)(max - min);
            int numBits = NetUtility.BitsToHoldUInt64(range);

            ulong rvalue = (ulong)(value - min);
            Write(rvalue, numBits);

            return numBits;
        }

        /// <summary>
        /// Write a string
        /// </summary>
        public void Write(string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                WriteVariableUInt32(0);
                return;
            }

            byte[] bytes = Encoding.UTF8.GetBytes(source);
            EnsureBufferSize(bitLength + 8 + (bytes.Length * 8));
            WriteVariableUInt32((uint)bytes.Length);
            Write(bytes);
        }

        /// <summary>
        /// Writes a guid
        /// </summary>
        /// <param name="source"></param>
        public void Write(Guid source)
        {
            byte[] bytes = source.ToByteArray();
            Write((byte)bytes.Length);
            Write(bytes);
        }

        /// <summary>
        /// Writes an endpoint description
        /// </summary>
        public void Write(IPEndPoint endPoint)
        {
            byte[] bytes = endPoint.Address.GetAddressBytes();
            Write((byte)bytes.Length);
            Write(bytes);
            Write((ushort)endPoint.Port);
        }

        /// <summary>
        /// Writes the current local time to a message; readable (and convertable to local time) by the remote host using ReadTime()
        /// </summary>
        public void WriteTime(bool highPrecision)
        {
            double localTime = NetTime.Now;
            if (highPrecision)
                Write(localTime);
            else
                Write((float)localTime);
        }

        /// <summary>
        /// Writes a local timestamp to a message; readable (and convertable to local time) by the remote host using ReadTime()
        /// </summary>
        public void WriteTime(double localTime, bool highPrecision)
        {
            if (highPrecision)
                Write(localTime);
            else
                Write((float)localTime);
        }

        /// <summary>
        /// Pads data with enough bits to reach a full byte. Decreases cpu usage for subsequent byte writes.
        /// </summary>
        public void WritePadBits()
        {
            bitLength = ((bitLength + 7) >> 3) * 8;
            EnsureBufferSize(bitLength);
        }

        /// <summary>
        /// Pads data with the specified number of bits.
        /// </summary>
        public void WritePadBits(int numberOfBits)
        {
            bitLength += numberOfBits;
            EnsureBufferSize(bitLength);
        }

        /// <summary>
        /// Append all the bits of message to this message
        /// </summary>
        public void Write(MessageBuffer buffer)
        {
            EnsureBufferSize(bitLength + (buffer.LengthBytes * 8));

            Write(buffer.data, 0, buffer.LengthBytes);

            // did we write excessive bits?
            int bitsInLastByte = (buffer.bitLength % 8);
            if (bitsInLastByte != 0)
            {
                int excessBits = 8 - bitsInLastByte;
                bitLength -= excessBits;
            }
        }
    } // public partial class MessageBuffer
} // namespace TridentFramework.RPC.Net.Message
