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

using System;
using System.IO;

using TridentFramework.Compression.LZMA.LZ;
using TridentFramework.Compression.LZMA.RangeCoder;

namespace TridentFramework.Compression.LZMA
{
    /// <summary>
    /// </summary>
    public class LzmaEncoder : ICoder, ISetCoderProperties, IWriteCoderProperties
    {
        public const uint IFINITY_PRICE = 0xFFFFFFF;

        public const int DEFAULT_DIC_LOG_SIZE = 22;
        public const uint NUM_FAST_BYTES_DEFAULT = 32;
        public const uint NUM_LEN_SPEC_SYMBOLS = LzmaBase.NUM_LOW_LEN_SYMBOLS + LzmaBase.NUM_MID_LEN_SYMBOLS;

        public const uint NUM_OPTS = 1 << 12;

        public const int PROP_SIZE = 5;

        private static readonly string[] MatchFinderIDs =
        {
            "BT2", "BT4"
        };

        private static readonly byte[] fastPos = new byte[1 << 11];
        private readonly uint[] alignPrices = new uint[LzmaBase.ALIGN_TABLE_SIZE];

        private readonly uint[] distancesPrices = new uint[LzmaBase.NUM_FULL_DISTANCE << LzmaBase.NUM_LEN_TO_POS_STATES_BITS];

        private readonly BitEncoder[] isMatch = new BitEncoder[LzmaBase.NUM_STATES << LzmaBase.NUM_POS_STATES_BITS_MAX];
        private readonly BitEncoder[] isRep = new BitEncoder[LzmaBase.NUM_STATES];

        private readonly BitEncoder[] isRep0Long = new BitEncoder[LzmaBase.NUM_STATES << LzmaBase.NUM_POS_STATES_BITS_MAX];

        private readonly BitEncoder[] isRepG0 = new BitEncoder[LzmaBase.NUM_STATES];
        private readonly BitEncoder[] isRepG1 = new BitEncoder[LzmaBase.NUM_STATES];
        private readonly BitEncoder[] isRepG2 = new BitEncoder[LzmaBase.NUM_STATES];

        private readonly LenPriceTableEncoder lenEncoder = new LenPriceTableEncoder();

        private readonly LiteralEncoder literalEncoder = new LiteralEncoder();

        private readonly uint[] matchDistances = new uint[LzmaBase.MATCH_MAX_LEN * 2 + 2];

        private readonly Optimal[] optimum = new Optimal[NUM_OPTS];

        private readonly BitEncoder[] posEncoders = new BitEncoder[LzmaBase.NUM_FULL_DISTANCE - LzmaBase.END_POS_MODEL_IDX];

        private readonly BitTreeEncoder[] posSlotEncoder = new BitTreeEncoder[LzmaBase.NUM_LEN_TO_POS_STATES];

        private readonly uint[] posSlotPrices = new uint[1 << (LzmaBase.NUM_POS_SLOT_BITS + LzmaBase.NUM_LEN_TO_POS_STATES_BITS)];

        private readonly byte[] properties = new byte[PROP_SIZE];
        private readonly RangeEncoder rangeEncoder = new RangeEncoder();
        private readonly uint[] repDistances = new uint[LzmaBase.NUM_REP_DISTANCE];
        private readonly uint[] repLens = new uint[LzmaBase.NUM_REP_DISTANCE];
        private readonly LenPriceTableEncoder repMatchLenEncoder = new LenPriceTableEncoder();

        private readonly uint[] reps = new uint[LzmaBase.NUM_REP_DISTANCE];

        private readonly uint[] tempPrices = new uint[LzmaBase.NUM_FULL_DISTANCE];

        private uint additionalOffset;
        private uint alignPriceCount;

        private uint dictionarySize = 1 << DEFAULT_DIC_LOG_SIZE;
        private uint dictionarySizePrev = 0xFFFFFFFF;

        private uint distTableSize = DEFAULT_DIC_LOG_SIZE * 2;
        private bool finished;
        private Stream inStream;
        private uint longestMatchLength;

        private bool longestMatchWasFound;
        private IMatchFinder matchFinder;

        private EMatchFinderType matchFinderType = EMatchFinderType.BT4;
        private uint matchPriceCount;

        private bool needReleaseMFStream;

        private long nowPos64;
        private uint numDistancePairs;

        private uint numFastBytes = NUM_FAST_BYTES_DEFAULT;
        private uint numFastBytesPrev = 0xFFFFFFFF;
        private int numLiteralContextBits = 3;
        private int numLiteralPosStateBits;
        private uint optimumCurrentIndex;

        private uint optimumEndIndex;
        private BitTreeEncoder posAlignEncoder = new BitTreeEncoder(LzmaBase.NUM_ALIGN_BITS);

        private int posStateBits = 2;
        private uint posStateMask = 4 - 1;
        private byte previousByte;

        private LzmaBase.State state = new LzmaBase.State();

        private uint trainSize;
        private bool writeEndMark;

        /// <summary>
        /// </summary>
        private enum EMatchFinderType
        {
            BT2,
            BT4
        }

        /*
        ** Classes
        */

        /// <summary>
        /// </summary>
        private class LenEncoder
        {
            private readonly BitTreeEncoder[] lowCoder = new BitTreeEncoder[LzmaBase.NUM_POS_STATES_ENCODING_MAX];

            private readonly BitTreeEncoder[] midCoder = new BitTreeEncoder[LzmaBase.NUM_POS_STATES_ENCODING_MAX];

            private BitEncoder choice = new BitEncoder();
            private BitEncoder choice2 = new BitEncoder();
            private BitTreeEncoder highCoder = new BitTreeEncoder(LzmaBase.NUM_HIGH_LEN_BITS);

            /**
             * Methods
             */

            /// <summary>
            /// Initializes a new instance of the <see cref="LenEncoder" /> class.
            /// </summary>
            public LenEncoder()
            {
                for (uint posState = 0; posState < LzmaBase.NUM_POS_STATES_ENCODING_MAX; posState++)
                {
                    lowCoder[posState] = new BitTreeEncoder(LzmaBase.NUM_LOW_LEN_BITS);
                    midCoder[posState] = new BitTreeEncoder(LzmaBase.NUM_MID_LEN_BITS);
                }
            }

            /// <summary>
            /// </summary>
            /// <param name="numPosStates"></param>
            public void Init(uint numPosStates)
            {
                choice.Init();
                choice2.Init();
                for (uint posState = 0; posState < numPosStates; posState++)
                {
                    lowCoder[posState].Init();
                    midCoder[posState].Init();
                }

                highCoder.Init();
            }

            /// <summary>
            /// </summary>
            /// <param name="rangeEncoder"></param>
            /// <param name="symbol"></param>
            /// <param name="posState"></param>
            public void Encode(RangeEncoder rangeEncoder, uint symbol, uint posState)
            {
                if (symbol < LzmaBase.NUM_LOW_LEN_SYMBOLS)
                {
                    choice.Encode(rangeEncoder, 0);
                    lowCoder[posState].Encode(rangeEncoder, symbol);
                }
                else
                {
                    symbol -= LzmaBase.NUM_LOW_LEN_SYMBOLS;
                    choice.Encode(rangeEncoder, 1);
                    if (symbol < LzmaBase.NUM_MID_LEN_SYMBOLS)
                    {
                        choice2.Encode(rangeEncoder, 0);
                        midCoder[posState].Encode(rangeEncoder, symbol);
                    }
                    else
                    {
                        choice2.Encode(rangeEncoder, 1);
                        highCoder.Encode(rangeEncoder, symbol - LzmaBase.NUM_MID_LEN_SYMBOLS);
                    }
                }
            }

