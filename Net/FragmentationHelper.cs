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

namespace TridentFramework.RPC.Net
{
    /// <summary>
    /// Fragmentation helper
    /// </summary>
    internal static class FragmentationHelper
    {
        /*
        ** Methods
        */

        /// <summary>
        /// Write fragmentation header
        /// </summary>
        /// <returns></returns>
        public static int WriteHeader(byte[] destination, int ptr, int group, int totalBits, int chunkByteSize, int chunkNumber)
        {
            uint num1 = (uint)group;
            while (num1 >= 0x80)
            {
                destination[ptr++] = (byte)(num1 | 0x80);
                num1 = num1 >> 7;
            }
            destination[ptr++] = (byte)num1;

            // write variable length fragment total bits
            uint num2 = (uint)totalBits;
            while (num2 >= 0x80)
            {
                destination[ptr++] = (byte)(num2 | 0x80);
                num2 = num2 >> 7;
            }
            destination[ptr++] = (byte)num2;

            // write variable length fragment chunk size
            uint num3 = (uint)chunkByteSize;
            while (num3 >= 0x80)
            {
                destination[ptr++] = (byte)(num3 | 0x80);
                num3 = num3 >> 7;
            }
            destination[ptr++] = (byte)num3;

            // write variable length fragment chunk number
            uint num4 = (uint)chunkNumber;
            while (num4 >= 0x80)
            {
                destination[ptr++] = (byte)(num4 | 0x80);
                num4 = num4 >> 7;
            }
            destination[ptr++] = (byte)num4;

            return ptr;
        }

        /// <summary>
        /// Read fragmentation header
        /// </summary>
        /// <returns></returns>
        public static int ReadHeader(byte[] buffer, int ptr, out int group, out int totalBits, out int chunkByteSize, out int chunkNumber)
        {
            int num1 = 0;
            int num2 = 0;
            while (true)
            {
                byte num3 = buffer[ptr++];
                num1 |= (num3 & 0x7f) << (num2 & 0x1f);
                num2 += 7;
                if ((num3 & 0x80) == 0)
                {
                    group = num1;
                    break;
                }
            }

            num1 = 0;
            num2 = 0;
            while (true)
            {
                byte num3 = buffer[ptr++];
                num1 |= (num3 & 0x7f) << (num2 & 0x1f);
                num2 += 7;
                if ((num3 & 0x80) == 0)
                {
                    totalBits = num1;
                    break;
                }
            }

            num1 = 0;
            num2 = 0;
            while (true)
            {
                byte num3 = buffer[ptr++];
                num1 |= (num3 & 0x7f) << (num2 & 0x1f);
                num2 += 7;
                if ((num3 & 0x80) == 0)
                {
                    chunkByteSize = num1;
                    break;
                }
            }

            num1 = 0;
            num2 = 0;
            while (true)
            {
                byte num3 = buffer[ptr++];
                num1 |= (num3 & 0x7f) << (num2 & 0x1f);
                num2 += 7;
                if ((num3 & 0x80) == 0)
                {
                    chunkNumber = num1;
                    break;
                }
            }

            return ptr;
        }

        /// <summary>
        /// Get fragmentation header size
        /// </summary>
        /// <returns></returns>
        public static int GetFragmentationHeaderSize(int groupId, int totalBytes, int chunkByteSize, int numChunks)
        {
            int len = 4;

            // write variable length fragment group id
            uint num1 = (uint)groupId;
            while (num1 >= 0x80)
            {
                len++;
                num1 = num1 >> 7;
            }

            // write variable length fragment total bits
            uint num2 = (uint)(totalBytes * 8);
            while (num2 >= 0x80)
            {
                len++;
                num2 = num2 >> 7;
            }

            // write variable length fragment chunk byte size
            uint num3 = (uint)chunkByteSize;
            while (num3 >= 0x80)
            {
                len++;
                num3 = num3 >> 7;
            }

            // write variable length fragment chunk number
            uint num4 = (uint)numChunks;
            while (num4 >= 0x80)
            {
                len++;
                num4 = num4 >> 7;
            }

            return len;
        }

        /// <summary>
        /// Get best chunk size for fragmentation
        /// </summary>
        /// <returns></returns>
        public static int GetBestChunkSize(int group, int totalBytes, int mtu)
        {
            int tryNumChunks = (totalBytes / (mtu - 8)) + 1;
            int tryChunkSize = (totalBytes / tryNumChunks) + 1; // +1 since we immediately decrement it in the loop

            int headerSize = 0;
            do
            {
                tryChunkSize--; // keep reducing chunk size until it fits within MTU including header

                int numChunks = totalBytes / tryChunkSize;

                if (numChunks * tryChunkSize < totalBytes)
                    numChunks++;

                headerSize = GetFragmentationHeaderSize(group, totalBytes, tryChunkSize, numChunks);
            }
            while (tryChunkSize + headerSize + 5 + 1 >= mtu);

            return tryChunkSize;
        }
    } // internal static class FragmentationHelper
} // namespace TridentFramework.RPC.Net
