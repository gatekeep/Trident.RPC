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
    ///   The Cache-Control general-header field is used to specify directives that
    ///   MUST be obeyed by all caching mechanisms along the request/response
    ///   chain. .
    /// </summary>
    /// <remarks>
    /// <para>
    /// The directives specify behavior intended to prevent caches from adversely
    /// interfering with the request or response. These directives typically
    /// override the default caching algorithms. Cache directives are
    /// unidirectional in that the presence of a directive in a request does not
    /// imply that the same directive is to be given in the response.
    ///</para><para>Note that HTTP/1.0 caches might not implement Cache-Control and
    ///might only implement Pragma: no-cache (see section 14.32 in RFC2616).
    ///</para><para>Cache directives MUST be passed through by a proxy or gateway
    ///application, regardless of their significance to that application, since the
    ///directives might be applicable to all recipients along the request/response
    ///chain. It is not possible to specify a cache- directive for a specific cache
    /// </para>
    /// <para>
    ///   When a directive appears without any 1#field-name parameter, the
    ///   directive applies to the entire request or response. When such a
    ///   directive appears with a 1#field-name parameter, it applies only to
    ///   the named field or fields, and not to the rest of the request or
    ///   response. This mechanism supports extensibility; implementations of
    ///   future versions of the HTTP protocol might apply these directives to
    ///   header fields not defined in HTTP/1.1.
    /// </para>
    /// <para>
    ///   The cache-control directives can be broken down into these general
    ///   categories:
    /// <list type="bullet">
    /// <item>
    ///      Restrictions on what are cacheable; these may only be imposed by
    ///      the origin server.
    ///</item><item>
    ///      Restrictions on what may be stored by a cache; these may be
    ///      imposed by either the origin server or the user agent.
    ///</item><item>
    ///      Modifications of the basic expiration mechanism; these may be
    ///      imposed by either the origin server or the user agent.
    ///</item><item>
    ///      Controls over cache revalidation and reload; these may only be
    ///      imposed by a user agent.
    ///</item><item>
    ///      Control over transformation of entities.
    ///</item><item>
    ///      Extensions to the caching system.
    /// </item>
    /// </list>
    ///	</para>
    /// </remarks>
    public class CacheControlHeader : IHeader
    {
        /// <summary>
        /// Header name
        /// </summary>
        public const string NAME = "Cache-Control";

        /*
        ** Properties
        */

        /// <summary>
        /// Gets header name
        /// </summary>
        public string Name
        {
            get { return NAME; }
        }

        /// <summary>
        /// Gets Cache-Control header as a string
        /// </summary>
        public string HeaderValue
        {
            get { return "NotImplementedYet"; }
        }
    } // public class CacheControlHeader : IHeader
} // namespace TridentFramework.RPC.Http.Headers
