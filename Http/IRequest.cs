/*
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

using TridentFramework.RPC.Http.Headers;
using TridentFramework.RPC.Http.HttpMessages;

namespace TridentFramework.RPC.Http
{
    /// <summary>
    /// Request sent to a HTTP server.
    /// </summary>
    /// <seealso cref="Request"/>
    public interface IRequest : IMessage
    {
        /*
        ** Properties
        */

        /// <summary>
        /// Gets or sets connection header.
        /// </summary>
        ConnectionHeader Connection { get; }

        /// <summary>
        /// Gets cookies.
        /// </summary>
        RequestCookieCollection Cookies { get; }

        /// <summary>
        /// Gets all uploaded files.
        /// </summary>
        HttpFileCollection Files { get; }

        /// <summary>
        /// Gets form parameters.
        /// </summary>
        IParameterCollection Form { get; }

        /// <summary>
        /// Gets or sets HTTP version.
        /// </summary>
        string HttpVersion { get; set; }

        /// <summary>
        /// Gets if request is an Ajax request.
        /// </summary>
        bool IsAjax { get; }

        /// <summary>
        /// Gets or sets HTTP method.
        /// </summary>
        string Method { get; set; }

        /// <summary>
        /// Gets query string and form parameters
        /// </summary>
        IParameterCollection Parameters { get; }

        /// <summary>
        /// Gets query string.
        /// </summary>
        IParameterCollection QueryString { get; }

        /// <summary>
        /// Gets requested URI.
        /// </summary>
        Uri Uri { get; set; }

        /// <summary>
        /// Gets the User-Agent header.
        /// </summary>
        string UserAgent { get; }

        /*
        ** Methods
        */

        /// <summary>
        /// Get a header
        /// </summary>
        /// <typeparam name="T">Type that it should be cast to</typeparam>
        /// <param name="headerName">Name of header</param>
        /// <returns>Header if found and casted properly; otherwise <c>null</c>.</returns>
        T Get<T>(string headerName) where T : class, IHeader;
    } // public interface IRequest : IMessage
} // namespace TridentFramework.RPC.Http
