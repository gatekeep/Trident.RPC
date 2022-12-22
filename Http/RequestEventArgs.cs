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

namespace TridentFramework.RPC.Http
{
    /// <summary>
    /// A request have been received.
    /// </summary>
    /// <remarks>
    /// </remarks>
    public class RequestEventArgs : EventArgs
    {
        /*
        ** Properties
        */

        /// <summary>
        /// Gets context that received the request.
        /// </summary>
        /// <remarks>
        /// Do not forget to set <see cref="IsHandled"/> to <c>true</c> if you are sending
        /// back a response manually through <see cref="IHttpContext.Stream"/>.
        /// </remarks>
        public IHttpContext Context { get; private set; }

        /// <summary>
        /// Gets or sets if the request have been handled.
        /// </summary>
        /// <remarks>
        /// The library will not attempt to send the response object
        /// back to the client if this property is set to <c>true</c>.
        /// </remarks>
        public bool IsHandled { get; set; }

        /// <summary>
        /// Gets request object.
        /// </summary>
        public IRequest Request { get; private set; }

        /// <summary>
        /// Gets response object.
        /// </summary>
        public IResponse Response { get; private set; }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestEventArgs"/> class.
        /// </summary>
        /// <param name="context">context that received the request.</param>
        /// <param name="request">Received request.</param>
        /// <param name="response">Response to send.</param>
        public RequestEventArgs(IHttpContext context, IRequest request, IResponse response)
        {
            Context = context;
            Response = response;
            Request = request;
        }
    } // public class RequestEventArgs : EventArgs
} // namespace TridentFramework.RPC.Http
