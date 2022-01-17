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

namespace TridentFramework.Compression.LZMA.RangeCoder
{
    /// <summary>
    /// </summary>
    public class RangeEncoder
    {
        public const uint TOP_VALUE = 1 << 24;
        private byte cache;
        private uint cacheSize;

        public ulong Low;
        public uint Range;

        private long StartPosition;

        private Stream Stream;

        /*
        ** Methods
        */

        /// <summary>
        /// </summary>
        /// <param name="stream"></param>
        public void SetStream(Stream stream)
        {
            Stream = stream;
        }

        /// <summary>
        /// </summary>
        public void ReleaseStream()
        {
            Stream = null;
        }

        /// <summary>
        /// </summary>
        public void Init()
        {
            StartPosition = Stream.Position;

            Low = 0;
            Range = 0xFFFFFFFF;
            cacheSize = 1;
            cache = 0;
        }

        /// <summary>
        /// </summary>
        public void FlushData()
        {
            for (int i = 0; i < 5; i++)
                ShiftLow();
        }

        /// <summary>
        /// </summary>
        public void FlushStream()
        {
            Stream.Flush();
        }

        /// <summary>
        /// </summary>
        public void CloseStream()
        {
            Stream.Close();
        }

        /// <summary>
        /// </summary>
        /// <param name="start"></param>
        /// <param name="size"></param>
        /// <param name="total"></param>
        public void Encode(uint start, uint size, uint total)
        {
            Low += start * (Range /= total);
            Range *= size;
            while (Range < TOP_VALUE)
            {
                Range <<= 8;
                ShiftLow();
            }
        }

        /// <summary>
        /// </summary>
        public void ShiftLow()
        {
            if ((uint)Low < 0xFF000000 || (uint)(Low >> 32) == 1)
            {
                byte temp = cache;
                do
                {
                    Stream.WriteByte((byte)(temp + (Low >> 32)));
                    temp = 0xFF;
                } while (--cacheSize != 0);

                cache = (byte)((uint)Low >> 24);
            }

            cacheSize++;
            Low = (uint)Low << 8;
        }

        /// <summary>
        /// </summary>
        /// <param name="v"></param>
        /// <param name="numTotalBits"></param>
        public void EncodeDirectBits(uint v, int numTotalBits)
        {
            for (int i = numTotalBits - 1; i >= 0; i--)
            {
                Range >>= 1;
                if (((v >> i) & 1) == 1)
                    Low += Range;
                if (Range < TOP_VALUE)
                {
                    Range <<= 8;
                    ShiftLow();
                }
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="size0"></param>
        /// <param name="numTotalBits"></param>
        /// <param name="symbol"></param>
        public void EncodeBit(uint size0, int numTotalBits, uint symbol)
        {
            uint newBound = (Range >> numTotalBits) * size0;
            if (symbol == 0)
                Range = newBound;
            else
            {
                Low += newBound;
                Range -= newBound;
            }

            while (Range < TOP_VALUE)
            {
                Range <<= 8;
                ShiftLow();
            }
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public long GetProcessedSizeAdd()
        {
            return cacheSize + Stream.Position - StartPosition + 4;

            // (long)Stream.GetProcessedSize();
        }
    } // public class Encoder

    /// <summary>
    /// </summary>
    public class RangeDecoder
    {
        public const uint TOP_VALUE = 1 << 24;
        public uint Code;

        public uint Range;

        public Stream Stream;

        /*
        ** Methods
        */

        /// <summary>
        /// </summary>
        /// <param name="stream"></param>
        public void Init(Stream stream)
        {
            // Stream.Init(stream);
            Stream = stream;

            Code = 0;
            Range = 0xFFFFFFFF;
            for (int i = 0; i < 5; i++)
                Code = (Code << 8) | (byte)Stream.ReadByte();
        }

        /// <summary>
        /// </summary>
        public void ReleaseStream()
        {
            // Stream.ReleaseStream();
            Stream = null;
        }

        /// <summary>
        /// </summary>
        public void CloseStream()
        {
            Stream.Close();
        }

        /// <summary>
        /// </summary>
        public void Normalize()
        {
            while (Range < TOP_VALUE)
            {
                Code = (Code << 8) | (byte)Stream.ReadByte();
                Range <<= 8;
            }
        }

        /// <summary>
        /// </summary>
        public void Normalize2()
        {
            if (Range < TOP_VALUE)
            {
                Code = (Code << 8) | (byte)Stream.ReadByte();
                Range <<= 8;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="total"></param>
        /// <returns></returns>
        public uint GetThreshold(uint total)
        {
            return Code / (Range /= total);
        }

        /// <summary>
        /// </summary>
        /// <param name="start"></param>
        /// <param name="size"></param>
        /// <param name="total"></param>
        public void Decode(uint start, uint size, uint total)
        {
            Code -= start * Range;
            Range *= size;
            Normalize();
        }

        /// <summary>
        /// </summary>
        /// <param name="numTotalBits"></param>
        /// <returns></returns>
        public uint DecodeDirectBits(int numTotalBits)
        {
            uint range = Range;
            uint code = Code;
            uint result = 0;
            for (int i = numTotalBits; i > 0; i--)
            {
                range >>= 1;
                /*
                result <<= 1;
                if (code >= range)
                {
                    code -= range;
                    result |= 1;
                }
                */
                uint t = (code - range) >> 31;
                code -= range & (t - 1);
                result = (result << 1) | (1 - t);

                if (range < TOP_VALUE)
                {
                    code = (code << 8) | (byte)Stream.ReadByte();
                    range <<= 8;
                }
            }

            Range = range;
            Code = code;
            return result;
        }

        /// <summary>
        /// </summary>
        /// <param name="size0"></param>
        /// <param name="numTotalBits"></param>
        /// <returns></returns>
        public uint DecodeBit(uint size0, int numTotalBits)
        {
            uint newBound = (Range >> numTotalBits) * size0;
            uint symbol;
            if (Code < newBound)
            {
                symbol = 0;
                Range = newBound;
            }
            else
            {
                symbol = 1;
                Code -= newBound;
                Range -= newBound;
            }

            Normalize();
            return symbol;
        }
    } // public class Decoder
} // namespace TridentFramework.Compression.LZMA.RangeCoder
