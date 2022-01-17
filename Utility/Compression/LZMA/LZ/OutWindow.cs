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

using System.IO;

namespace TridentFramework.Compression.LZMA.LZ
{
    /// <summary>
    /// </summary>
    public class OutWindow
    {
        private byte[] buffer;
        private uint pos;
        private Stream stream;
        private uint streamPos;

        public uint TrainSize;
        private uint windowSize;

        /*
        ** Methods
        */

        /// <summary>
        /// </summary>
        /// <param name="windowSize"></param>
        public void Create(uint windowSize)
        {
            if (this.windowSize != windowSize)
                buffer = new byte[windowSize];

            this.windowSize = windowSize;
            pos = 0;
            streamPos = 0;
        }

        /// <summary>
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="solid"></param>
        public void Init(Stream stream, bool solid)
        {
            ReleaseStream();
            this.stream = stream;
            if (!solid)
            {
                streamPos = 0;
                pos = 0;
                TrainSize = 0;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public bool Train(Stream stream)
        {
            long len = stream.Length;
            uint size = len < windowSize ? (uint)len : windowSize;
            TrainSize = size;
            stream.Position = len - size;
            streamPos = pos = 0;
            while (size > 0)
            {
                uint curSize = windowSize - pos;
                if (size < curSize)
                    curSize = size;
                int numReadBytes = stream.Read(buffer, (int)pos, (int)curSize);
                if (numReadBytes == 0)
                    return false;
                size -= (uint)numReadBytes;
                pos += (uint)numReadBytes;
                streamPos += (uint)numReadBytes;
                if (pos == windowSize)
                    streamPos = pos = 0;
            }

            return true;
        }

        /// <summary>
        /// </summary>
        public void ReleaseStream()
        {
            Flush();
            stream = null;
        }

        /// <summary>
        /// </summary>
        public void Flush()
        {
            uint size = pos - streamPos;
            if (size == 0)
                return;
            stream.Write(buffer, (int)streamPos, (int)size);

            if (pos >= windowSize)
                pos = 0;
            streamPos = pos;
        }

        /// <summary>
        /// </summary>
        /// <param name="distance"></param>
        /// <param name="len"></param>
        public void CopyBlock(uint distance, uint len)
        {
            uint pos = this.pos - distance - 1;
            if (pos >= windowSize)
                pos += windowSize;

            for (; len > 0; len--)
            {
                if (pos >= windowSize)
                    pos = 0;
                buffer[this.pos++] = buffer[pos++];
                if (this.pos >= windowSize)
                    Flush();
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="b"></param>
        public void PutByte(byte b)
        {
            buffer[pos++] = b;
            if (pos >= windowSize)
                Flush();
        }

        /// <summary>
        /// </summary>
        /// <param name="distance"></param>
        /// <returns></returns>
        public byte GetByte(uint distance)
        {
            uint pos = this.pos - distance - 1;
            if (pos >= windowSize)
                pos += windowSize;
            return buffer[pos];
        }
    } // public class OutWindow
} // namespace libtrace.LZ
