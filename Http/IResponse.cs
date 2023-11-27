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
//
// Based on code from the C# WebServer project. (http://webserver.codeplex.com/)
// Copyright (c) 2012 Jonas Gauffin
// Licensed under the Apache 2.0 License (http://opensource.org/licenses/Apache-2.0)
//

using System.Net;

using TridentFramework.RPC.Http.Headers;
using TridentFramework.RPC.Http.HttpMessages;

namespace TridentFramework.RPC.Http
{
    /// <summary>
    /// Response to a request.
    /// </summary>
    public interface IResponse : IMessage
    {
        /*
        ** Properties
        */

        /// <summary>
        /// Gets connection type.
        /// </summary>
        ConnectionHeader Connection { get; }

        /// <summary>
        /// Gets cookies.
        /// </summary>
        ResponseCookieCollection Cookies { get; }

        /// <summary>
        /// Gets HTTP version.
        /// </summary>
        /// <remarks>
        /// Default is HTTP/1.1
        /// </remarks>
        string HttpVersion { get; }

        /// <summary>
        /// Information about why a specific status code was used.
        /// </summary>
        string Reason { get; set; }

        /// <summary>
        /// Status code that is sent to the client.
        /// </summary>
        /// <remarks>Default is <see cref="HttpStatusCode.OK"/></remarks>
        HttpStatusCode Status { get; set; }

        ///<summary>
        /// Gets or sets content type
        ///</summary>
        new ContentTypeHeader ContentType { get; set; }

        ///<summary>
        /// Gets or sets content encoding
        ///</summary>
        ContentEncodingHeader ContentEncoding { get; set; }

        /*
        ** Methods
        */

        /// <summary>
        /// Redirect user.
        /// </summary>
        /// <param name="uri">Where to redirect to.</param>
        /// <remarks>
        /// Any modifications after a redirect will be ignored.
        /// </remarks>
        void Redirect(string uri);
    } // public interface IResponse : IMessage
} // namespace TridentFramework.RPC.Http
