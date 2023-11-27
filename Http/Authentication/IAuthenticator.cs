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

using TridentFramework.RPC.Http.Headers;

namespace TridentFramework.RPC.Http.Authentication
{
    /// <summary>
    /// Authenticates requests
    /// </summary>
    public interface IAuthenticator
    {
        /*
        ** Properties
        */

        /// <summary>
        /// Gets authenticator scheme
        /// </summary>
        /// <example>
        /// digest
        /// </example>
        string Scheme { get; }

        /*
        ** Methods
        */

        /// <summary>
        /// Authenticate request
        /// </summary>
        /// <param name="header">Authorization header send by web client</param>
        /// <param name="realm">Realm to authenticate in, typically a domain name.</param>
        /// <param name="httpVerb">HTTP Verb used in the request.</param>
        /// <returns><c>User</c> if authentication was successful; otherwise <c>null</c>.</returns>
        IAuthenticationUser Authenticate(AuthorizationHeader header, string realm, string httpVerb);

        /// <summary>
        /// Create a authentication challenge.
        /// </summary>
        /// <param name="realm">Realm that the user should authenticate in</param>
        /// <returns>A WWW-Authenticate header.</returns>
        /// <exception cref="ArgumentNullException">If realm is empty or <c>null</c>.</exception>
        IHeader CreateChallenge(string realm);
    } // public interface IAuthenticator
} // namespace TridentFramework.RPC.Http.Authentication
