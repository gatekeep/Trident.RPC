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
using System.Net;

namespace TridentFramework.RPC.Http.HttpMessages.Parser
{
    /// <summary>
    /// A request have been received.
    /// </summary>
    public class RequestEventArgs : EventArgs
    {
        /*
        ** Properties
        */

        /// <summary>
        /// End point that the message was received from.
        /// </summary>
        public EndPoint RemoteEndPoint { get; private set; }

        /// <summary>
        /// Received request.
        /// </summary>
        public IRequest Request { get; private set; }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestEventArgs"/> class.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="endPoint">End point that the request was received from.</param>
        public RequestEventArgs(IRequest request, EndPoint endPoint)
        {
            Request = request;
            RemoteEndPoint = endPoint;
        }
    } // public class RequestEventArgs : EventArgs
} // namespace TridentFramework.RPC.Http.HttpMessages.Parser
