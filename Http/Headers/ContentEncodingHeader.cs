﻿/**
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
//
// Based on code from the C# WebServer project. (http://webserver.codeplex.com/)
// Copyright (c) 2012 Jonas Gauffin
// Licensed under the Apache 2.0 License (http://opensource.org/licenses/Apache-2.0)
//

using System;

namespace TridentFramework.RPC.Http.Headers
{
    /// <summary>
    /// Content-Encoding
    /// </summary>
    public class ContentEncodingHeader : IHeader
    {
        /// <summary>
        /// Header name.
        /// </summary>
        public const string NAME = "Content-Encoding";

        /*
        ** Properties
        */

        /// <summary>
        /// Gets content type.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Gets header name
        /// </summary>
        public string Name
        {
            get { return NAME; }
        }

        /// <summary>
        /// Gets Content-Encoding header as a string
        /// </summary>
        public string HeaderValue
        {
            get
            {
                return Value;
            }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentEncodingHeader"/> class.
        /// </summary>
        /// <param name="contentEncoding">Encoding of the content.</param>
        public ContentEncodingHeader(string contentEncoding)
        {
            Value = contentEncoding;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return HeaderValue;
        }
    } // public class ContentEncodingHeader : IHeader
} // namespace TridentFramework.RPC.Http.Headers
