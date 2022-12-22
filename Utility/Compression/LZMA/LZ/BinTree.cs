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

using TridentFramework.RPC.Utility;

namespace TridentFramework.Compression.LZMA.LZ
{
    /// <summary>
    /// </summary>
    public class BinTree : InWindow, IMatchFinder
    {
        public const uint HASH_2_SIZE = 1 << 10;
        public const uint HASH_3_SIZE = 1 << 16;
        public const uint BT2_HASH_SIZE = 1 << 16;
        public const uint START_MAX_LEN = 1;
        public const uint HASH_3_OFFSET = HASH_2_SIZE;
        public const uint EMPTY_HASH_VALUE = 0;
        public const uint MAX_VAL_FOR_NORMALIZE = ((uint)1 << 31) - 1;

        private uint cutValue = 0xFF;

        private uint cyclicBufferPos;
        private uint cyclicBufferSize;
        private uint[] hash;

        private bool hashArray = true;
        private uint hashMask;
        private uint hashSizeSum;
        private uint kFixHashSize = HASH_2_SIZE + HASH_3_SIZE;
        private uint kMinMatchCheck = 4;

        private uint kNumHashDirectBytes;
        private uint matchMaxLen;

        private uint[] son;

        /*
        ** Methods
        */

        /// <summary>
        /// </summary>
        /// <param name="stream"></param>
        public new void SetStream(Stream stream)
        {
            base.SetStream(stream);
        }

        /// <summary>
        /// </summary>
        public new void ReleaseStream()
        {
            base.ReleaseStream();
        }

        /// <summary>
        /// </summary>
        public new void Init()
        {
            base.Init();
            for (uint i = 0; i < hashSizeSum; i++)
                hash[i] = EMPTY_HASH_VALUE;
            cyclicBufferPos = 0;
            ReduceOffsets(-1);
        }

        /// <summary>
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public new byte GetIndexByte(int index)
        {
            return base.GetIndexByte(index);
        }

