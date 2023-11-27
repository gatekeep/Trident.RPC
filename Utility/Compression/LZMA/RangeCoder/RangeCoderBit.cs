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
    public struct BitEncoder
    {
        public const int NUM_BIT_MODEL_TOTAL = 11;
        public const uint BIT_MODEL_TOTAL = 1 << NUM_BIT_MODEL_TOTAL;

        public const int NUM_MOVE_BITS = 5;
        public const int NUM_REDUCING_BITS = 2;
        public const int NUM_BIT_PRICE_SHIFT_BITS = 6;

        private uint Prob;
        private static readonly uint[] ProbPrices = new uint[BIT_MODEL_TOTAL >> NUM_REDUCING_BITS];

        /*
        ** Methods
        */

        /// <summary>
        /// Static initialize for the <see cref="BitEncoder" /> structure.
        /// </summary>
        static BitEncoder()
        {
            const int NUM_BITS = NUM_BIT_MODEL_TOTAL - NUM_REDUCING_BITS;
            for (int i = NUM_BITS - 1; i >= 0; i--)
            {
                uint start = (uint)1 << (NUM_BITS - i - 1);
                uint end = (uint)1 << (NUM_BITS - i);
                for (uint j = start; j < end; j++)
                {
                    ProbPrices[j] = ((uint)i << NUM_BIT_PRICE_SHIFT_BITS) +
                                    (((end - j) << NUM_BIT_PRICE_SHIFT_BITS) >> (NUM_BITS - i - 1));
                }
            }
        }

        /// <summary>
        /// </summary>
        public void Init()
        {
            Prob = BIT_MODEL_TOTAL >> 1;
        }

        /// <summary>
        /// </summary>
        /// <param name="symbol"></param>
        public void UpdateModel(uint symbol)
        {
            if (symbol == 0)
                Prob += (BIT_MODEL_TOTAL - Prob) >> NUM_MOVE_BITS;
            else
                Prob -= Prob >> NUM_MOVE_BITS;
        }

        /// <summary>
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="symbol"></param>
        public void Encode(RangeEncoder encoder, uint symbol)
        {
            // encoder.EncodeBit(Prob, kNumBitModelTotalBits, symbol);
            // UpdateModel(symbol);
            uint newBound = (encoder.Range >> NUM_BIT_MODEL_TOTAL) * Prob;
            if (symbol == 0)
            {
                encoder.Range = newBound;
                Prob += (BIT_MODEL_TOTAL - Prob) >> NUM_MOVE_BITS;
            }
            else
            {
                encoder.Low += newBound;
                encoder.Range -= newBound;
                Prob -= Prob >> NUM_MOVE_BITS;
            }

            if (encoder.Range < RangeEncoder.TOP_VALUE)
            {
                encoder.Range <<= 8;
                encoder.ShiftLow();
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public uint GetPrice(uint symbol)
        {
            return ProbPrices[(((Prob - symbol) ^ -(int)symbol) & (BIT_MODEL_TOTAL - 1)) >> NUM_REDUCING_BITS];
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public uint GetPrice0()
        {
            return ProbPrices[Prob >> NUM_REDUCING_BITS];
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public uint GetPrice1()
        {
            return ProbPrices[(BIT_MODEL_TOTAL - Prob) >> NUM_REDUCING_BITS];
        }
    } // public struct BitEncoder

    /// <summary>
    /// </summary>
    public struct BitDecoder
    {
        /**
         * Fields
         */
        public const int NUM_BIT_MODEL_TOTAL = 11;
        public const uint BIT_MODEL_TOTAL = 1 << NUM_BIT_MODEL_TOTAL;
        public const int NUM_MOVE_BITS = 5;

        private uint Prob;

        /*
        ** Methods
        */

        /// <summary>
        /// </summary>
        /// <param name="numMoveBits"></param>
        /// <param name="symbol"></param>
        public void UpdateModel(int numMoveBits, uint symbol)
        {
            if (symbol == 0)
                Prob += (BIT_MODEL_TOTAL - Prob) >> numMoveBits;
            else
                Prob -= Prob >> numMoveBits;
        }

        /// <summary>
        /// </summary>
        public void Init()
        {
            Prob = BIT_MODEL_TOTAL >> 1;
        }

        /// <summary>
        /// </summary>
        /// <param name="rangeDecoder"></param>
        /// <returns></returns>
        public uint Decode(RangeDecoder rangeDecoder)
        {
            uint newBound = (rangeDecoder.Range >> NUM_BIT_MODEL_TOTAL) * Prob;
            if (rangeDecoder.Code < newBound)
            {
                rangeDecoder.Range = newBound;
                Prob += (BIT_MODEL_TOTAL - Prob) >> NUM_MOVE_BITS;
                if (rangeDecoder.Range < RangeDecoder.TOP_VALUE)
                {
                    rangeDecoder.Code = (rangeDecoder.Code << 8) | (byte)rangeDecoder.Stream.ReadByte();
                    rangeDecoder.Range <<= 8;
                }

                return 0;
            }

            rangeDecoder.Range -= newBound;
            rangeDecoder.Code -= newBound;
            Prob -= Prob >> NUM_MOVE_BITS;
            if (rangeDecoder.Range < RangeDecoder.TOP_VALUE)
            {
                rangeDecoder.Code = (rangeDecoder.Code << 8) | (byte)rangeDecoder.Stream.ReadByte();
                rangeDecoder.Range <<= 8;
            }

            return 1;
        }
    } // public struct BitDecoder
} // namespace TridentFramework.Compression.LZMA.RangeCoder
