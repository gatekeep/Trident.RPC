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

using System.IO;
using System.Net;
using System.Net.Security;

using TridentFramework.RPC.Http.HttpMessages;

namespace TridentFramework.RPC.Http
{
    /// <summary>
    /// Context that received a HTTP request.
    /// </summary>
    public interface IHttpContext
    {
        /*
        ** Properties
        */

        /// <summary>
        /// Gets if current context is using a secure connection.
        /// </summary>
        bool IsSecure { get; }

        /// <summary>
        /// Gets remote end point
        /// </summary>
        IPEndPoint RemoteEndPoint { get; }

        /// <summary>
        /// Gets stream used to send/receive data to/from remote end point.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The stream can be any type of stream, do not assume that it's a network
        /// stream. For instance, it can be a <see cref="SslStream"/> or a ZipStream.
        /// </para>
        /// </remarks>
        Stream Stream { get; }

        /// <summary>
        /// Gets the currently handled request
        /// </summary>
        /// <value>The request.</value>
        IRequest Request { get; }

        /// <summary>
        /// Gets the response that is going to be sent back
        /// </summary>
        /// <value>The response.</value>
        IResponse Response { get; }

        /*
        ** Methods
        */

        /// <summary>
        /// Disconnect context.
        /// </summary>
        void Disconnect();
    } // public interface IHttpContext
} // namespace TridentFramework.RPC.Http
