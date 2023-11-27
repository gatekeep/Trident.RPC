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

using System;

namespace TridentFramework.RPC.Http.Headers
{
    /// <summary>
    /// Authorization response
    /// </summary>
    /// <remarks>
    /// <para>
    /// A user agent that wishes to authenticate itself with a server--
    /// usually, but not necessarily, after receiving a 401 response--does
    /// so by including an Authorization request-header field with the
    /// request.  The Authorization field value consists of credentials
    /// containing the authentication information of the user agent for
    /// the realm of the resource being requested.
    /// </para>
    /// <example>
    ///     Authorization  = "Authorization" ":" credentials
    /// </example>
    /// <para>
    /// HTTP access authentication is described in "HTTP Authentication:
    /// Basic and Digest Access Authentication" [43]. If a request is
    /// authenticated and a realm specified, the same credentials SHOULD
    /// be valid for all other requests within this realm (assuming that
    /// the authentication scheme itself does not require otherwise, such
    /// as credentials that vary according to a challenge value or using
    /// synchronized clocks).
    /// When a shared cache (see section 13.7) receives a request
    /// containing an Authorization field, it MUST NOT return the
    /// corresponding response as a reply to any other request, unless one
    /// of the following specific exceptions holds:
    /// </para>
    /// <list type="number">
    /// <item>
    ///  If the response includes the "s-maxage" cache-control
    ///    directive, the cache MAY use that response in replying to a
    ///    subsequent request. But (if the specified maximum age has
    ///    passed) a proxy cache MUST first revalidate it with the origin
    ///    server, using the request-headers from the new request to allow
    ///    the origin server to authenticate the new request. (This is the
    ///    defined behavior for s-maxage.) If the response includes "s-
    ///    maxage=0", the proxy MUST always revalidate it before re-using
    ///    it.
    /// </item><item>
    ///  If the response includes the "must-revalidate" cache-control
    ///    directive, the cache MAY use that response in replying to a
    ///    subsequent request. But if the response is stale, all caches
    ///    MUST first revalidate it with the origin server, using the
    ///    request-headers from the new request to allow the origin server
    ///    to authenticate the new request.
    /// </item><item>
    ///  If the response includes the "public" cache-control directive,
    ///    it MAY be returned in reply to any subsequent request.
    /// </item>
    /// </list>
    /// </remarks>
    public class AuthorizationHeader : IHeader
    {
        /// <summary>
        /// Name constant
        /// </summary>
        public const string NAME = "Authorization";

        /*
        ** Properties
        */

        /// <summary>
        /// Gets or sets authentication data.
        /// </summary>
        public string Data { get; set; }

        /// <summary>
        /// Gets or sets authentication protocol.
        /// </summary>
        public string Scheme { get; set; }

        /// <summary>
        /// Gets name of header.
        /// </summary>
        public string Name
        {
            get { return NAME; }
        }

        /// <summary>
        /// Gets Authorization header as a string
        /// </summary>
        public string HeaderValue
        {
            get { throw new NotImplementedException(); }
        }
    } // public class AuthorizationHeader : IHeader
} // namespace TridentFramework.RPC.Http.Headers
