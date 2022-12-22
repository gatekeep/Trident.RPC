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

namespace TridentFramework.Compression.LZMA
{
    /// <summary>
    /// </summary>
    public interface ICodeProgress
    {
        /*
        ** Methods
        */

        /// <summary>
        /// Callback progress.
        /// </summary>
        /// <param name="inSize">Input size. -1 if unknown</param>
        /// <param name="outSize">Output size. -1 if unknown</param>
        void SetProgress(long inSize, long outSize);
    } // public interface ICodeProgress

    /// <summary>
    /// </summary>
    public interface ICoder
    {
        /*
        ** Methods
        */

        /// <summary>
        /// Codes streams.
        /// </summary>
        /// <param name="inStream">Input Stream</param>
        /// <param name="outStream">Output Stream</param>
        /// <param name="inSize">Input Size. -1 if unknown</param>
        /// <param name="outSize">Output Size. -1 if unknown</param>
        /// <param name="progress">Callback progress reference</param>
        /// <exception cref="DataErrorException">If input stream is not valid</exception>
        void Code(Stream inStream, Stream outStream, long inSize, long outSize, ICodeProgress progress);
    } // public interface ICoder

    /// <summary>
    /// Provides the fields that represent properties idenitifiers for compressing.
    /// </summary>
    public enum CoderPropID
    {
        /// <summary>
        /// Specifies default property.
        /// </summary>
        DefaultProp = 0,

        /// <summary>
        /// Specifies size of dictionary.
        /// </summary>
        DictionarySize,

        /// <summary>
        /// Specifies size of memory for PPM*.
        /// </summary>
        UsedMemorySize,

        /// <summary>
        /// Specifies order for PPM methods.
        /// </summary>
        Order,

        /// <summary>
        /// Specifies Block Size.
        /// </summary>
        BlockSize,

        /// <summary>
        /// Specifies number of postion state bits for LZMA (0 &lt;= x &lt;= 4).
        /// </summary>
        PosStateBits,

        /// <summary>
        /// Specifies number of literal context bits for LZMA (0 &lt;= x &lt;= 8).
        /// </summary>
        LitContextBits,

        /// <summary>
        /// Specifies number of literal position bits for LZMA (0 &lt;= x &lt;= 4).
        /// </summary>
        LitPosBits,

        /// <summary>
        /// Specifies number of fast bytes for LZ*.
        /// </summary>
        NumFastBytes,

        /// <summary>
        /// Specifies match finder. LZMA: "BT2", "BT4" or "BT4B".
        /// </summary>
        MatchFinder,

        /// <summary>
        /// Specifies the number of match finder cyckes.
        /// </summary>
        MatchFinderCycles,

        /// <summary>
        /// Specifies number of passes.
        /// </summary>
        NumPasses,

        /// <summary>
        /// Specifies number of algorithm.
        /// </summary>
        Algorithm,

        /// <summary>
        /// Specifies the number of threads.
        /// </summary>
        NumThreads,

        /// <summary>
        /// Specifies mode with end marker.
        /// </summary>
        EndMarker
    }

    /// <summary>
    /// </summary>
    public interface ISetCoderProperties
    {
        /*
        ** Methods
        */

        /// <summary>
        /// </summary>
        /// <param name="propIDs"></param>
        /// <param name="properties"></param>
        void SetCoderProperties(CoderPropID[] propIDs, object[] properties);
    } // public interface ISetCoderProperties

    /// <summary>
    /// </summary>
    public interface IWriteCoderProperties
    {
        /*
        ** Methods
        */

        /// <summary>
        /// </summary>
        /// <param name="outStream"></param>
        void WriteCoderProperties(Stream outStream);
    } // public interface IWriteCoderProperties

    /// <summary>
    /// </summary>
    public interface ISetDecoderProperties
    {
        /*
        ** Methods
        */

        /// <summary>
        /// </summary>
        /// <param name="properties"></param>
        void SetDecoderProperties(byte[] properties);
    } // public interface ISetDecoderProperties
} // namespace TridentFramework.Compression.LZMA
