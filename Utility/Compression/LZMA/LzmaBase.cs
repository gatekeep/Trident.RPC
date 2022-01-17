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

namespace TridentFramework.Compression.LZMA
{
    /// <summary>
    /// </summary>
    internal abstract class LzmaBase
    {
        public const uint NUM_REP_DISTANCE = 4;
        public const uint NUM_STATES = 12;

        public const int NUM_POS_SLOT_BITS = 6;
        public const int DIC_LOG_SIZE_MIN = 0;

        public const int NUM_LEN_TO_POS_STATES_BITS = 2; // it's for speed optimization
        public const uint NUM_LEN_TO_POS_STATES = 1 << NUM_LEN_TO_POS_STATES_BITS;

        public const uint MATCH_MIN_LEN = 2;

        public const int NUM_ALIGN_BITS = 4;
        public const uint ALIGN_TABLE_SIZE = 1 << NUM_ALIGN_BITS;
        public const uint ALIGN_MASK = ALIGN_TABLE_SIZE - 1;

        public const uint START_POS_MODEL_IDX = 4;
        public const uint END_POS_MODEL_IDX = 14;
        public const uint NUM_POS_MODELS = END_POS_MODEL_IDX - START_POS_MODEL_IDX;

        public const uint NUM_FULL_DISTANCE = 1 << ((int)END_POS_MODEL_IDX / 2);

        public const uint NUM_LIT_POS_STATES_ENCODING_MAX = 4;
        public const uint NUM_LIT_CONTEXT_BITS_MAX = 8;

        public const int NUM_POS_STATES_BITS_MAX = 4;
        public const uint NUM_POS_STATES_MAX = 1 << NUM_POS_STATES_BITS_MAX;
        public const int NUM_POS_STATES_BITS_ENCODING_MAX = 4;
        public const uint NUM_POS_STATES_ENCODING_MAX = 1 << NUM_POS_STATES_BITS_ENCODING_MAX;

        public const int NUM_LOW_LEN_BITS = 3;
        public const int NUM_MID_LEN_BITS = 3;
        public const int NUM_HIGH_LEN_BITS = 8;
        public const uint NUM_LOW_LEN_SYMBOLS = 1 << NUM_LOW_LEN_BITS;
        public const uint NUM_MID_LEN_SYMBOLS = 1 << NUM_MID_LEN_BITS;
        public const uint NUM_LEN_SYMBOLS = NUM_LOW_LEN_SYMBOLS + NUM_MID_LEN_SYMBOLS + (1 << NUM_HIGH_LEN_BITS);
        public const uint MATCH_MAX_LEN = MATCH_MIN_LEN + NUM_LEN_SYMBOLS - 1;

        /**
         * Structures
         */

        /// <summary>
        /// </summary>
        public struct State
        {
            public uint Index;

            /**
             * Methods
             */

            /// <summary>
            /// </summary>
            public void Init()
            {
                Index = 0;
            }

            /// <summary>
            /// </summary>
            public void UpdateChar()
            {
                if (Index < 4) Index = 0;
                else if (Index < 10) Index -= 3;
                else Index -= 6;
            }

            /// <summary>
            /// </summary>
            public void UpdateMatch()
            {
                Index = (uint)(Index < 7 ? 7 : 10);
            }

            /// <summary>
            /// </summary>
            public void UpdateRep()
            {
                Index = (uint)(Index < 7 ? 8 : 11);
            }

            /// <summary>
            /// </summary>
            public void UpdateShortRep()
            {
                Index = (uint)(Index < 7 ? 9 : 11);
            }

            /// <summary>
            /// </summary>
            /// <returns></returns>
            public bool IsCharState()
            {
                return Index < 7;
            }
        } // public struct State

        /*
        ** Methods
        */

        /// <summary>
        /// </summary>
        /// <param name="len"></param>
        /// <returns></returns>
        public static uint GetLenToPosState(uint len)
        {
            len -= MATCH_MIN_LEN;
            if (len < NUM_LEN_TO_POS_STATES)
                return len;
            return NUM_LEN_TO_POS_STATES - 1;
        }
    } // internal abstract class LzmaBase
} // namespace TridentFramework.Compression.LZMA
