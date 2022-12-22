/*
 * Copyright (c) 2008-2020 Bryan Biedenkapp., All Rights Reserved.
 * MIT Open Source. Use is subject to license terms.
 * DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
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
    public class LzmaDecoder : ICoder, ISetDecoderProperties
    {
        private readonly BitDecoder[] isMatchDecoders = new BitDecoder[LzmaBase.NUM_STATES << LzmaBase.NUM_POS_STATES_BITS_MAX];

        private readonly BitDecoder[] isRep0LongDecoders = new BitDecoder[LzmaBase.NUM_STATES << LzmaBase.NUM_POS_STATES_BITS_MAX];

        private readonly BitDecoder[] isRepDecoders = new BitDecoder[LzmaBase.NUM_STATES];
        private readonly BitDecoder[] isRepG0Decoders = new BitDecoder[LzmaBase.NUM_STATES];
        private readonly BitDecoder[] isRepG1Decoders = new BitDecoder[LzmaBase.NUM_STATES];
        private readonly BitDecoder[] isRepG2Decoders = new BitDecoder[LzmaBase.NUM_STATES];

        private readonly LenDecoder lenDecoder = new LenDecoder();

        private readonly LiteralDecoder literalDecoder = new LiteralDecoder();

        private readonly OutWindow outWindow = new OutWindow();

        private readonly BitDecoder[] posDecoders = new BitDecoder[LzmaBase.NUM_FULL_DISTANCE - LzmaBase.END_POS_MODEL_IDX];

        private readonly BitTreeDecoder[] posSlotDecoder = new BitTreeDecoder[LzmaBase.NUM_LEN_TO_POS_STATES];
        private readonly RangeDecoder rangeDecoder = new RangeDecoder();
        private readonly LenDecoder repLenDecoder = new LenDecoder();
        private uint dictionarySize;
        private uint dictionarySizeCheck;

        private BitTreeDecoder posAlignDecoder = new BitTreeDecoder(LzmaBase.NUM_ALIGN_BITS);

        private uint posStateMask;

        private bool solid;

        /*
        ** Classes
        */

        /// <summary>
        /// </summary>
        private class LenDecoder
        {
            private readonly BitTreeDecoder[] lowCoder = new BitTreeDecoder[LzmaBase.NUM_POS_STATES_MAX];

            private readonly BitTreeDecoder[] midCoder = new BitTreeDecoder[LzmaBase.NUM_POS_STATES_MAX];

            private BitDecoder choice = new BitDecoder();
            private BitDecoder choice2 = new BitDecoder();
            private BitTreeDecoder highCoder = new BitTreeDecoder(LzmaBase.NUM_HIGH_LEN_BITS);
            private uint numPosStates;

            /**
             * Methods
             */

            /// <summary>
            /// </summary>
            /// <param name="numPosStates"></param>
            public void Create(uint numPosStates)
            {
                for (uint posState = this.numPosStates; posState < numPosStates; posState++)
                {
                    lowCoder[posState] = new BitTreeDecoder(LzmaBase.NUM_LOW_LEN_BITS);
                    midCoder[posState] = new BitTreeDecoder(LzmaBase.NUM_MID_LEN_BITS);
                }

                this.numPosStates = numPosStates;
            }

            /// <summary>
            /// </summary>
            public void Init()
            {
                choice.Init();
                for (uint posState = 0; posState < numPosStates; posState++)
                {
                    lowCoder[posState].Init();
                    midCoder[posState].Init();
                }

                choice2.Init();
                highCoder.Init();
            }

            /// <summary>
            /// </summary>
            /// <param name="rangeDecoder"></param>
            /// <param name="posState"></param>
            /// <returns></returns>
            public uint Decode(RangeDecoder rangeDecoder, uint posState)
            {
                if (choice.Decode(rangeDecoder) == 0) return lowCoder[posState].Decode(rangeDecoder);

                uint symbol = LzmaBase.NUM_LOW_LEN_SYMBOLS;
                if (choice2.Decode(rangeDecoder) == 0)
                    symbol += midCoder[posState].Decode(rangeDecoder);
                else
                {
                    symbol += LzmaBase.NUM_MID_LEN_SYMBOLS;
                    symbol += highCoder.Decode(rangeDecoder);
                }

                return symbol;
            }
        } // private class LenDecoder

        /// <summary>
        /// </summary>
        private class LiteralDecoder
        {
            private Decoder[] coders;
            private int numPosBits;
            private int numPrevBits;
            private uint posMask;

            /**
             * Structures
             */

            /// <summary>
            /// </summary>
            private struct Decoder
            {
                public const int DECODER_COUNT = 768;
                private BitDecoder[] decoders;

                /// <summary>
                /// </summary>
                public void Create()
                {
                    decoders = new BitDecoder[DECODER_COUNT];
                }

                /// <summary>
                /// </summary>
                public void Init()
                {
                    for (int i = 0; i < DECODER_COUNT; i++) decoders[i].Init();
                }

                /// <summary>
                /// </summary>
                /// <param name="rangeDecoder"></param>
                /// <returns></returns>
                public byte DecodeNormal(RangeDecoder rangeDecoder)
                {
                    uint symbol = 1;
                    do
                    {
                        symbol = (symbol << 1) | decoders[symbol].Decode(rangeDecoder);
                    } while (symbol < 0x100);

                    return (byte)symbol;
                }

                /// <summary>
                /// </summary>
                /// <param name="rangeDecoder"></param>
                /// <param name="matchByte"></param>
                /// <returns></returns>
                public byte DecodeWithMatchByte(RangeDecoder rangeDecoder, byte matchByte)
                {
                    uint symbol = 1;
                    do
                    {
                        uint matchBit = (uint)(matchByte >> 7) & 1;
                        matchByte <<= 1;
                        uint bit = decoders[((1 + matchBit) << 8) + symbol].Decode(rangeDecoder);
                        symbol = (symbol << 1) | bit;
                        if (matchBit != bit)
                        {
                            while (symbol < 0x100)
                                symbol = (symbol << 1) | decoders[symbol].Decode(rangeDecoder);
                            break;
                        }
                    } while (symbol < 0x100);

                    return (byte)symbol;
                }
            } // private struct Decoder

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
                coders = new Decoder[numStates];
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
            private uint GetState(uint pos, byte prevByte)
            {
                return ((pos & posMask) << numPrevBits) + (uint)(prevByte >> (8 - numPrevBits));
            }

            /// <summary>
            /// </summary>
            /// <param name="rangeDecoder"></param>
            /// <param name="pos"></param>
            /// <param name="prevByte"></param>
            /// <returns></returns>
            public byte DecodeNormal(RangeDecoder rangeDecoder, uint pos, byte prevByte)
            {
                return coders[GetState(pos, prevByte)].DecodeNormal(rangeDecoder);
            }

            /// <summary>
            /// </summary>
            /// <param name="rangeDecoder"></param>
            /// <param name="pos"></param>
            /// <param name="prevByte"></param>
            /// <param name="matchByte"></param>
            /// <returns></returns>
            public byte DecodeWithMatchByte(RangeDecoder rangeDecoder, uint pos, byte prevByte, byte matchByte)
            {
                return coders[GetState(pos, prevByte)].DecodeWithMatchByte(rangeDecoder, matchByte);
            }
        } // private class LiteralDecoder

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="LzmaDecoder" /> class.
        /// </summary>
        public LzmaDecoder()
        {
            dictionarySize = 0xFFFFFFFF;
            for (int i = 0; i < LzmaBase.NUM_LEN_TO_POS_STATES; i++)
                posSlotDecoder[i] = new BitTreeDecoder(LzmaBase.NUM_POS_SLOT_BITS);
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
            Init(inStream, outStream);

            LzmaBase.State state = new LzmaBase.State();
            state.Init();
            uint rep0 = 0, rep1 = 0, rep2 = 0, rep3 = 0;

            ulong nowPos64 = 0;
            ulong outSize64 = (ulong)outSize;
            if (nowPos64 < outSize64)
            {
                if (isMatchDecoders[state.Index << LzmaBase.NUM_POS_STATES_BITS_MAX].Decode(rangeDecoder) != 0)
                    throw new DataErrorException();
                state.UpdateChar();
                byte b = literalDecoder.DecodeNormal(rangeDecoder, 0, 0);
                outWindow.PutByte(b);
                nowPos64++;
            }

            while (nowPos64 < outSize64)

            // ulong next = Math.Min(nowPos64 + (1 << 18), outSize64);
            // while(nowPos64 < next)
            {
                uint posState = (uint)nowPos64 & posStateMask;
                if (isMatchDecoders[(state.Index << LzmaBase.NUM_POS_STATES_BITS_MAX) + posState].Decode(rangeDecoder) == 0)
                {
                    byte b;
                    byte prevByte = outWindow.GetByte(0);
                    if (!state.IsCharState())
                        b = literalDecoder.DecodeWithMatchByte(rangeDecoder, (uint)nowPos64, prevByte, outWindow.GetByte(rep0));
                    else
                        b = literalDecoder.DecodeNormal(rangeDecoder, (uint)nowPos64, prevByte);

                    outWindow.PutByte(b);
                    state.UpdateChar();
                    nowPos64++;
                }
                else
                {
                    uint len;
                    if (isRepDecoders[state.Index].Decode(rangeDecoder) == 1)
                    {
                        if (isRepG0Decoders[state.Index].Decode(rangeDecoder) == 0)
                        {
                            if (isRep0LongDecoders[(state.Index << LzmaBase.NUM_POS_STATES_BITS_MAX) + posState].Decode(rangeDecoder) == 0)
                            {
                                state.UpdateShortRep();
                                outWindow.PutByte(outWindow.GetByte(rep0));
                                nowPos64++;
                                continue;
                            }
                        }
                        else
                        {
                            uint distance;
                            if (isRepG1Decoders[state.Index].Decode(rangeDecoder) == 0)
                                distance = rep1;
                            else
                            {
                                if (isRepG2Decoders[state.Index].Decode(rangeDecoder) == 0)
                                    distance = rep2;
                                else
                                {
                                    distance = rep3;
                                    rep3 = rep2;
                                }

                                rep2 = rep1;
                            }

                            rep1 = rep0;
                            rep0 = distance;
                        }

                        len = repLenDecoder.Decode(rangeDecoder, posState) + LzmaBase.MATCH_MIN_LEN;
                        state.UpdateRep();
                    }
                    else
                    {
                        rep3 = rep2;
                        rep2 = rep1;
                        rep1 = rep0;
                        len = LzmaBase.MATCH_MIN_LEN + lenDecoder.Decode(rangeDecoder, posState);
                        state.UpdateMatch();
                        uint posSlot = posSlotDecoder[LzmaBase.GetLenToPosState(len)].Decode(rangeDecoder);
                        if (posSlot >= LzmaBase.START_POS_MODEL_IDX)
                        {
                            int numDirectBits = (int)((posSlot >> 1) - 1);
                            rep0 = (2 | (posSlot & 1)) << numDirectBits;
                            if (posSlot < LzmaBase.END_POS_MODEL_IDX)
                                rep0 += BitTreeDecoder.ReverseDecode(posDecoders, rep0 - posSlot - 1, rangeDecoder, numDirectBits);
                            else
                            {
                                rep0 += rangeDecoder.DecodeDirectBits(numDirectBits - LzmaBase.NUM_ALIGN_BITS) << LzmaBase.NUM_ALIGN_BITS;
                                rep0 += posAlignDecoder.ReverseDecode(rangeDecoder);
                            }
                        }
                        else
                            rep0 = posSlot;
                    }

                    if (rep0 >= outWindow.TrainSize + nowPos64 || rep0 >= dictionarySizeCheck)
                    {
                        if (rep0 == 0xFFFFFFFF)
                            break;
                        throw new DataErrorException();
                    }

                    outWindow.CopyBlock(rep0, len);
                    nowPos64 += len;
                }
            }

            outWindow.Flush();
            outWindow.ReleaseStream();
            rangeDecoder.ReleaseStream();
        }

        /// <summary>
        /// </summary>
        /// <param name="properties"></param>
        public void SetDecoderProperties(byte[] properties)
        {
            if (properties.Length < 5)
                throw new InvalidParamException();

            int lc = properties[0] % 9;
            int remainder = properties[0] / 9;
            int lp = remainder % 5;
            int pb = remainder / 5;
            if (pb > LzmaBase.NUM_POS_STATES_BITS_MAX)
                throw new InvalidParamException();

            uint dictionarySize = 0;
            for (int i = 0; i < 4; i++)
                dictionarySize += (uint)properties[1 + i] << (i * 8);
            SetDictionarySize(dictionarySize);
            SetLiteralProperties(lp, lc);
            SetPosBitsProperties(pb);
        }

        /// <summary>
        /// </summary>
        /// <param name="dictionarySize"></param>
        private void SetDictionarySize(uint dictionarySize)
        {
            if (this.dictionarySize != dictionarySize)
            {
                this.dictionarySize = dictionarySize;
                dictionarySizeCheck = Math.Max(this.dictionarySize, 1);
                uint blockSize = Math.Max(dictionarySizeCheck, 1 << 12);
                outWindow.Create(blockSize);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="lp"></param>
        /// <param name="lc"></param>
        private void SetLiteralProperties(int lp, int lc)
        {
            if (lp > 8)
                throw new InvalidParamException();
            if (lc > 8)
                throw new InvalidParamException();
            literalDecoder.Create(lp, lc);
        }

        /// <summary>
        /// </summary>
        /// <param name="pb"></param>
        private void SetPosBitsProperties(int pb)
        {
            if (pb > LzmaBase.NUM_POS_STATES_BITS_MAX)
                throw new InvalidParamException();

            uint numPosStates = (uint)1 << pb;
            lenDecoder.Create(numPosStates);
            repLenDecoder.Create(numPosStates);
            posStateMask = numPosStates - 1;
        }

        /// <summary>
        /// </summary>
        /// <param name="inStream"></param>
        /// <param name="outStream"></param>
        private void Init(Stream inStream, Stream outStream)
        {
            rangeDecoder.Init(inStream);
            outWindow.Init(outStream, solid);

            uint i;
            for (i = 0; i < LzmaBase.NUM_STATES; i++)
            {
                for (uint j = 0; j <= posStateMask; j++)
                {
                    uint index = (i << LzmaBase.NUM_POS_STATES_BITS_MAX) + j;
                    isMatchDecoders[index].Init();
                    isRep0LongDecoders[index].Init();
                }

                isRepDecoders[i].Init();
                isRepG0Decoders[i].Init();
                isRepG1Decoders[i].Init();
                isRepG2Decoders[i].Init();
            }

            literalDecoder.Init();
            for (i = 0; i < LzmaBase.NUM_LEN_TO_POS_STATES; i++)
                posSlotDecoder[i].Init();

            // m_PosSpecDecoder.Init();
            for (i = 0; i < LzmaBase.NUM_FULL_DISTANCE - LzmaBase.END_POS_MODEL_IDX; i++)
                posDecoders[i].Init();

            lenDecoder.Init();
            repLenDecoder.Init();
            posAlignDecoder.Init();
        }

        /// <summary>
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public bool Train(Stream stream)
        {
            solid = true;
            return outWindow.Train(stream);
        }
    } // public class Decoder : ICoder, ISetDecoderProperties
} // namespace TridentFramework.Compression.LZMA
