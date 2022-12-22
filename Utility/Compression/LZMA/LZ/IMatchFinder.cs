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

using System.IO;

namespace TridentFramework.Compression.LZMA.LZ
{
    /// <summary>
    /// </summary>
    public interface IInWindowStream
    {
        /*
        ** Methods
        */

        /// <summary>
        /// </summary>
        /// <param name="inStream"></param>
        void SetStream(Stream inStream);

        /// <summary>
        /// </summary>
        void Init();

        /// <summary>
        /// </summary>
        void ReleaseStream();

        /// <summary>
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        byte GetIndexByte(int index);

        /// <summary>
        /// </summary>
        /// <param name="index"></param>
        /// <param name="distance"></param>
        /// <param name="limit"></param>
        /// <returns></returns>
        uint GetMatchLen(int index, uint distance, uint limit);

        /// <summary>
        /// </summary>
        /// <returns></returns>
        uint GetNumAvailableBytes();
    } // public interface IInWindowStream

    /// <summary>
    /// </summary>
    public interface IMatchFinder : IInWindowStream
    {
        /*
        ** Methods
        */

        /// <summary>
        /// </summary>
        /// <param name="historySize"></param>
        /// <param name="keepAddBufferBefore"></param>
        /// <param name="matchMaxLen"></param>
        /// <param name="keepAddBufferAfter"></param>
        void Create(uint historySize, uint keepAddBufferBefore, uint matchMaxLen, uint keepAddBufferAfter);

        /// <summary>
        /// </summary>
        /// <param name="distances"></param>
        /// <returns></returns>
        uint GetMatches(uint[] distances);

        /// <summary>
        /// </summary>
        /// <param name="num"></param>
        void Skip(uint num);
    } // public interface IMatchFinder : IInWindowStream
} // namespace libtrace.LZ
