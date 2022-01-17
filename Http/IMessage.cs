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
//
// Based on code from the C# WebServer project. (http://webserver.codeplex.com/)
// Copyright (c) 2012 Jonas Gauffin
// Licensed under the Apache 2.0 License (http://opensource.org/licenses/Apache-2.0)
//

using System.Collections.Generic;
using System.IO;
using System.Text;

using TridentFramework.RPC.Http.Headers;

namespace TridentFramework.RPC.Http
{
    /// <summary>
    /// Base interface for request and response.
    /// </summary>
    public interface IMessage : IEnumerable<IHeader>
    {
        /*
        ** Properties
        */

        /// <summary>
        /// Gets body stream.
        /// </summary>
        Stream Body { get; }

        /// <summary>
        /// Size of the body. MUST be specified before sending the header,
        /// unless property Chunked is set to <c>true</c>.
        /// </summary>
        NumericHeader ContentLength { get; }

        /// <summary>
        /// Kind of content in the body
        /// </summary>
        /// <remarks>Default is <c>text/html</c></remarks>
        ContentTypeHeader ContentType { get; }

        /// <summary>
        /// Gets or sets encoding
        /// </summary>
        Encoding Encoding { get; set; }

        /// <summary>
        /// Gets headers.
        /// </summary>
        IHeaderCollection Headers { get; }

        /*
        ** Methods
        */

        /// <summary>
        /// Add a new header.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        void Add(string name, IHeader value);

        /// <summary>
        /// Add a new header.
        /// </summary>
        /// <param name="header">Header to add.</param>
        void Add(IHeader header);
    } // public interface IMessage : IEnumerable<IHeader>
} // namespace TridentFramework.RPC.Http