            /// <summary>
            /// </summary>
            /// <param name="posState"></param>
            /// <param name="numSymbols"></param>
            /// <param name="prices"></param>
            /// <param name="st"></param>
            public void SetPrices(uint posState, uint numSymbols, uint[] prices, uint st)
            {
                uint a0 = choice.GetPrice0();
                uint a1 = choice.GetPrice1();
                uint b0 = a1 + choice2.GetPrice0();
                uint b1 = a1 + choice2.GetPrice1();
                uint i = 0;

                for (i = 0; i < LzmaBase.NUM_LOW_LEN_SYMBOLS; i++)
                {
                    if (i >= numSymbols)
                        return;
                    prices[st + i] = a0 + lowCoder[posState].GetPrice(i);
                }

                for (; i < LzmaBase.NUM_LOW_LEN_SYMBOLS + LzmaBase.NUM_MID_LEN_SYMBOLS; i++)
                {
                    if (i >= numSymbols)
                        return;
                    prices[st + i] = b0 + midCoder[posState].GetPrice(i - LzmaBase.NUM_LOW_LEN_SYMBOLS);
                }

                for (; i < numSymbols; i++)
                {
                    prices[st + i] =
                        b1 + highCoder.GetPrice(i - LzmaBase.NUM_LOW_LEN_SYMBOLS - LzmaBase.NUM_MID_LEN_SYMBOLS);
                }
            }
        } // private class LenEncoder

        /// <summary>
        /// </summary>
        private class LiteralEncoder
        {
            private Encoder[] coders;
            private int numPosBits;
            private int numPrevBits;
            private uint posMask;

            /**
             * Structures
             */

            /// <summary>
            /// </summary>
            public struct Encoder
            {
                public const int ENCODER_COUNT = 768;
                private BitEncoder[] encoders;

                /**
                 * Methods
                 */

                /// <summary>
                /// </summary>
                public void Create()
                {
                    encoders = new BitEncoder[ENCODER_COUNT];
                }

                /// <summary>
                /// </summary>
                public void Init()
                {
                    for (int i = 0; i < ENCODER_COUNT; i++)
                        encoders[i].Init();
                }

                /// <summary>
                /// </summary>
                /// <param name="rangeEncoder"></param>
                /// <param name="symbol"></param>
                public void Encode(RangeEncoder rangeEncoder, byte symbol)
                {
                    uint context = 1;
                    for (int i = 7; i >= 0; i--)
                    {
                        uint bit = (uint)((symbol >> i) & 1);
                        encoders[context].Encode(rangeEncoder, bit);
                        context = (context << 1) | bit;
                    }
                }

                /// <summary>
                /// </summary>
                /// <param name="rangeEncoder"></param>
                /// <param name="matchByte"></param>
                /// <param name="symbol"></param>
                public void EncodeMatched(RangeEncoder rangeEncoder, byte matchByte, byte symbol)
                {
                    uint context = 1;
                    bool same = true;
                    for (int i = 7; i >= 0; i--)
                    {
                        uint bit = (uint)((symbol >> i) & 1);
                        uint state = context;
                        if (same)
                        {
                            uint matchBit = (uint)((matchByte >> i) & 1);
                            state += (1 + matchBit) << 8;
                            same = matchBit == bit;
                        }

                        encoders[state].Encode(rangeEncoder, bit);
                        context = (context << 1) | bit;
                    }
                }

                /// <summary>
                /// </summary>
                /// <param name="matchMode"></param>
                /// <param name="matchByte"></param>
                /// <param name="symbol"></param>
                /// <returns></returns>
                public uint GetPrice(bool matchMode, byte matchByte, byte symbol)
                {
                    uint price = 0;
                    uint context = 1;
                    int i = 7;
                    if (matchMode)
                    {
                        for (; i >= 0; i--)
                        {
                            uint matchBit = (uint)(matchByte >> i) & 1;
                            uint bit = (uint)(symbol >> i) & 1;
                            price += encoders[((1 + matchBit) << 8) + context].GetPrice(bit);
                            context = (context << 1) | bit;
                            if (matchBit != bit)
                            {
                                i--;
                                break;
                            }
                        }
                    }

                    for (; i >= 0; i--)
                    {
                        uint bit = (uint)(symbol >> i) & 1;
                        price += encoders[context].GetPrice(bit);
                        context = (context << 1) | bit;
                    }

                    return price;
                }
            } // private struct Encoder

            /**
             * Methods
             */

            /// <summary>
            /// </summary>
            /// <param name="numPosBits"></param>
            /// <param name="numPrevBits"></param>
            public void Create(int numPosBits, int numPrevBits)
            {
                if (coders != null && this.numPrevBits == numPrevBits && this.numPosBits == numPosBits)
                    return;

                this.numPosBits = numPosBits;
                posMask = ((uint)1 << numPosBits) - 1;
                this.numPrevBits = numPrevBits;
                uint numStates = (uint)1 << (this.numPrevBits + this.numPosBits);
                coders = new Encoder[numStates];
                for (uint i = 0; i < numStates; i++)
                    coders[i].Create();
            }

            /// <summary>
            /// </summary>
            public void Init()
            {
                uint numStates = (uint)1 << (numPrevBits + numPosBits);
                for (uint i = 0; i < numStates; i++)
                    coders[i].Init();
            }

            /// <summary>
            /// </summary>
            /// <param name="pos"></param>
            /// <param name="prevByte"></param>
            /// <returns></returns>
            public Encoder GetSubCoder(uint pos, byte prevByte)
            {
                return coders[((pos & posMask) << numPrevBits) + (uint)(prevByte >> (8 - numPrevBits))];
            }
        } // private class LiteralEncoder

        /// <summary>
        /// </summary>
        private class LenPriceTableEncoder : LenEncoder
        {
            private readonly uint[] counters = new uint[LzmaBase.NUM_POS_STATES_ENCODING_MAX];

            private readonly uint[] prices = new uint[LzmaBase.NUM_LEN_SYMBOLS << LzmaBase.NUM_POS_STATES_BITS_ENCODING_MAX];

            private uint tableSize;

            /**
             * Methods
             */

            /// <summary>
            /// </summary>
            /// <param name="tableSize"></param>
            public void SetTableSize(uint tableSize)
            {
                this.tableSize = tableSize;
            }

            /// <summary>
            /// </summary>
            /// <param name="symbol"></param>
            /// <param name="posState"></param>
            /// <returns></returns>
            public uint GetPrice(uint symbol, uint posState)
            {
                return prices[posState * LzmaBase.NUM_LEN_SYMBOLS + symbol];
            }

            /// <summary>
            /// </summary>
            /// <param name="posState"></param>
            private void UpdateTable(uint posState)
            {
                SetPrices(posState, tableSize, prices, posState * LzmaBase.NUM_LEN_SYMBOLS);
                counters[posState] = tableSize;
            }

            /// <summary>
            /// </summary>
            /// <param name="numPosStates"></param>
            public void UpdateTables(uint numPosStates)
            {
                for (uint posState = 0; posState < numPosStates; posState++)
                    UpdateTable(posState);
            }

            /// <summary>
            /// </summary>
            /// <param name="rangeEncoder"></param>
            /// <param name="symbol"></param>
            /// <param name="posState"></param>
            public new void Encode(RangeEncoder rangeEncoder, uint symbol, uint posState)
            {
                base.Encode(rangeEncoder, symbol, posState);
                if (--counters[posState] == 0)
                    UpdateTable(posState);
            }
        } // private class LenPriceTableEncoder : LenEncoder

        /// <summary>
        /// </summary>
        private class Optimal
        {
            public uint BackPrev;
            public uint BackPrev2;

            public uint Backs0;
            public uint Backs1;
            public uint Backs2;
            public uint Backs3;
            public uint PosPrev;

