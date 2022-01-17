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

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using TridentFramework.RPC.Http.Headers;

namespace TridentFramework.RPC.Http.BodyDecoders
{
    /// <summary>
    /// Decodes body stream.
    /// </summary>
    public interface IBodyDecoder
    {
        /*
        ** Properties
        */

        /// <summary>
        /// All content types that the decoder can parse.
        /// </summary>
        /// <returns>A collection of all content types that the decoder can handle.</returns>
        IEnumerable<string> ContentTypes { get; }

        /*
        ** Methods
        */

        /// <summary>
        /// Decode body stream
        /// </summary>
        /// <param name="stream">Stream containing the content</param>
        /// <param name="contentType">Content type header</param>
        /// <param name="encoding">Stream encoding</param>
        /// <returns>Decoded data.</returns>
        /// <exception cref="FormatException">Body format is invalid for the specified content type.</exception>
        /// <exception cref="InternalServerException">Something unexpected failed.</exception>
        DecodedData Decode(Stream stream, ContentTypeHeader contentType, Encoding encoding);
    } // public interface IBodyDecoder
} // namespace TridentFramework.RPC.Http.BodyDecoders
