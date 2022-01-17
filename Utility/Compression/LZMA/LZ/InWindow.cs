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
    public class InWindow
    {
        public uint blockSize; // size of Allocated memory block

        public byte[] bufBase; // pointer to buffer with data

        public uint bufferOffset;
        private uint keepSizeAfter; // how many BYTEs must be kept buffer after _pos
        private uint keepSizeBefore; // how many BYTEs must be kept in buffer before _pos

        private uint pointerToLastSafePosition;
        public uint pos; // offset (from _buffer) of curent byte
        private uint posLimit; // offset (from _buffer) of first byte when new block reading must be done
        private Stream stream;
        private bool streamEndWasReached; // if (true) then _streamPos shows real end of stream
        public uint streamPos; // offset (from _buffer) of first not read byte from Stream

        /*
        ** Methods
        */

        /// <summary>
        /// </summary>
        public void MoveBlock()
        {
            uint offset = bufferOffset + pos - keepSizeBefore;

            // we need one additional byte, since MovePos moves on 1 byte.
            if (offset > 0)
                offset--;

            uint numBytes = bufferOffset + streamPos - offset;

            // check negative offset ????
            for (uint i = 0; i < numBytes; i++)
                bufBase[i] = bufBase[offset + i];
            bufferOffset -= offset;
        }

        /// <summary>
        /// </summary>
        public virtual void ReadBlock()
        {
            if (streamEndWasReached)
                return;

            while (true)
            {
                int size = (int)(0 - bufferOffset + blockSize - streamPos);
                if (size == 0)
                    return;

                int numReadBytes = stream.Read(bufBase, (int)(bufferOffset + streamPos), size);
                if (numReadBytes == 0)
                {
                    posLimit = streamPos;
                    uint pointerToPostion = bufferOffset + posLimit;
                    if (pointerToPostion > pointerToLastSafePosition)
                        posLimit = pointerToLastSafePosition - bufferOffset;

                    streamEndWasReached = true;
                    return;
                }

                streamPos += (uint)numReadBytes;
                if (streamPos >= pos + keepSizeAfter)
                    posLimit = streamPos - keepSizeAfter;
            }
        }

        /// <summary>
        /// </summary>
        private void Free()
        {
            bufBase = null;
        }

        /// <summary>
        /// </summary>
        /// <param name="keepSizeBefore"></param>
        /// <param name="keepSizeAfter"></param>
        /// <param name="keepSizeReserv"></param>
        public void Create(uint keepSizeBefore, uint keepSizeAfter, uint keepSizeReserv)
        {
            this.keepSizeBefore = keepSizeBefore;
            this.keepSizeAfter = keepSizeAfter;
            uint blockSize = keepSizeBefore + keepSizeAfter + keepSizeReserv;
            if (bufBase == null || this.blockSize != blockSize)
            {
                Free();
                this.blockSize = blockSize;
                bufBase = new byte[this.blockSize];
            }

            pointerToLastSafePosition = this.blockSize - keepSizeAfter;
        }

        /// <summary>
        /// </summary>
        /// <param name="stream"></param>
        public void SetStream(Stream stream)
        {
            this.stream = stream;
        }

        /// <summary>
        /// </summary>
        public void ReleaseStream()
        {
            stream = null;
        }

        /// <summary>
        /// </summary>
        public void Init()
        {
            bufferOffset = 0;
            pos = 0;
            streamPos = 0;
            streamEndWasReached = false;
            ReadBlock();
        }

        /// <summary>
        /// </summary>
        public void MovePos()
        {
            pos++;
            if (pos > posLimit)
            {
                uint pointerToPostion = bufferOffset + pos;
                if (pointerToPostion > pointerToLastSafePosition)
                    MoveBlock();
                ReadBlock();
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public byte GetIndexByte(int index)
        {
            return bufBase[bufferOffset + pos + index];
        }

        /// <summary>
        /// </summary>
        /// <remarks>index + limit have not to exceed keepSizeAfter</remarks>
        /// <param name="index"></param>
        /// <param name="distance"></param>
        /// <param name="limit"></param>
        /// <returns></returns>
        public uint GetMatchLen(int index, uint distance, uint limit)
        {
            if (streamEndWasReached)
            {
                if (pos + index + limit > streamPos)
                    limit = streamPos - (uint)(pos + index);
            }

            distance++;

            // Byte *pby = _buffer + (size_t)_pos + index;
            uint pby = bufferOffset + pos + (uint)index;

            uint i;
            for (i = 0; i < limit && bufBase[pby + i] == bufBase[pby + i - distance]; i++) ;
            return i;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public uint GetNumAvailableBytes()
        {
            return streamPos - pos;
        }

        /// <summary>
        /// </summary>
        /// <param name="subValue"></param>
        public void ReduceOffsets(int subValue)
        {
            bufferOffset += (uint)subValue;
            posLimit -= (uint)subValue;
            pos -= (uint)subValue;
            streamPos -= (uint)subValue;
        }
    } // public class InWindow
} // namespace libtrace.LZ