            public uint PosPrev2;

            public bool Prev1IsChar;
            public bool Prev2;

            public uint Price;

            public LzmaBase.State State;

            /**
             * Methods
             */

            /// <summary>
            /// </summary>
            public void MakeAsChar()
            {
                BackPrev = 0xFFFFFFFF;
                Prev1IsChar = false;
            }

            /// <summary>
            /// </summary>
            public void MakeAsShortRep()
            {
                BackPrev = 0;
                Prev1IsChar = false;
            }

            /// <summary>
            /// </summary>
            /// <returns></returns>
            public bool IsShortRep()
            {
                return BackPrev == 0;
            }
        } // private class Optimal

        /*
        ** Methods
        */

        /// <summary>
        /// Static initializer for the <see cref="LzmaEncoder" /> class.
        /// </summary>
        static LzmaEncoder()
        {
            const byte FAST_SLOTS = 22;
            int c = 2;
            fastPos[0] = 0;
            fastPos[1] = 1;

            for (byte slotFast = 2; slotFast < FAST_SLOTS; slotFast++)
            {
                uint k = (uint)1 << ((slotFast >> 1) - 1);
                for (uint j = 0; j < k; j++, c++)
                    fastPos[c] = slotFast;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LzmaEncoder" /> class.
        /// </summary>
        public LzmaEncoder()
        {
            for (int i = 0; i < NUM_OPTS; i++)
                optimum[i] = new Optimal();
            for (int i = 0; i < LzmaBase.NUM_LEN_TO_POS_STATES; i++)
                posSlotEncoder[i] = new BitTreeEncoder(LzmaBase.NUM_POS_SLOT_BITS);
        }

        /// <summary>
        /// </summary>
        /// <param name="inStream"></param>
        /// <param name="outStream"></param>
        /// <param name="inSize"></param>
        /// <param name="outSize"></param>
        /// <param name="progress"></param>
        public void Code(Stream inStream, Stream outStream, long inSize, long outSize, ICodeProgress progress)
        {
            needReleaseMFStream = false;
            try
            {
                SetStreams(inStream, outStream, inSize, outSize);
                while (true)
                {
                    long processedInSize;
                    long processedOutSize;
                    bool finished;
                    CodeOneBlock(out processedInSize, out processedOutSize, out finished);
                    if (finished)
                        return;
                    if (progress != null) progress.SetProgress(processedInSize, processedOutSize);
                }
            }
            finally
            {
                ReleaseStreams();
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="propIDs"></param>
        /// <param name="properties"></param>
        public void SetCoderProperties(CoderPropID[] propIDs, object[] properties)
        {
            for (uint i = 0; i < properties.Length; i++)
            {
                object prop = properties[i];
                switch (propIDs[i])
                {
                    case CoderPropID.NumFastBytes:
                        {
                            if (!(prop is int))
                                throw new InvalidParamException();
                            int numFastBytes = (int)prop;
                            if (numFastBytes < 5 || numFastBytes > LzmaBase.MATCH_MAX_LEN)
                                throw new InvalidParamException();
                            this.numFastBytes = (uint)numFastBytes;
                            break;
                        }
                    case CoderPropID.Algorithm:
                        {
                            /*
                            if (!(prop is int))
                                throw new InvalidParamException();
                            int maximize = (int)prop;
                            _fastMode = (maximize == 0);
                            _maxMode = (maximize >= 2);
                            */
                            break;
                        }
                    case CoderPropID.MatchFinder:
                        {
                            if (!(prop is string))
                                throw new InvalidParamException();
                            EMatchFinderType matchFinderIndexPrev = matchFinderType;
                            int m = FindMatchFinder(((string)prop).ToUpper());
                            if (m < 0)
                                throw new InvalidParamException();
                            matchFinderType = (EMatchFinderType)m;
                            if (matchFinder != null && matchFinderIndexPrev != matchFinderType)
                            {
                                dictionarySizePrev = 0xFFFFFFFF;
                                matchFinder = null;
                            }

                            break;
                        }
                    case CoderPropID.DictionarySize:
                        {
                            const int kDicLogSizeMaxCompress = 30;
                            if (!(prop is int))
                                throw new InvalidParamException();
                            ;
                            int dictionarySize = (int)prop;
                            if (dictionarySize < (uint)(1 << LzmaBase.DIC_LOG_SIZE_MIN) ||
                                dictionarySize > (uint)(1 << kDicLogSizeMaxCompress))
                                throw new InvalidParamException();
                            this.dictionarySize = (uint)dictionarySize;
                            int dicLogSize;
                            for (dicLogSize = 0; dicLogSize < (uint)kDicLogSizeMaxCompress; dicLogSize++)
                            {
                                if (dictionarySize <= (uint)1 << dicLogSize)
                                    break;
                            }

                            distTableSize = (uint)dicLogSize * 2;
                            break;
                        }
                    case CoderPropID.PosStateBits:
                        {
                            if (!(prop is int))
                                throw new InvalidParamException();
                            int v = (int)prop;
                            if (v < 0 || v > (uint)LzmaBase.NUM_POS_STATES_BITS_ENCODING_MAX)
                                throw new InvalidParamException();
                            posStateBits = v;
                            posStateMask = ((uint)1 << posStateBits) - 1;
                            break;
                        }
                    case CoderPropID.LitPosBits:
                        {
                            if (!(prop is int))
                                throw new InvalidParamException();
                            int v = (int)prop;
                            if (v < 0 || v > LzmaBase.NUM_LIT_POS_STATES_ENCODING_MAX)
                                throw new InvalidParamException();
                            numLiteralPosStateBits = v;
                            break;
                        }
                    case CoderPropID.LitContextBits:
                        {
                            if (!(prop is int))
                                throw new InvalidParamException();
                            int v = (int)prop;
                            if (v < 0 || v > LzmaBase.NUM_LIT_CONTEXT_BITS_MAX)
                                throw new InvalidParamException();
                            ;
                            numLiteralContextBits = v;
                            break;
                        }
                    case CoderPropID.EndMarker:
                        {
                            if (!(prop is bool))
                                throw new InvalidParamException();
                            SetWriteEndMarkerMode((bool)prop);
                            break;
                        }
                    default:
                        throw new InvalidParamException();
                }
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="outStream"></param>
        public void WriteCoderProperties(Stream outStream)
        {
            properties[0] = (byte)((posStateBits * 5 + numLiteralPosStateBits) * 9 + numLiteralContextBits);
            for (int i = 0; i < 4; i++)
                properties[1 + i] = (byte)((dictionarySize >> (8 * i)) & 0xFF);
            outStream.Write(properties, 0, PROP_SIZE);
        }

        /// <summary>
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        private static uint GetPosSlot(uint pos)
        {
            if (pos < 1 << 11)
                return fastPos[pos];
            if (pos < 1 << 21)
                return (uint)(fastPos[pos >> 10] + 20);
            return (uint)(fastPos[pos >> 20] + 40);
        }

        /// <summary>
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        private static uint GetPosSlot2(uint pos)
        {
            if (pos < 1 << 17)
                return (uint)(fastPos[pos >> 6] + 12);
            if (pos < 1 << 27)
                return (uint)(fastPos[pos >> 16] + 32);
            return (uint)(fastPos[pos >> 26] + 52);
        }

        /// <summary>
        /// </summary>
        private void BaseInit()
        {
            state.Init();
            previousByte = 0;
            for (uint i = 0; i < LzmaBase.NUM_REP_DISTANCE; i++)
                repDistances[i] = 0;
        }

        /// <summary>
        /// </summary>
        private void Create()
        {
            if (matchFinder == null)
            {
                BinTree bt = new BinTree();
                int numHashBytes = 4;
                if (matchFinderType == EMatchFinderType.BT2)
                    numHashBytes = 2;

                bt.SetType(numHashBytes);
                matchFinder = bt;
            }

            literalEncoder.Create(numLiteralPosStateBits, numLiteralContextBits);

            if (dictionarySize == dictionarySizePrev && numFastBytesPrev == numFastBytes)
                return;

            matchFinder.Create(dictionarySize, NUM_OPTS, numFastBytes, LzmaBase.MATCH_MAX_LEN + 1);
            dictionarySizePrev = dictionarySize;
            numFastBytesPrev = numFastBytes;
        }

        /// <summary>
        /// </summary>
        /// <param name="writeEndMarker"></param>
        private void SetWriteEndMarkerMode(bool writeEndMarker)
        {
            writeEndMark = writeEndMarker;
        }

        /// <summary>
        /// </summary>
        private void Init()
        {
            BaseInit();
            rangeEncoder.Init();

            uint i;
            for (i = 0; i < LzmaBase.NUM_STATES; i++)
            {
                for (uint j = 0; j <= posStateMask; j++)
                {
                    uint complexState = (i << LzmaBase.NUM_POS_STATES_BITS_MAX) + j;
                    isMatch[complexState].Init();
                    isRep0Long[complexState].Init();
                }

                isRep[i].Init();
                isRepG0[i].Init();
                isRepG1[i].Init();
                isRepG2[i].Init();
            }

            literalEncoder.Init();

            for (i = 0; i < LzmaBase.NUM_LEN_TO_POS_STATES; i++)
                posSlotEncoder[i].Init();
            for (i = 0; i < LzmaBase.NUM_FULL_DISTANCE - LzmaBase.END_POS_MODEL_IDX; i++)
                posEncoders[i].Init();

            lenEncoder.Init((uint)1 << posStateBits);
            repMatchLenEncoder.Init((uint)1 << posStateBits);

            posAlignEncoder.Init();

            longestMatchWasFound = false;
            optimumEndIndex = 0;
            optimumCurrentIndex = 0;
            additionalOffset = 0;
        }

        /// <summary>
        /// </summary>
        /// <param name="lenRes"></param>
        /// <param name="numDistancePairs"></param>
        private void ReadMatchDistances(out uint lenRes, out uint numDistancePairs)
        {
            lenRes = 0;
            numDistancePairs = matchFinder.GetMatches(matchDistances);
            if (numDistancePairs > 0)
            {
                lenRes = matchDistances[numDistancePairs - 2];
                if (lenRes == numFastBytes)
                    lenRes += matchFinder.GetMatchLen((int)lenRes - 1, matchDistances[numDistancePairs - 1], LzmaBase.MATCH_MAX_LEN - lenRes);
            }

            additionalOffset++;
        }

        /// <summary>
        /// </summary>
        /// <param name="num"></param>
        private void MovePos(uint num)
        {
            if (num > 0)
            {
                matchFinder.Skip(num);
                additionalOffset += num;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="state"></param>
        /// <param name="posState"></param>
        /// <returns></returns>
        private uint GetRepLen1Price(LzmaBase.State state, uint posState)
        {
            return isRepG0[state.Index].GetPrice0() + isRep0Long[(state.Index << LzmaBase.NUM_POS_STATES_BITS_MAX) + posState].GetPrice0();
        }

        /// <summary>
        /// </summary>
        /// <param name="repIndex"></param>
        /// <param name="state"></param>
        /// <param name="posState"></param>
        /// <returns></returns>
        private uint GetPureRepPrice(uint repIndex, LzmaBase.State state, uint posState)
        {
            uint price;
            if (repIndex == 0)
            {
                price = isRepG0[state.Index].GetPrice0();
                price += isRep0Long[(state.Index << LzmaBase.NUM_POS_STATES_BITS_MAX) + posState].GetPrice1();
            }
            else
            {
                price = isRepG0[state.Index].GetPrice1();
                if (repIndex == 1)
                    price += isRepG1[state.Index].GetPrice0();
                else
                {
                    price += isRepG1[state.Index].GetPrice1();
                    price += isRepG2[state.Index].GetPrice(repIndex - 2);
                }
            }

            return price;
        }

        /// <summary>
        /// </summary>
        /// <param name="repIndex"></param>
        /// <param name="len"></param>
        /// <param name="state"></param>
        /// <param name="posState"></param>
        /// <returns></returns>
        private uint GetRepPrice(uint repIndex, uint len, LzmaBase.State state, uint posState)
        {
            uint price = repMatchLenEncoder.GetPrice(len - LzmaBase.MATCH_MIN_LEN, posState);
            return price + GetPureRepPrice(repIndex, state, posState);
        }

        /// <summary>
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="len"></param>
        /// <param name="posState"></param>
        /// <returns></returns>
        private uint GetPosLenPrice(uint pos, uint len, uint posState)
        {
            uint price;
            uint lenToPosState = LzmaBase.GetLenToPosState(len);
            if (pos < LzmaBase.NUM_FULL_DISTANCE)
                price = distancesPrices[lenToPosState * LzmaBase.NUM_FULL_DISTANCE + pos];
            else
                price = posSlotPrices[(lenToPosState << LzmaBase.NUM_POS_SLOT_BITS) + GetPosSlot2(pos)] + alignPrices[pos & LzmaBase.ALIGN_MASK];

            return price + lenEncoder.GetPrice(len - LzmaBase.MATCH_MIN_LEN, posState);
        }

        /// <summary>
        /// </summary>
        /// <param name="backRes"></param>
        /// <param name="cur"></param>
        /// <returns></returns>
        private uint Backward(out uint backRes, uint cur)
        {
            optimumEndIndex = cur;
            uint posMem = optimum[cur].PosPrev;
            uint backMem = optimum[cur].BackPrev;
            do
            {
                if (optimum[cur].Prev1IsChar)
                {
                    optimum[posMem].MakeAsChar();
                    optimum[posMem].PosPrev = posMem - 1;
                    if (optimum[cur].Prev2)
                    {
                        optimum[posMem - 1].Prev1IsChar = false;
                        optimum[posMem - 1].PosPrev = optimum[cur].PosPrev2;
                        optimum[posMem - 1].BackPrev = optimum[cur].BackPrev2;
                    }
                }

                uint posPrev = posMem;
                uint backCur = backMem;

                backMem = optimum[posPrev].BackPrev;
                posMem = optimum[posPrev].PosPrev;

                optimum[posPrev].BackPrev = backCur;
                optimum[posPrev].PosPrev = cur;
                cur = posPrev;
            } while (cur > 0);

            backRes = optimum[0].BackPrev;
            optimumCurrentIndex = optimum[0].PosPrev;
            return optimumCurrentIndex;
        }

        /// <summary>
        /// </summary>
        /// <param name="position"></param>
        /// <param name="backRes"></param>
        /// <returns></returns>
        private uint GetOptimum(uint position, out uint backRes)
        {
            if (optimumEndIndex != optimumCurrentIndex)
            {
                uint lenRes = optimum[optimumCurrentIndex].PosPrev - optimumCurrentIndex;
                backRes = optimum[optimumCurrentIndex].BackPrev;
                optimumCurrentIndex = optimum[optimumCurrentIndex].PosPrev;
                return lenRes;
            }

            optimumCurrentIndex = optimumEndIndex = 0;

            uint lenMain, numDistancePairs;
            if (!longestMatchWasFound)
                ReadMatchDistances(out lenMain, out numDistancePairs);
            else
            {
                lenMain = longestMatchLength;
                numDistancePairs = this.numDistancePairs;
                longestMatchWasFound = false;
            }

            uint numAvailableBytes = matchFinder.GetNumAvailableBytes() + 1;
            if (numAvailableBytes < 2)
            {
                backRes = 0xFFFFFFFF;
                return 1;
            }

            if (numAvailableBytes > LzmaBase.MATCH_MAX_LEN)
                numAvailableBytes = LzmaBase.MATCH_MAX_LEN;

            uint repMaxIndex = 0;
            uint i;
            for (i = 0; i < LzmaBase.NUM_REP_DISTANCE; i++)
            {
                reps[i] = repDistances[i];
                repLens[i] = matchFinder.GetMatchLen(0 - 1, reps[i], LzmaBase.MATCH_MAX_LEN);
                if (repLens[i] > repLens[repMaxIndex])
                    repMaxIndex = i;
            }

            if (repLens[repMaxIndex] >= numFastBytes)
            {
                backRes = repMaxIndex;
                uint lenRes = repLens[repMaxIndex];
                MovePos(lenRes - 1);
                return lenRes;
            }

            if (lenMain >= numFastBytes)
            {
                backRes = matchDistances[numDistancePairs - 1] + LzmaBase.NUM_REP_DISTANCE;
                MovePos(lenMain - 1);
                return lenMain;
            }

            byte currentByte = matchFinder.GetIndexByte(0 - 1);
            byte matchByte = matchFinder.GetIndexByte((int)(0 - repDistances[0] - 1 - 1));

            if (lenMain < 2 && currentByte != matchByte && repLens[repMaxIndex] < 2)
            {
                backRes = 0xFFFFFFFF;
                return 1;
            }

            optimum[0].State = state;

            uint posState = position & posStateMask;

            optimum[1].Price = isMatch[(state.Index << LzmaBase.NUM_POS_STATES_BITS_MAX) + posState].GetPrice0() +
                               literalEncoder.GetSubCoder(position, previousByte).GetPrice(!state.IsCharState(), matchByte, currentByte);
            optimum[1].MakeAsChar();

            uint matchPrice = isMatch[(state.Index << LzmaBase.NUM_POS_STATES_BITS_MAX) + posState].GetPrice1();
            uint repMatchPrice = matchPrice + isRep[state.Index].GetPrice1();

            if (matchByte == currentByte)
            {
                uint shortRepPrice = repMatchPrice + GetRepLen1Price(state, posState);
                if (shortRepPrice < optimum[1].Price)
                {
                    optimum[1].Price = shortRepPrice;
                    optimum[1].MakeAsShortRep();
                }
            }

            uint lenEnd = lenMain >= repLens[repMaxIndex] ? lenMain : repLens[repMaxIndex];

            if (lenEnd < 2)
            {
                backRes = optimum[1].BackPrev;
                return 1;
            }

            optimum[1].PosPrev = 0;

            optimum[0].Backs0 = reps[0];
            optimum[0].Backs1 = reps[1];
            optimum[0].Backs2 = reps[2];
            optimum[0].Backs3 = reps[3];

            uint len = lenEnd;
            do
            {
                optimum[len--].Price = IFINITY_PRICE;
            } while (len >= 2);

            for (i = 0; i < LzmaBase.NUM_REP_DISTANCE; i++)
            {
                uint repLen = repLens[i];
                if (repLen < 2)
                    continue;
                uint price = repMatchPrice + GetPureRepPrice(i, state, posState);
                do
                {
                    uint curAndLenPrice = price + repMatchLenEncoder.GetPrice(repLen - 2, posState);
                    Optimal optimum = this.optimum[repLen];
                    if (curAndLenPrice < optimum.Price)
                    {
                        optimum.Price = curAndLenPrice;
                        optimum.PosPrev = 0;
                        optimum.BackPrev = i;
                        optimum.Prev1IsChar = false;
                    }
                } while (--repLen >= 2);
            }

            uint normalMatchPrice = matchPrice + isRep[state.Index].GetPrice0();

            len = repLens[0] >= 2 ? repLens[0] + 1 : 2;
            if (len <= lenMain)
            {
                uint offs = 0;
                while (len > matchDistances[offs])
                    offs += 2;
                for (; ; len++)
                {
                    uint distance = matchDistances[offs + 1];
                    uint curAndLenPrice = normalMatchPrice + GetPosLenPrice(distance, len, posState);
                    Optimal optimum = this.optimum[len];
                    if (curAndLenPrice < optimum.Price)
                    {
                        optimum.Price = curAndLenPrice;
                        optimum.PosPrev = 0;
                        optimum.BackPrev = distance + LzmaBase.NUM_REP_DISTANCE;
                        optimum.Prev1IsChar = false;
                    }

                    if (len == matchDistances[offs])
                    {
                        offs += 2;
                        if (offs == numDistancePairs)
                            break;
                    }
                }
            }

            uint cur = 0;

            while (true)
            {
                cur++;
                if (cur == lenEnd)
                    return Backward(out backRes, cur);
                uint newLen;
                ReadMatchDistances(out newLen, out numDistancePairs);
                if (newLen >= numFastBytes)
                {
                    this.numDistancePairs = numDistancePairs;
                    longestMatchLength = newLen;
                    longestMatchWasFound = true;
                    return Backward(out backRes, cur);
                }

                position++;
                uint posPrev = optimum[cur].PosPrev;
                LzmaBase.State state;
                if (optimum[cur].Prev1IsChar)
                {
                    posPrev--;
                    if (optimum[cur].Prev2)
                    {
                        state = optimum[optimum[cur].PosPrev2].State;
                        if (optimum[cur].BackPrev2 < LzmaBase.NUM_REP_DISTANCE)
                            state.UpdateRep();
                        else
                            state.UpdateMatch();
                    }
                    else
                        state = optimum[posPrev].State;

                    state.UpdateChar();
                }
                else
                    state = optimum[posPrev].State;

                if (posPrev == cur - 1)
                {
                    if (optimum[cur].IsShortRep())
                        state.UpdateShortRep();
                    else
                        state.UpdateChar();
                }
                else
                {
                    uint pos;
                    if (optimum[cur].Prev1IsChar && optimum[cur].Prev2)
                    {
                        posPrev = optimum[cur].PosPrev2;
                        pos = optimum[cur].BackPrev2;
                        state.UpdateRep();
                    }
                    else
                    {
                        pos = optimum[cur].BackPrev;
                        if (pos < LzmaBase.NUM_REP_DISTANCE)
                            state.UpdateRep();
                        else
                            state.UpdateMatch();
                    }

                    Optimal opt = optimum[posPrev];
                    if (pos < LzmaBase.NUM_REP_DISTANCE)
                    {
                        if (pos == 0)
                        {
                            reps[0] = opt.Backs0;
                            reps[1] = opt.Backs1;
                            reps[2] = opt.Backs2;
                            reps[3] = opt.Backs3;
                        }
                        else if (pos == 1)
                        {
                            reps[0] = opt.Backs1;
                            reps[1] = opt.Backs0;
                            reps[2] = opt.Backs2;
                            reps[3] = opt.Backs3;
                        }
                        else if (pos == 2)
                        {
                            reps[0] = opt.Backs2;
                            reps[1] = opt.Backs0;
                            reps[2] = opt.Backs1;
                            reps[3] = opt.Backs3;
                        }
                        else
                        {
                            reps[0] = opt.Backs3;
                            reps[1] = opt.Backs0;
                            reps[2] = opt.Backs1;
                            reps[3] = opt.Backs2;
                        }
                    }
                    else
                    {
                        reps[0] = pos - LzmaBase.NUM_REP_DISTANCE;
                        reps[1] = opt.Backs0;
                        reps[2] = opt.Backs1;
                        reps[3] = opt.Backs2;
                    }
                }

                optimum[cur].State = state;
                optimum[cur].Backs0 = reps[0];
                optimum[cur].Backs1 = reps[1];
                optimum[cur].Backs2 = reps[2];
                optimum[cur].Backs3 = reps[3];
                uint curPrice = optimum[cur].Price;

                currentByte = matchFinder.GetIndexByte(0 - 1);
                matchByte = matchFinder.GetIndexByte((int)(0 - reps[0] - 1 - 1));

                posState = position & posStateMask;

                uint curAnd1Price = curPrice + isMatch[(state.Index << LzmaBase.NUM_POS_STATES_BITS_MAX) + posState].GetPrice0() +
                    literalEncoder.GetSubCoder(position, matchFinder.GetIndexByte(0 - 2)).GetPrice(!state.IsCharState(), matchByte, currentByte);

                Optimal nextOptimum = optimum[cur + 1];

                bool nextIsChar = false;
                if (curAnd1Price < nextOptimum.Price)
                {
                    nextOptimum.Price = curAnd1Price;
                    nextOptimum.PosPrev = cur;
                    nextOptimum.MakeAsChar();
                    nextIsChar = true;
                }

                matchPrice = curPrice + isMatch[(state.Index << LzmaBase.NUM_POS_STATES_BITS_MAX) + posState].GetPrice1();
                repMatchPrice = matchPrice + isRep[state.Index].GetPrice1();

                if (matchByte == currentByte && !(nextOptimum.PosPrev < cur && nextOptimum.BackPrev == 0))
                {
                    uint shortRepPrice = repMatchPrice + GetRepLen1Price(state, posState);
                    if (shortRepPrice <= nextOptimum.Price)
                    {
                        nextOptimum.Price = shortRepPrice;
                        nextOptimum.PosPrev = cur;
                        nextOptimum.MakeAsShortRep();
                        nextIsChar = true;
                    }
                }

                uint numAvailableBytesFull = matchFinder.GetNumAvailableBytes() + 1;
                numAvailableBytesFull = Math.Min(NUM_OPTS - 1 - cur, numAvailableBytesFull);
                numAvailableBytes = numAvailableBytesFull;

                if (numAvailableBytes < 2)
                    continue;
                if (numAvailableBytes > numFastBytes)
                    numAvailableBytes = numFastBytes;
                if (!nextIsChar && matchByte != currentByte)
                {
                    // try Literal + rep0
                    uint t = Math.Min(numAvailableBytesFull - 1, numFastBytes);
                    uint lenTest2 = matchFinder.GetMatchLen(0, reps[0], t);
                    if (lenTest2 >= 2)
                    {
                        LzmaBase.State state2 = state;
                        state2.UpdateChar();
                        uint posStateNext = (position + 1) & posStateMask;
                        uint nextRepMatchPrice = curAnd1Price + isMatch[(state2.Index << LzmaBase.NUM_POS_STATES_BITS_MAX) + posStateNext].GetPrice1() +
                            isRep[state2.Index].GetPrice1();
                        {
                            uint offset = cur + 1 + lenTest2;
                            while (lenEnd < offset)
                                this.optimum[++lenEnd].Price = IFINITY_PRICE;
                            uint curAndLenPrice = nextRepMatchPrice + GetRepPrice(0, lenTest2, state2, posStateNext);
                            Optimal optimum = this.optimum[offset];
                            if (curAndLenPrice < optimum.Price)
                            {
                                optimum.Price = curAndLenPrice;
                                optimum.PosPrev = cur + 1;
                                optimum.BackPrev = 0;
                                optimum.Prev1IsChar = true;
                                optimum.Prev2 = false;
                            }
                        }
                    }
                }

                uint startLen = 2; // speed optimization

                for (uint repIndex = 0; repIndex < LzmaBase.NUM_REP_DISTANCE; repIndex++)
                {
                    uint lenTest = matchFinder.GetMatchLen(0 - 1, reps[repIndex], numAvailableBytes);
                    if (lenTest < 2)
                        continue;
                    uint lenTestTemp = lenTest;
                    do
                    {
                        while (lenEnd < cur + lenTest)
                            this.optimum[++lenEnd].Price = IFINITY_PRICE;
                        uint curAndLenPrice = repMatchPrice + GetRepPrice(repIndex, lenTest, state, posState);
                        Optimal optimum = this.optimum[cur + lenTest];
                        if (curAndLenPrice < optimum.Price)
                        {
                            optimum.Price = curAndLenPrice;
                            optimum.PosPrev = cur;
                            optimum.BackPrev = repIndex;
                            optimum.Prev1IsChar = false;
                        }
                    } while (--lenTest >= 2);

                    lenTest = lenTestTemp;

                    if (repIndex == 0)
                        startLen = lenTest + 1;

                    // if (_maxMode)
                    if (lenTest < numAvailableBytesFull)
                    {
                        uint t = Math.Min(numAvailableBytesFull - 1 - lenTest, numFastBytes);
                        uint lenTest2 = matchFinder.GetMatchLen((int)lenTest, reps[repIndex], t);
                        if (lenTest2 >= 2)
                        {
                            LzmaBase.State state2 = state;
                            state2.UpdateRep();
                            uint posStateNext = (position + lenTest) & posStateMask;
                            uint curAndLenCharPrice = repMatchPrice + GetRepPrice(repIndex, lenTest, state, posState) +
                                                      isMatch[(state2.Index << LzmaBase.NUM_POS_STATES_BITS_MAX) + posStateNext].GetPrice0() +
                                                      literalEncoder.GetSubCoder(position + lenTest, matchFinder.GetIndexByte((int)lenTest - 1 - 1)).GetPrice(true,
                                                              matchFinder.GetIndexByte((int)lenTest - 1 - (int)(reps[repIndex] + 1)), 
                                                              matchFinder.GetIndexByte((int)lenTest - 1));
                            state2.UpdateChar();
                            posStateNext = (position + lenTest + 1) & posStateMask;
                            uint nextMatchPrice = curAndLenCharPrice + isMatch[(state2.Index << LzmaBase.NUM_POS_STATES_BITS_MAX) + posStateNext].GetPrice1();
                            uint nextRepMatchPrice = nextMatchPrice + isRep[state2.Index].GetPrice1();

                            // for(; lenTest2 >= 2; lenTest2--)
                            {
                                uint offset = lenTest + 1 + lenTest2;
                                while (lenEnd < cur + offset)
                                    this.optimum[++lenEnd].Price = IFINITY_PRICE;
                                uint curAndLenPrice =
                                    nextRepMatchPrice + GetRepPrice(0, lenTest2, state2, posStateNext);
                                Optimal optimum = this.optimum[cur + offset];
                                if (curAndLenPrice < optimum.Price)
                                {
                                    optimum.Price = curAndLenPrice;
                                    optimum.PosPrev = cur + lenTest + 1;
                                    optimum.BackPrev = 0;
                                    optimum.Prev1IsChar = true;
                                    optimum.Prev2 = true;
                                    optimum.PosPrev2 = cur;
                                    optimum.BackPrev2 = repIndex;
                                }
                            }
                        }
                    }
                }

                if (newLen > numAvailableBytes)
                {
                    newLen = numAvailableBytes;
                    for (numDistancePairs = 0; newLen > matchDistances[numDistancePairs]; numDistancePairs += 2) ;
                    matchDistances[numDistancePairs] = newLen;
                    numDistancePairs += 2;
                }

                if (newLen >= startLen)
                {
                    normalMatchPrice = matchPrice + isRep[state.Index].GetPrice0();
                    while (lenEnd < cur + newLen)
                        optimum[++lenEnd].Price = IFINITY_PRICE;

                    uint offs = 0;
                    while (startLen > matchDistances[offs])
                        offs += 2;

                    for (uint lenTest = startLen; ; lenTest++)
                    {
                        uint curBack = matchDistances[offs + 1];
                        uint curAndLenPrice = normalMatchPrice + GetPosLenPrice(curBack, lenTest, posState);
                        Optimal optimum = this.optimum[cur + lenTest];
                        if (curAndLenPrice < optimum.Price)
                        {
                            optimum.Price = curAndLenPrice;
                            optimum.PosPrev = cur;
                            optimum.BackPrev = curBack + LzmaBase.NUM_REP_DISTANCE;
                            optimum.Prev1IsChar = false;
                        }

                        if (lenTest == matchDistances[offs])
                        {
                            if (lenTest < numAvailableBytesFull)
                            {
                                uint t = Math.Min(numAvailableBytesFull - 1 - lenTest, numFastBytes);
                                uint lenTest2 = matchFinder.GetMatchLen((int)lenTest, curBack, t);
                                if (lenTest2 >= 2)
                                {
                                    LzmaBase.State state2 = state;
                                    state2.UpdateMatch();
                                    uint posStateNext = (position + lenTest) & posStateMask;
                                    uint curAndLenCharPrice = curAndLenPrice + isMatch[(state2.Index << LzmaBase.NUM_POS_STATES_BITS_MAX) + posStateNext].GetPrice0() + 
                                        literalEncoder.GetSubCoder(position + lenTest, matchFinder.GetIndexByte((int)lenTest - 1 - 1)).GetPrice(true, 
                                            matchFinder.GetIndexByte((int)lenTest - (int)(curBack + 1) - 1), matchFinder.GetIndexByte((int)lenTest - 1));
                                    state2.UpdateChar();
                                    posStateNext = (position + lenTest + 1) & posStateMask;
                                    uint nextMatchPrice = curAndLenCharPrice + isMatch[(state2.Index << LzmaBase.NUM_POS_STATES_BITS_MAX) + posStateNext].GetPrice1();
                                    uint nextRepMatchPrice = nextMatchPrice + isRep[state2.Index].GetPrice1();

                                    uint offset = lenTest + 1 + lenTest2;
                                    while (lenEnd < cur + offset)
                                        this.optimum[++lenEnd].Price = IFINITY_PRICE;
                                    curAndLenPrice = nextRepMatchPrice + GetRepPrice(0, lenTest2, state2, posStateNext);
                                    optimum = this.optimum[cur + offset];
                                    if (curAndLenPrice < optimum.Price)
                                    {
                                        optimum.Price = curAndLenPrice;
                                        optimum.PosPrev = cur + lenTest + 1;
                                        optimum.BackPrev = 0;
                                        optimum.Prev1IsChar = true;
                                        optimum.Prev2 = true;
                                        optimum.PosPrev2 = cur;
                                        optimum.BackPrev2 = curBack + LzmaBase.NUM_REP_DISTANCE;
                                    }
                                }
                            }

                            offs += 2;
                            if (offs == numDistancePairs)
                                break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="smallDist"></param>
        /// <param name="bigDist"></param>
        /// <returns></returns>
        private bool ChangePair(uint smallDist, uint bigDist)
        {
            const int kDif = 7;
            return smallDist < (uint)1 << (32 - kDif) && bigDist >= smallDist << kDif;
        }

        /// <summary>
        /// </summary>
        /// <param name="posState"></param>
        private void WriteEndMarker(uint posState)
        {
            if (!writeEndMark)
                return;

            isMatch[(state.Index << LzmaBase.NUM_POS_STATES_BITS_MAX) + posState].Encode(rangeEncoder, 1);
            isRep[state.Index].Encode(rangeEncoder, 0);
            state.UpdateMatch();

            uint len = LzmaBase.MATCH_MIN_LEN;
            lenEncoder.Encode(rangeEncoder, len - LzmaBase.MATCH_MIN_LEN, posState);

            uint posSlot = (1 << LzmaBase.NUM_POS_SLOT_BITS) - 1;
            uint lenToPosState = LzmaBase.GetLenToPosState(len);
            posSlotEncoder[lenToPosState].Encode(rangeEncoder, posSlot);

            int footerBits = 30;
            uint posReduced = ((uint)1 << footerBits) - 1;
            rangeEncoder.EncodeDirectBits(posReduced >> LzmaBase.NUM_ALIGN_BITS, footerBits - LzmaBase.NUM_ALIGN_BITS);
            posAlignEncoder.ReverseEncode(rangeEncoder, posReduced & LzmaBase.ALIGN_MASK);
        }

        /// <summary>
        /// </summary>
        /// <param name="nowPos"></param>
        private void Flush(uint nowPos)
        {
            ReleaseMFStream();
            WriteEndMarker(nowPos & posStateMask);
            rangeEncoder.FlushData();
            rangeEncoder.FlushStream();
        }

        /// <summary>
        /// </summary>
        /// <param name="inSize"></param>
        /// <param name="outSize"></param>
        /// <param name="finished"></param>
        public void CodeOneBlock(out long inSize, out long outSize, out bool finished)
        {
            inSize = 0;
            outSize = 0;
            finished = true;

            if (inStream != null)
            {
                matchFinder.SetStream(inStream);
                matchFinder.Init();
                needReleaseMFStream = true;
                inStream = null;
                if (trainSize > 0)
                    matchFinder.Skip(trainSize);
            }

            if (this.finished)
                return;
            this.finished = true;

            long progressPosValuePrev = nowPos64;
            if (nowPos64 == 0)
            {
                if (matchFinder.GetNumAvailableBytes() == 0)
                {
                    Flush((uint)nowPos64);
                    return;
                }

                uint len, numDistancePairs; // it's not used
                ReadMatchDistances(out len, out numDistancePairs);

                uint posState = (uint)nowPos64 & posStateMask;
                isMatch[(state.Index << LzmaBase.NUM_POS_STATES_BITS_MAX) + posState].Encode(rangeEncoder, 0);
                state.UpdateChar();

                byte curByte = matchFinder.GetIndexByte((int)(0 - additionalOffset));
                literalEncoder.GetSubCoder((uint)nowPos64, previousByte).Encode(rangeEncoder, curByte);
                previousByte = curByte;
                additionalOffset--;
                nowPos64++;
            }

            if (matchFinder.GetNumAvailableBytes() == 0)
            {
                Flush((uint)nowPos64);
                return;
            }

            while (true)
            {
                uint pos;
                uint len = GetOptimum((uint)nowPos64, out pos);

                uint posState = (uint)nowPos64 & posStateMask;
                uint complexState = (state.Index << LzmaBase.NUM_POS_STATES_BITS_MAX) + posState;
                if (len == 1 && pos == 0xFFFFFFFF)
                {
                    isMatch[complexState].Encode(rangeEncoder, 0);
                    byte curByte = matchFinder.GetIndexByte((int)(0 - additionalOffset));
                    LiteralEncoder.Encoder subCoder = literalEncoder.GetSubCoder((uint)nowPos64, previousByte);
                    if (!state.IsCharState())
                    {
                        byte matchByte = matchFinder.GetIndexByte((int)(0 - repDistances[0] - 1 - additionalOffset));
                        subCoder.EncodeMatched(rangeEncoder, matchByte, curByte);
                    }
                    else
                        subCoder.Encode(rangeEncoder, curByte);

                    previousByte = curByte;
                    state.UpdateChar();
                }
                else
                {
                    isMatch[complexState].Encode(rangeEncoder, 1);
                    if (pos < LzmaBase.NUM_REP_DISTANCE)
                    {
                        isRep[state.Index].Encode(rangeEncoder, 1);
                        if (pos == 0)
                        {
                            isRepG0[state.Index].Encode(rangeEncoder, 0);
                            if (len == 1)
                                isRep0Long[complexState].Encode(rangeEncoder, 0);
                            else
                                isRep0Long[complexState].Encode(rangeEncoder, 1);
                        }
                        else
                        {
                            isRepG0[state.Index].Encode(rangeEncoder, 1);
                            if (pos == 1)
                                isRepG1[state.Index].Encode(rangeEncoder, 0);
                            else
                            {
                                isRepG1[state.Index].Encode(rangeEncoder, 1);
                                isRepG2[state.Index].Encode(rangeEncoder, pos - 2);
                            }
                        }

                        if (len == 1)
                            state.UpdateShortRep();
                        else
                        {
                            repMatchLenEncoder.Encode(rangeEncoder, len - LzmaBase.MATCH_MIN_LEN, posState);
                            state.UpdateRep();
                        }

                        uint distance = repDistances[pos];
                        if (pos != 0)
                        {
                            for (uint i = pos; i >= 1; i--)
                                repDistances[i] = repDistances[i - 1];
                            repDistances[0] = distance;
                        }
                    }
                    else
                    {
                        isRep[state.Index].Encode(rangeEncoder, 0);
                        state.UpdateMatch();
                        lenEncoder.Encode(rangeEncoder, len - LzmaBase.MATCH_MIN_LEN, posState);
                        pos -= LzmaBase.NUM_REP_DISTANCE;

                        uint posSlot = GetPosSlot(pos);
                        uint lenToPosState = LzmaBase.GetLenToPosState(len);
                        posSlotEncoder[lenToPosState].Encode(rangeEncoder, posSlot);

                        if (posSlot >= LzmaBase.START_POS_MODEL_IDX)
                        {
                            int footerBits = (int)((posSlot >> 1) - 1);
                            uint baseVal = (2 | (posSlot & 1)) << footerBits;
                            uint posReduced = pos - baseVal;

                            if (posSlot < LzmaBase.END_POS_MODEL_IDX)
                                BitTreeEncoder.ReverseEncode(posEncoders, baseVal - posSlot - 1, rangeEncoder, footerBits, posReduced);
                            else
                            {
                                rangeEncoder.EncodeDirectBits(posReduced >> LzmaBase.NUM_ALIGN_BITS, footerBits - LzmaBase.NUM_ALIGN_BITS);
                                posAlignEncoder.ReverseEncode(rangeEncoder, posReduced & LzmaBase.ALIGN_MASK);
                                alignPriceCount++;
                            }
                        }

                        uint distance = pos;
                        for (uint i = LzmaBase.NUM_REP_DISTANCE - 1; i >= 1; i--)
                            repDistances[i] = repDistances[i - 1];
                        repDistances[0] = distance;
                        matchPriceCount++;
                    }

                    previousByte = matchFinder.GetIndexByte((int)(len - 1 - additionalOffset));
                }

                additionalOffset -= len;
                nowPos64 += len;

                if (additionalOffset == 0)
                {
                    // if (!_fastMode)
                    if (matchPriceCount >= 1 << 7)
                        FillDistancesPrices();
                    if (alignPriceCount >= LzmaBase.ALIGN_TABLE_SIZE)
                        FillAlignPrices();
                    inSize = nowPos64;
                    outSize = rangeEncoder.GetProcessedSizeAdd();
                    if (matchFinder.GetNumAvailableBytes() == 0)
                    {
                        Flush((uint)nowPos64);
                        return;
                    }

                    if (nowPos64 - progressPosValuePrev >= 1 << 12)
                    {
                        this.finished = false;
                        finished = false;
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// </summary>
        private void ReleaseMFStream()
        {
            if (matchFinder != null && needReleaseMFStream)
            {
                matchFinder.ReleaseStream();
                needReleaseMFStream = false;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="outStream"></param>
        private void SetOutStream(Stream outStream)
        {
            rangeEncoder.SetStream(outStream);
        }

        /// <summary>
        /// </summary>
        private void ReleaseOutStream()
        {
            rangeEncoder.ReleaseStream();
        }

        /// <summary>
        /// </summary>
        private void ReleaseStreams()
        {
            ReleaseMFStream();
            ReleaseOutStream();
        }

        /// <summary>
        /// </summary>
        /// <param name="inStream"></param>
        /// <param name="outStream"></param>
        /// <param name="inSize"></param>
        /// <param name="outSize"></param>
        private void SetStreams(Stream inStream, Stream outStream, long inSize, long outSize)
        {
            this.inStream = inStream;
            finished = false;
            Create();
            SetOutStream(outStream);
            Init();

            // if (!_fastMode)
            {
                FillDistancesPrices();
                FillAlignPrices();
            }

            lenEncoder.SetTableSize(numFastBytes + 1 - LzmaBase.MATCH_MIN_LEN);
            lenEncoder.UpdateTables((uint)1 << posStateBits);
            repMatchLenEncoder.SetTableSize(numFastBytes + 1 - LzmaBase.MATCH_MIN_LEN);
            repMatchLenEncoder.UpdateTables((uint)1 << posStateBits);

            nowPos64 = 0;
        }

        /// <summary>
        /// </summary>
        private void FillDistancesPrices()
        {
            for (uint i = LzmaBase.START_POS_MODEL_IDX; i < LzmaBase.NUM_FULL_DISTANCE; i++)
            {
                uint posSlot = GetPosSlot(i);
                int footerBits = (int)((posSlot >> 1) - 1);
                uint baseVal = (2 | (posSlot & 1)) << footerBits;
                tempPrices[i] = BitTreeEncoder.ReverseGetPrice(posEncoders, baseVal - posSlot - 1, footerBits, i - baseVal);
            }

            for (uint lenToPosState = 0; lenToPosState < LzmaBase.NUM_LEN_TO_POS_STATES; lenToPosState++)
            {
                uint posSlot;
                BitTreeEncoder encoder = posSlotEncoder[lenToPosState];

                uint st = lenToPosState << LzmaBase.NUM_POS_SLOT_BITS;
                for (posSlot = 0; posSlot < distTableSize; posSlot++)
                    posSlotPrices[st + posSlot] = encoder.GetPrice(posSlot);

                for (posSlot = LzmaBase.END_POS_MODEL_IDX; posSlot < distTableSize; posSlot++)
                    posSlotPrices[st + posSlot] += ((posSlot >> 1) - 1 - LzmaBase.NUM_ALIGN_BITS) << BitEncoder.NUM_BIT_PRICE_SHIFT_BITS;

                uint st2 = lenToPosState * LzmaBase.NUM_FULL_DISTANCE;
                uint i;
                for (i = 0; i < LzmaBase.START_POS_MODEL_IDX; i++)
                    distancesPrices[st2 + i] = posSlotPrices[st + i];

                for (; i < LzmaBase.NUM_FULL_DISTANCE; i++)
                    distancesPrices[st2 + i] = posSlotPrices[st + GetPosSlot(i)] + tempPrices[i];
            }

            matchPriceCount = 0;
        }

        /// <summary>
        /// </summary>
        private void FillAlignPrices()
        {
            for (uint i = 0; i < LzmaBase.ALIGN_TABLE_SIZE; i++)
                alignPrices[i] = posAlignEncoder.ReverseGetPrice(i);
            alignPriceCount = 0;
        }

        /// <summary>
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private static int FindMatchFinder(string s)
        {
            for (int m = 0; m < MatchFinderIDs.Length; m++)
            {
                if (s == MatchFinderIDs[m])
                    return m;
            }

            return -1;
        }

        /// <summary>
        /// </summary>
        /// <param name="trainSize"></param>
        public void SetTrainSize(uint trainSize)
        {
            this.trainSize = trainSize;
        }
    } // public class LzmaEncoder : ICoder, ISetCoderProperties, IWriteCoderProperties
} // namespace TridentFramework.Compression.LZMA