        /// <summary>
        /// </summary>
        /// <param name="index"></param>
        /// <param name="distance"></param>
        /// <param name="limit"></param>
        /// <returns></returns>
        public new uint GetMatchLen(int index, uint distance, uint limit)
        {
            return base.GetMatchLen(index, distance, limit);
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public new uint GetNumAvailableBytes()
        {
            return base.GetNumAvailableBytes();
        }

        /// <summary>
        /// </summary>
        /// <param name="historySize"></param>
        /// <param name="keepAddBufferBefore"></param>
        /// <param name="matchMaxLen"></param>
        /// <param name="keepAddBufferAfter"></param>
        public void Create(uint historySize, uint keepAddBufferBefore, uint matchMaxLen, uint keepAddBufferAfter)
        {
            if (historySize > MAX_VAL_FOR_NORMALIZE - 256)
                throw new Exception();
            cutValue = 16 + (matchMaxLen >> 1);

            uint windowReservSize = (historySize + keepAddBufferBefore + matchMaxLen + keepAddBufferAfter) / 2 + 256;

            base.Create(historySize + keepAddBufferBefore, matchMaxLen + keepAddBufferAfter, windowReservSize);

            this.matchMaxLen = matchMaxLen;

            uint cyclicBufferSize = historySize + 1;
            if (this.cyclicBufferSize != cyclicBufferSize)
                son = new uint[(this.cyclicBufferSize = cyclicBufferSize) * 2];

            uint hs = BT2_HASH_SIZE;

            if (hashArray)
            {
                hs = historySize - 1;
                hs |= hs >> 1;
                hs |= hs >> 2;
                hs |= hs >> 4;
                hs |= hs >> 8;
                hs >>= 1;
                hs |= 0xFFFF;
                if (hs > 1 << 24)
                    hs >>= 1;
                hashMask = hs;
                hs++;
                hs += kFixHashSize;
            }

            if (hs != hashSizeSum)
                hash = new uint[hashSizeSum = hs];
        }

        /// <summary>
        /// </summary>
        /// <param name="distances"></param>
        /// <returns></returns>
        public uint GetMatches(uint[] distances)
        {
            uint lenLimit;
            if (pos + matchMaxLen <= streamPos)
                lenLimit = matchMaxLen;
            else
            {
                lenLimit = streamPos - pos;
                if (lenLimit < kMinMatchCheck)
                {
                    MovePos();
                    return 0;
                }
            }

            uint offset = 0;
            uint matchMinPos = pos > cyclicBufferSize ? pos - cyclicBufferSize : 0;
            uint cur = bufferOffset + pos;
            uint maxLen = START_MAX_LEN; // to avoid items for len < hashSize;
            uint hashValue, hash2Value = 0, hash3Value = 0;

            if (hashArray)
            {
                uint temp = CRC.Table[bufBase[cur]] ^ bufBase[cur + 1];
                hash2Value = temp & (HASH_2_SIZE - 1);
                temp ^= (uint)bufBase[cur + 2] << 8;
                hash3Value = temp & (HASH_3_SIZE - 1);
                hashValue = (temp ^ (CRC.Table[bufBase[cur + 3]] << 5)) & hashMask;
            }
            else
                hashValue = bufBase[cur] ^ ((uint)bufBase[cur + 1] << 8);

            uint curMatch = hash[kFixHashSize + hashValue];
            if (hashArray)
            {
                uint curMatch2 = hash[hash2Value];
                uint curMatch3 = hash[HASH_3_OFFSET + hash3Value];
                hash[hash2Value] = pos;
                hash[HASH_3_OFFSET + hash3Value] = pos;
                if (curMatch2 > matchMinPos)
                {
                    if (bufBase[bufferOffset + curMatch2] == bufBase[cur])
                    {
                        distances[offset++] = maxLen = 2;
                        distances[offset++] = pos - curMatch2 - 1;
                    }
                }

                if (curMatch3 > matchMinPos)
                {
                    if (bufBase[bufferOffset + curMatch3] == bufBase[cur])
                    {
                        if (curMatch3 == curMatch2)
                            offset -= 2;
                        distances[offset++] = maxLen = 3;
                        distances[offset++] = pos - curMatch3 - 1;
                        curMatch2 = curMatch3;
                    }
                }

                if (offset != 0 && curMatch2 == curMatch)
                {
                    offset -= 2;
                    maxLen = START_MAX_LEN;
                }
            }

            hash[kFixHashSize + hashValue] = pos;

            uint ptr0 = (cyclicBufferPos << 1) + 1;
            uint ptr1 = cyclicBufferPos << 1;

            uint len0, len1;
            len0 = len1 = kNumHashDirectBytes;

            if (kNumHashDirectBytes != 0)
            {
                if (curMatch > matchMinPos)
                {
                    if (bufBase[bufferOffset + curMatch + kNumHashDirectBytes] != bufBase[cur + kNumHashDirectBytes])
                    {
                        distances[offset++] = maxLen = kNumHashDirectBytes;
                        distances[offset++] = pos - curMatch - 1;
                    }
                }
            }

            uint count = cutValue;

            while (true)
            {
                if (curMatch <= matchMinPos || count-- == 0)
                {
                    son[ptr0] = son[ptr1] = EMPTY_HASH_VALUE;
                    break;
                }

                uint delta = pos - curMatch;
                uint cyclicPos = (delta <= cyclicBufferPos ? cyclicBufferPos - delta : cyclicBufferPos - delta + cyclicBufferSize) << 1;

                uint pby1 = bufferOffset + curMatch;
                uint len = Math.Min(len0, len1);
                if (bufBase[pby1 + len] == bufBase[cur + len])
                {
                    while (++len != lenLimit)
                    {
                        if (bufBase[pby1 + len] != bufBase[cur + len])
                            break;
                    }

                    if (maxLen < len)
                    {
                        distances[offset++] = maxLen = len;
                        distances[offset++] = delta - 1;
                        if (len == lenLimit)
                        {
                            son[ptr1] = son[cyclicPos];
                            son[ptr0] = son[cyclicPos + 1];
                            break;
                        }
                    }
                }

                if (bufBase[pby1 + len] < bufBase[cur + len])
                {
                    son[ptr1] = curMatch;
                    ptr1 = cyclicPos + 1;
                    curMatch = son[ptr1];
                    len1 = len;
                }
                else
                {
                    son[ptr0] = curMatch;
                    ptr0 = cyclicPos;
                    curMatch = son[ptr0];
                    len0 = len;
                }
            }

            MovePos();
            return offset;
        }

        /// <summary>
        /// </summary>
        /// <param name="num"></param>
        public void Skip(uint num)
        {
            do
            {
                uint lenLimit;
                if (pos + matchMaxLen <= streamPos)
                    lenLimit = matchMaxLen;
                else
                {
                    lenLimit = streamPos - pos;
                    if (lenLimit < kMinMatchCheck)
                    {
                        MovePos();
                        continue;
                    }
                }

                uint matchMinPos = pos > cyclicBufferSize ? pos - cyclicBufferSize : 0;
                uint cur = bufferOffset + pos;

                uint hashValue;

                if (hashArray)
                {
                    uint temp = CRC.Table[bufBase[cur]] ^ bufBase[cur + 1];
                    uint hash2Value = temp & (HASH_2_SIZE - 1);
                    hash[hash2Value] = pos;
                    temp ^= (uint)bufBase[cur + 2] << 8;
                    uint hash3Value = temp & (HASH_3_SIZE - 1);
                    hash[HASH_3_OFFSET + hash3Value] = pos;
                    hashValue = (temp ^ (CRC.Table[bufBase[cur + 3]] << 5)) & hashMask;
                }
                else
                    hashValue = bufBase[cur] ^ ((uint)bufBase[cur + 1] << 8);

                uint curMatch = hash[kFixHashSize + hashValue];
                hash[kFixHashSize + hashValue] = pos;

                uint ptr0 = (cyclicBufferPos << 1) + 1;
                uint ptr1 = cyclicBufferPos << 1;

                uint len0, len1;
                len0 = len1 = kNumHashDirectBytes;

                uint count = cutValue;
                while (true)
                {
                    if (curMatch <= matchMinPos || count-- == 0)
                    {
                        son[ptr0] = son[ptr1] = EMPTY_HASH_VALUE;
                        break;
                    }

                    uint delta = pos - curMatch;
                    uint cyclicPos = (delta <= cyclicBufferPos ? cyclicBufferPos - delta : cyclicBufferPos - delta + cyclicBufferSize) << 1;

                    uint pby1 = bufferOffset + curMatch;
                    uint len = Math.Min(len0, len1);
                    if (bufBase[pby1 + len] == bufBase[cur + len])
                    {
                        while (++len != lenLimit)
                        {
                            if (bufBase[pby1 + len] != bufBase[cur + len])
                                break;
                        }

                        if (len == lenLimit)
                        {
                            son[ptr1] = son[cyclicPos];
                            son[ptr0] = son[cyclicPos + 1];
                            break;
                        }
                    }

                    if (bufBase[pby1 + len] < bufBase[cur + len])
                    {
                        son[ptr1] = curMatch;
                        ptr1 = cyclicPos + 1;
                        curMatch = son[ptr1];
                        len1 = len;
                    }
                    else
                    {
                        son[ptr0] = curMatch;
                        ptr0 = cyclicPos;
                        curMatch = son[ptr0];
                        len0 = len;
                    }
                }

                MovePos();
            } while (--num != 0);
        }

        /// <summary>
        /// </summary>
        /// <param name="numHashBytes"></param>
        public void SetType(int numHashBytes)
        {
            hashArray = numHashBytes > 2;
            if (hashArray)
            {
                kNumHashDirectBytes = 0;
                kMinMatchCheck = 4;
                kFixHashSize = HASH_2_SIZE + HASH_3_SIZE;
            }
            else
            {
                kNumHashDirectBytes = 2;
                kMinMatchCheck = 2 + 1;
                kFixHashSize = 0;
            }
        }

        /// <summary>
        /// </summary>
        public new void MovePos()
        {
            if (++cyclicBufferPos >= cyclicBufferSize)
                cyclicBufferPos = 0;
            base.MovePos();
            if (pos == MAX_VAL_FOR_NORMALIZE)
                Normalize();
        }

        /// <summary>
        /// </summary>
        /// <param name="items"></param>
        /// <param name="numItems"></param>
        /// <param name="subValue"></param>
        private void NormalizeLinks(uint[] items, uint numItems, uint subValue)
        {
            for (uint i = 0; i < numItems; i++)
            {
                uint value = items[i];
                if (value <= subValue)
                    value = EMPTY_HASH_VALUE;
                else
                    value -= subValue;
                items[i] = value;
            }
        }

        /// <summary>
        /// </summary>
        private void Normalize()
        {
            uint subValue = pos - cyclicBufferSize;
            NormalizeLinks(son, cyclicBufferSize * 2, subValue);
            NormalizeLinks(hash, hashSizeSum, subValue);
            ReduceOffsets((int)subValue);
        }

        /// <summary>
        /// </summary>
        /// <param name="cutValue"></param>
        public void SetCutValue(uint cutValue)
        {
            this.cutValue = cutValue;
        }
    } // public class BinTree : InWindow, IMatchFinder
} // namespace TridentFramework.Compression.LZMA.LZ
