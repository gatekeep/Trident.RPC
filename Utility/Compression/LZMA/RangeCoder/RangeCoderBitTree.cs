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

namespace TridentFramework.Compression.LZMA.RangeCoder
{
    /// <summary>
    /// </summary>
    public struct BitTreeEncoder
    {
        private readonly BitEncoder[] Models;
        private readonly int NumBitLevels;

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="BitTreeEncoder" /> structure.
        /// </summary>
        /// <param name="numBitLevels"></param>
        public BitTreeEncoder(int numBitLevels)
        {
            NumBitLevels = numBitLevels;
            Models = new BitEncoder[1 << numBitLevels];
        }

        /// <summary>
        /// </summary>
        public void Init()
        {
            for (uint i = 1; i < 1 << NumBitLevels; i++)
                Models[i].Init();
        }

        /// <summary>
        /// </summary>
        /// <param name="rangeEncoder"></param>
        /// <param name="symbol"></param>
        public void Encode(RangeEncoder rangeEncoder, uint symbol)
        {
            uint m = 1;
            for (int bitIndex = NumBitLevels; bitIndex > 0;)
            {
                bitIndex--;
                uint bit = (symbol >> bitIndex) & 1;
                Models[m].Encode(rangeEncoder, bit);
                m = (m << 1) | bit;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="rangeEncoder"></param>
        /// <param name="symbol"></param>
        public void ReverseEncode(RangeEncoder rangeEncoder, uint symbol)
        {
            uint m = 1;
            for (uint i = 0; i < NumBitLevels; i++)
            {
                uint bit = symbol & 1;
                Models[m].Encode(rangeEncoder, bit);
                m = (m << 1) | bit;
                symbol >>= 1;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public uint GetPrice(uint symbol)
        {
            uint price = 0;
            uint m = 1;
            for (int bitIndex = NumBitLevels; bitIndex > 0;)
            {
                bitIndex--;
                uint bit = (symbol >> bitIndex) & 1;
                price += Models[m].GetPrice(bit);
                m = (m << 1) + bit;
            }

            return price;
        }

        /// <summary>
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public uint ReverseGetPrice(uint symbol)
        {
            uint price = 0;
            uint m = 1;
            for (int i = NumBitLevels; i > 0; i--)
            {
                uint bit = symbol & 1;
                symbol >>= 1;
                price += Models[m].GetPrice(bit);
                m = (m << 1) | bit;
            }

            return price;
        }

        /// <summary>
        /// </summary>
        /// <param name="Models"></param>
        /// <param name="startIndex"></param>
        /// <param name="NumBitLevels"></param>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public static uint ReverseGetPrice(BitEncoder[] Models, uint startIndex, int NumBitLevels, uint symbol)
        {
            uint price = 0;
            uint m = 1;
            for (int i = NumBitLevels; i > 0; i--)
            {
                uint bit = symbol & 1;
                symbol >>= 1;
                price += Models[startIndex + m].GetPrice(bit);
                m = (m << 1) | bit;
            }

            return price;
        }

        /// <summary>
        /// </summary>
        /// <param name="Models"></param>
        /// <param name="startIndex"></param>
        /// <param name="rangeEncoder"></param>
        /// <param name="NumBitLevels"></param>
        /// <param name="symbol"></param>
        public static void ReverseEncode(BitEncoder[] Models, uint startIndex, RangeEncoder rangeEncoder,
            int NumBitLevels, uint symbol)
        {
            uint m = 1;
            for (int i = 0; i < NumBitLevels; i++)
            {
                uint bit = symbol & 1;
                Models[startIndex + m].Encode(rangeEncoder, bit);
                m = (m << 1) | bit;
                symbol >>= 1;
            }
        }
    } // public struct BitTreeEncoder

    /// <summary>
    /// </summary>
    public struct BitTreeDecoder
    {
        private readonly BitDecoder[] Models;
        private readonly int NumBitLevels;

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="BitTreeDecoder" />  class.
        /// </summary>
        /// <param name="numBitLevels"></param>
        public BitTreeDecoder(int numBitLevels)
        {
            NumBitLevels = numBitLevels;
            Models = new BitDecoder[1 << numBitLevels];
        }

        /// <summary>
        /// </summary>
        public void Init()
        {
            for (uint i = 1; i < 1 << NumBitLevels; i++)
                Models[i].Init();
        }

        /// <summary>
        /// </summary>
        /// <param name="rangeDecoder"></param>
        /// <returns></returns>
        public uint Decode(RangeDecoder rangeDecoder)
        {
            uint m = 1;
            for (int bitIndex = NumBitLevels; bitIndex > 0; bitIndex--)
                m = (m << 1) + Models[m].Decode(rangeDecoder);
            return m - ((uint)1 << NumBitLevels);
        }

        /// <summary>
        /// </summary>
        /// <param name="rangeDecoder"></param>
        /// <returns></returns>
        public uint ReverseDecode(RangeDecoder rangeDecoder)
        {
            uint m = 1;
            uint symbol = 0;
            for (int bitIndex = 0; bitIndex < NumBitLevels; bitIndex++)
            {
                uint bit = Models[m].Decode(rangeDecoder);
                m <<= 1;
                m += bit;
                symbol |= bit << bitIndex;
            }

            return symbol;
        }

        /// <summary>
        /// </summary>
        /// <param name="Models"></param>
        /// <param name="startIndex"></param>
        /// <param name="rangeDecoder"></param>
        /// <param name="NumBitLevels"></param>
        /// <returns></returns>
        public static uint ReverseDecode(BitDecoder[] Models, uint startIndex, RangeDecoder rangeDecoder,
            int NumBitLevels)
        {
            uint m = 1;
            uint symbol = 0;
            for (int bitIndex = 0; bitIndex < NumBitLevels; bitIndex++)
            {
                uint bit = Models[startIndex + m].Decode(rangeDecoder);
                m <<= 1;
                m += bit;
                symbol |= bit << bitIndex;
            }

            return symbol;
        }
    } // public struct BitTreeDecoder
} // namespace TridentFramework.Compression.LZMA.RangeCoder
