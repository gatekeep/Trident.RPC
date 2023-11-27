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

using TridentFramework.RPC.Utility;

namespace TridentFramework.RPC.Http.Routing
{
    /// <summary>
    /// Redirects from one URL to another.
    /// </summary>
    public class SimpleRouter : IRouter
    {
        private readonly string fromUrl;
        private readonly bool shouldRedirect;
        private readonly string toUrl;

        /*
        ** Properties
        */

        /// <summary>
        /// Gets string to match request URI with.
        /// </summary>
        /// <remarks>Is compared to request.Uri.AbsolutePath</remarks>
        public string FromUrl
        {
            get { return fromUrl; }
        }

        /// <summary>
        /// Gets whether the server should redirect the client instead of simply modifying the URI.
        /// </summary>
        /// <remarks>
        /// <c>false</c> means that the rule will replace
        /// the current request URI with the new one from this class.
        /// <c>true</c> means that a redirect response is sent to the client.
        /// </remarks>
        public bool ShouldRedirect
        {
            get { return shouldRedirect; }
        }

        /// <summary>
        /// Gets where to redirect.
        /// </summary>
        public string ToUrl
        {
            get { return toUrl; }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleRouter"/> class.
        /// </summary>
        /// <param name="fromUrl">Absolute path (no server name)</param>
        /// <param name="toUrl">Absolute path (no server name)</param>
        /// <example>
        /// server.Add(new RedirectRule("/", "/user/index"));
        /// </example>
        public SimpleRouter(string fromUrl, string toUrl)
        {
            this.fromUrl = fromUrl;
            this.toUrl = toUrl;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleRouter"/> class.
        /// </summary>
        /// <param name="fromUrl">Absolute path (no server name)</param>
        /// <param name="toUrl">Absolute path (no server name)</param>
        /// <param name="shouldRedirect"><c>true</c> if request should be redirected, <c>false</c> if the request URI should be replaced.</param>
        /// <example>
        /// server.Add(new RedirectRule("/", "/user/index"));
        /// </example>
        public SimpleRouter(string fromUrl, string toUrl, bool shouldRedirect)
        {
            this.fromUrl = fromUrl;
            this.toUrl = toUrl;
            this.shouldRedirect = shouldRedirect;
        }

        /// <summary>
        /// Process the incoming request.
        /// </summary>
        /// <param name="context">Request context.</param>
        /// <returns>Processing result.</returns>
        /// <exception cref="ArgumentNullException">If any parameter is <c>null</c>.</exception>
        public virtual ProcessingResult Process(RequestContext context)
        {
            IRequest request = context.Request;
            IResponse response = context.Response;

            if (request.Uri.AbsolutePath == FromUrl)
            {
                if (!ShouldRedirect)
                {
                    RPCLogger.Trace("Redirecting (internally) from " + FromUrl + " to " + ToUrl);
                    request.Uri = new Uri(request.Uri, ToUrl);
                    return ProcessingResult.Continue;
                }

                RPCLogger.Trace("Redirecting browser from " + FromUrl + " to " + ToUrl);
                response.Redirect(ToUrl);
                return ProcessingResult.SendResponse;
            }

            return ProcessingResult.Continue;
        }
    } // public class SimpleRouter : IRouter
} // namespace TridentFramework.RPC.Http.Routing
