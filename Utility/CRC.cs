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
// -----------------------------------------------------------------------
//
// This program is based on LZMA SDK 16.04
// Copyright (C) 2016 Igor Pavlov., All Rights Reserved.
// LZMA SDK is placed in the public domain.
// Anyone is free to copy, modify, publish, use, compile, sell, or distribute the original LZMA SDK code,
// either in source code form or as a compiled binary, for any purpose, commercial or non-commercial,
// and by any means.
//
// -----------------------------------------------------------------------

namespace TridentFramework.RPC.Utility
{
    /// <summary>
    /// Implements a 32-bit cyclic redundancy check hashing algorithm
    /// </summary>
    public class CRC
    {
        public static readonly uint[] Table;
        private uint value = 0xFFFFFFFF;

        /*
        ** Methods
        */

        /// <summary>
        /// Static initializer for the <see cref="CRC" /> class.
        /// </summary>
        static CRC()
        {
            Table = new uint[256];

            const uint kPoly = 0xEDB88320;
            for (uint i = 0; i < 256; i++)
            {
                uint r = i;
                for (int j = 0; j < 8; j++)
                {
                    if ((r & 1) != 0)
                        r = (r >> 1) ^ kPoly;
                    else
                        r >>= 1;
                }

                Table[i] = r;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CRC" /> class.
        /// </summary>
        public CRC()
        {
            value = 0xFFFFFFFF;
        }

        /// <summary>
        /// </summary>
        /// <param name="b"></param>
        public void UpdateByte(byte b)
        {
            value = Table[(byte)value ^ b] ^ (value >> 8);
        }

        /// <summary>
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public void Update(byte[] data, uint offset, uint count)
        {
            for (uint i = 0; i < count; i++)
                value = Table[(byte)value ^ data[offset + i]] ^ (value >> 8);
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public uint GetDigest()
        {
            return value ^ 0xFFFFFFFF;
        }

        /// <summary>
        /// Helper to calculate the CRC digest of the given byte buffer.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static uint CalculateDigest(byte[] data, uint offset, int count)
        {
            return CalculateDigest(data, offset, (uint)count);
        }

        /// <summary>
        /// Helper to calculate the CRC digest of the given byte buffer.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static uint CalculateDigest(byte[] data, uint offset, uint count)
        {
            CRC crc = new CRC();
            crc.Update(data, offset, count);

            return crc.GetDigest();
        }

        /// <summary>
        /// Helper to verify the CRC digest of the given byte buffer.
        /// </summary>
        /// <param name="digest"></param>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static bool VerifyDigest(uint digest, byte[] data, uint offset, int count)
        {
            return VerifyDigest(digest, data, offset, (uint)count);
        }

        /// <summary>
        /// Helper to verify the CRC digest of the given byte buffer.
        /// </summary>
        /// <param name="digest"></param>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static bool VerifyDigest(uint digest, byte[] data, uint offset, uint count)
        {
            return CalculateDigest(data, offset, count) == digest;
        }
    } // public class CRC
} // namespace TridentFramework.RPC.Utility
