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

namespace TridentFramework.Compression.LZMA
{
    /// <summary>
    /// The exception that is thrown when an error in input stream occurs during decoding.
    /// </summary>
    public class DataErrorException : ApplicationException
    {
        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="DataErrorException" /> class.
        /// </summary>
        public DataErrorException() : base("Data Error")
        {
            /* stub */
        }
    } // public class DataErrorException : ApplicationException
} // namespace TridentFramework.Compression.LZMA
