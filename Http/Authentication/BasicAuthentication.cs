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
using System.Text;

using TridentFramework.RPC.Http.Headers;

namespace TridentFramework.RPC.Http.Authentication
{
    /// <summary>
    /// Implements basic authentication scheme.
    /// </summary>
    public class BasicAuthentication : IAuthenticator
    {
        private readonly IUserProvider userProvider;

        /*
        ** Properties
        */

        /// <summary>
        /// Gets authenticator scheme
        /// </summary>
        /// <value></value>
        /// <example>
        /// digest
        /// </example>
        public string Scheme
        {
            get { return "basic"; }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="BasicAuthentication"/> class.
        /// </summary>
        /// <param name="userProvider"></param>
        public BasicAuthentication(IUserProvider userProvider)
        {
            this.userProvider = userProvider;
        }

        /// <summary>
        /// Create a response that can be sent in the WWW-Authenticate header.
        /// </summary>
        /// <param name="realm">Realm that the user should authenticate in</param>
        /// <param name="options">Not used by basic authentication</param>
        /// <returns>A WWW-Authenticate header.</returns>
        /// <exception cref="ArgumentNullException">Argument is <c>null</c>.</exception>
        public IHeader CreateChallenge(string realm)
        {
            if (string.IsNullOrEmpty(realm))
                throw new ArgumentNullException("realm");

            return new StringHeader("WWW-Authenticate", "Basic realm=\"" + realm + "\"");
        }

        /// <summary>
        /// An authentication response have been received from the web browser.
        /// Check if it's correct
        /// </summary>
        /// <param name="header">Authorization header</param>
        /// <param name="realm">Realm that should be authenticated</param>
        /// <param name="httpVerb">GET/POST/PUT/DELETE etc.</param>
        /// <returns>Authentication object that is stored for the request. A user class or something like that.</returns>
        /// <exception cref="ArgumentException">if authenticationHeader is invalid</exception>
        /// <exception cref="ArgumentNullException">If any of the paramters is empty or null.</exception>
        public IAuthenticationUser Authenticate(AuthorizationHeader header, string realm, string httpVerb)
        {
            if (header == null)
                throw new ArgumentNullException("realm");
            if (string.IsNullOrEmpty(realm))
                throw new ArgumentNullException("realm");
            if (string.IsNullOrEmpty(httpVerb))
                throw new ArgumentNullException("httpVerb");

            /*
             * To receive authorization, the client sends the userid and password,
                separated by a single colon (":") character, within a base64 [7]
                encoded string in the credentials.*/
            string decoded = Encoding.UTF8.GetString(Convert.FromBase64String(header.Data));
            int pos = decoded.IndexOf(':');
            if (pos == -1)
                throw new BadRequestException("Invalid basic authentication header, failed to find colon.");

            string password = decoded.Substring(pos + 1, decoded.Length - pos - 1);
            string userName = decoded.Substring(0, pos);

            var user = userProvider.Lookup(userName, realm);
            if (user == null)
                return null;

            if (user.Password == null)
            {
                var ha1 = DigestAuthentication.GetHA1(realm, userName, password);
                if (ha1 != user.HA1)
                    return null;
            }
            else
            {
                if (password != user.Password)
                    return null;
            }

            return user;
        }
    } // public class BasicAuthentication : IAuthenticator
} // namespace TridentFramework.RPC.Http.Authentication
