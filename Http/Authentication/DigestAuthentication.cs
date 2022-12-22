﻿/*
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
using System.Security.Cryptography;
using System.Text;
using System.Threading;

using TridentFramework.RPC.Http.Headers;
using TridentFramework.RPC.Http.Tools;

using TridentFramework.RPC.Utility;

namespace TridentFramework.RPC.Http.Authentication
{
    /// <summary>
    /// Implements HTTP Digest authentication. It's more secure than Basic auth since password is
    /// encrypted with a "key" from the server.
    /// </summary>
    /// <remarks>
    /// Keep in mind that the password is encrypted with MD5. Use a combination of SSL and digest auth to be secure.
    /// </remarks>
    public class DigestAuthentication : IAuthenticator
    {
        private readonly IUserProvider userProvider;
        private static readonly Dictionary<string, DateTime> nonces = new Dictionary<string, DateTime>();
        private static Timer timer;

        /// <summary>
        /// Used by test classes to be able to use hardcoded values
        /// </summary>
        public static bool DisableNonceCheck;

        /*
        ** Properties
        */

        /// <summary>
        /// Gets authentication scheme name
        /// </summary>
        public string Name
        {
            get { return "digest"; }
        }

        /// <summary>
        /// Gets authenticator scheme
        /// </summary>
        /// <value></value>
        /// <example>
        /// digest
        /// </example>
        public string Scheme
        {
            get { return "digest"; }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="DigestAuthentication"/> class.
        /// </summary>
        /// <param name="userProvider">Supplies users during authentication process.</param>
        public DigestAuthentication(IUserProvider userProvider)
        {
            this.userProvider = userProvider;
        }

        /// <summary>
        /// An authentication response have been received from the web browser.
        /// Check if it's correct
        /// </summary>
        /// <param name="header">Contents from the Authorization header</param>
        /// <param name="realm">Realm that should be authenticated</param>
        /// <param name="httpVerb">GET/POST/PUT/DELETE etc.</param>
        /// <returns>
        /// Authentication object that is stored for the request. A user class or something like that.
        /// </returns>
        /// <exception cref="ArgumentException">if authenticationHeader is invalid</exception>
        /// <exception cref="ArgumentNullException">If any of the parameters is empty or null.</exception>
        public IAuthenticationUser Authenticate(AuthorizationHeader header, string realm, string httpVerb)
        {
            if (header == null)
                throw new ArgumentNullException("header");

            lock (nonces)
            {
                if (timer == null)
                    timer = new Timer(ManageNonces, null, 15000, 15000);
            }

            if (!header.Scheme.Equals("digest", StringComparison.OrdinalIgnoreCase))
                return null;

            var parameters = HeaderParameterCollection.Parse(new StringReader(header.Data), ',');
            if (!IsValidNonce(parameters["nonce"]) && !DisableNonceCheck)
                return null;

            // request authentication information
            string username = parameters["username"];
            var user = userProvider.Lookup(username, realm);
            if (user == null)
                return null;

            // Encode authentication info
            string HA1 = string.IsNullOrEmpty(user.HA1) ? GetHA1(realm, username, user.Password) : user.HA1;

            // encode challenge info
            string A2 = String.Format("{0}:{1}", httpVerb, parameters["uri"]);
            string HA2 = GetMD5HashBinHex2(A2);
            string hashedDigest = Encrypt(HA1, HA2, parameters["qop"],
                                          parameters["nonce"], parameters["nc"], parameters["cnonce"]);

            //validate
            if (parameters["response"] == hashedDigest)
                return user;

            return null;
        }

        /// <summary>
        /// Encrypts parameters into a Digest string
        /// </summary>
        /// <param name="realm">Realm that the user want to log into.</param>
        /// <param name="userName">User logging in</param>
        /// <param name="password">Users password.</param>
        /// <param name="method">HTTP method.</param>
        /// <param name="uri">Uri/domain that generated the login prompt.</param>
        /// <param name="qop">Quality of Protection.</param>
        /// <param name="nonce">"Number used ONCE"</param>
        /// <param name="nc">Hexadecimal request counter.</param>
        /// <param name="cnonce">"Client Number used ONCE"</param>
        /// <returns>Digest encrypted string</returns>
        public static string Encrypt(string realm, string userName, string password, string method, string uri, string qop, string nonce, string nc, string cnonce)
        {
            string HA1 = GetHA1(realm, userName, password);
            string A2 = String.Format("{0}:{1}", method, uri);
            string HA2 = GetMD5HashBinHex2(A2);

            string unhashedDigest;
            if (qop != null)
                unhashedDigest = String.Format("{0}:{1}:{2}:{3}:{4}:{5}", HA1, nonce, nc, cnonce, qop, HA2);
            else
                unhashedDigest = String.Format("{0}:{1}:{2}", HA1, nonce, HA2);

            return GetMD5HashBinHex2(unhashedDigest);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="realm"></param>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static string GetHA1(string realm, string userName, string password)
        {
            return GetMD5HashBinHex2(String.Format("{0}:{1}:{2}", userName, realm, password));
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="ha1">Md5 hex encoded "userName:realm:password", without the quotes.</param>
        /// <param name="ha2">Md5 hex encoded "method:uri", without the quotes</param>
        /// <param name="qop">Quality of Protection</param>
        /// <param name="nonce">"Number used ONCE"</param>
        /// <param name="nc">Hexadecimal request counter.</param>
        /// <param name="cnonce">Client number used once</param>
        /// <returns></returns>
        protected virtual string Encrypt(string ha1, string ha2, string qop, string nonce, string nc, string cnonce)
        {
            string unhashedDigest;
            if (qop != null)
                unhashedDigest = String.Format("{0}:{1}:{2}:{3}:{4}:{5}", ha1, nonce, nc, cnonce, qop, ha2);
            else
                unhashedDigest = String.Format("{0}:{1}:{2}", ha1, nonce, ha2);

            return GetMD5HashBinHex2(unhashedDigest);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="state"></param>
        private void ManageNonces(object state)
        {
            try
            {
                lock (nonces)
                {
                    foreach (KeyValuePair<string, DateTime> pair in nonces)
                    {
                        if (pair.Value >= DateTime.Now)
                            continue;

                        nonces.Remove(pair.Key);
                        return;
                    }
                }
            }
            catch (Exception err)
            {
                RPCLogger.StackTrace("Failed to manage nonces.", err);
            }
        }

        /// <summary>
        /// Create a authentication challenge.
        /// </summary>
        /// <param name="realm">Realm that the user should authenticate in</param>
        /// <returns>A correct auth request.</returns>
        /// <exception cref="ArgumentNullException">If realm is empty or null.</exception>
        public IHeader CreateChallenge(string realm)
        {
            string nonce = GetCurrentNonce();

            StringBuilder challenge = new StringBuilder("Digest realm=\"");
            challenge.Append(realm);
            challenge.Append("\"");
            challenge.Append(", nonce=\"");
            challenge.Append(nonce);
            challenge.Append("\"");
            challenge.Append(", opaque=\"" + Guid.NewGuid().ToString().Replace("-", string.Empty) + "\"");
            challenge.Append(", stale=");

            /*if (options.Length > 0)
                challenge.Append((bool)options[0] ? "true" : "false");
            else*/
            challenge.Append("false");

            challenge.Append(", algorithm=MD5");
            challenge.Append(", qop=auth");

            return new StringHeader("WWW-Authenticate", challenge.ToString());
        }

        /// <summary>
        /// Gets the current nonce.
        /// </summary>
        /// <returns></returns>
        protected virtual string GetCurrentNonce()
        {
            string nonce = Guid.NewGuid().ToString().Replace("-", string.Empty);
            lock (nonces)
                nonces.Add(nonce, DateTime.Now.AddSeconds(30));

            return nonce;
        }

        /// <summary>
        /// Gets the Md5 hash bin hex2.
        /// </summary>
        /// <param name="toBeHashed">To be hashed.</param>
        /// <returns></returns>
        public static string GetMD5HashBinHex2(string toBeHashed)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] result = md5.ComputeHash(Encoding.Default.GetBytes(toBeHashed));

            StringBuilder sb = new StringBuilder();
            foreach (byte b in result)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        /// <summary>
        /// determines if the nonce is valid or has expired.
        /// </summary>
        /// <param name="nonce">nonce value (check wikipedia for info)</param>
        /// <returns><c>true</c> if the nonce has not expired.</returns>
        protected virtual bool IsValidNonce(string nonce)
        {
            lock (nonces)
            {
                if (nonces.ContainsKey(nonce))
                {
                    if (nonces[nonce] < DateTime.Now)
                    {
                        nonces.Remove(nonce);
                        return false;
                    }

                    return true;
                }
            }

            return false;
        }
    } // public class DigestAuthentication : IAuthenticator
} // namespace TridentFramework.RPC.Http.Authentication
