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

namespace TridentFramework.Compression.LZMA.Common
{
    /// <summary>
    /// </summary>
    public class InBuffer
    {
        private readonly byte[] buffer;
        private readonly uint bufSize;
        private uint bufLimit;
        private uint bufPosition;
        private ulong processedSize;
        private Stream stream;
        private bool streamWasExhausted;

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="InBuffer" /> class.
        /// </summary>
        /// <param name="bufferSize"></param>
        public InBuffer(uint bufferSize)
        {
            buffer = new byte[bufferSize];
            bufSize = bufferSize;
        }

        /// <summary>
        /// </summary>
        /// <param name="stream"></param>
        public void Init(Stream stream)
        {
            this.stream = stream;
            processedSize = 0;
            bufLimit = 0;
            bufPosition = 0;
            streamWasExhausted = false;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public bool ReadBlock()
        {
            if (streamWasExhausted)
                return false;

            processedSize += bufPosition;

            int aNumProcessedBytes = stream.Read(buffer, 0, (int)bufSize);
            bufPosition = 0;
            bufLimit = (uint)aNumProcessedBytes;

            streamWasExhausted = aNumProcessedBytes == 0;
            return !streamWasExhausted;
        }

        /// <summary>
        /// </summary>
        public void ReleaseStream()
        {
            //stream.Close();
            stream = null;
        }

        /// <summary>
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public bool ReadByte(byte b) // check it
        {
            if (bufPosition >= bufLimit)
            {
                if (!ReadBlock())
                    return false;
            }

            b = buffer[bufPosition++];
            return true;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public byte ReadByte()
        {
            // return (byte)m_Stream.ReadByte();
            if (bufPosition >= bufLimit)
            {
                if (!ReadBlock())
                    return 0xFF;
            }

            return buffer[bufPosition++];
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public ulong GetProcessedSize()
        {
            return processedSize + bufPosition;
        }
    } // public class InBuffer
} // namespace TridentFramework.Compression.LZMA.Common
