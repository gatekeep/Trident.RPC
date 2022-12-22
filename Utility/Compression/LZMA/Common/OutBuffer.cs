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

namespace TridentFramework.Compression.LZMA.Common
{
    /// <summary>
    /// </summary>
    public class OutBuffer
    {
        private readonly byte[] buffer;
        private readonly uint bufferSize;
        private uint bufPosition;
        private ulong processedSize;
        private Stream stream;

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="OutBuffer" /> class.
        /// </summary>
        /// <param name="bufferSize"></param>
        public OutBuffer(uint bufferSize)
        {
            buffer = new byte[bufferSize];
            this.bufferSize = bufferSize;
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
        public void FlushStream()
        {
            stream.Flush();
        }

        /// <summary>
        /// </summary>
        public void CloseStream()
        {
            stream.Close();
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
            processedSize = 0;
            bufPosition = 0;
        }

        /// <summary>
        /// </summary>
        /// <param name="b"></param>
        public void WriteByte(byte b)
        {
            buffer[bufPosition++] = b;
            if (bufPosition >= bufferSize)
                FlushData();
        }

        /// <summary>
        /// </summary>
        public void FlushData()
        {
            if (bufPosition == 0)
                return;
            stream.Write(buffer, 0, (int)bufPosition);
            bufPosition = 0;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public ulong GetProcessedSize()
        {
            return processedSize + bufPosition;
        }
    } // public class OutBuffer
} // namespace TridentFramework.Compression.LZMA.Common
