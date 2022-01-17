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
using System.Web;

namespace TridentFramework.RPC.Http.HttpMessages
{
    /// <summary>
    /// Cookie being sent back to the browser.
    /// </summary>
    /// <seealso cref="ResponseCookie"/>
    public class ResponseCookie : RequestCookie
    {
        private string domain = string.Empty;
        private DateTime expires;
        private string path = string.Empty;

        /*
        ** Properties
        */

        /// <summary>
        /// Gets when the cookie expires.
        /// </summary>
        /// <remarks><see cref="DateTime.MinValue"/> means that the cookie expires when the session do so.</remarks>
        public DateTime Expires
        {
            get { return expires; }
            set
            {
                expires = value;
            }
        }

        /// <summary>
        /// Gets path that the cookie is valid under.
        /// </summary>
        public string Path
        {
            get { return path; }
            set { path = value; }
        }

        /// <summary>
        /// Gets domain that the cookie is valid under.
        /// </summary>
        public string Domain
        {
            get { return domain; }
            set { domain = value; }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="id">cookie identifier</param>
        /// <param name="content">cookie content</param>
        /// <param name="expiresAt">cookie expiration date. Use <see cref="DateTime.MinValue"/> for session cookie.</param>
        /// <exception cref="ArgumentNullException">id or content is <c>null</c></exception>
        /// <exception cref="ArgumentException">id is empty</exception>
        public ResponseCookie(string id, string content, DateTime expiresAt)
            : base(id, content)
        {
            expires = expiresAt;
        }

        /// <summary>
        /// Create a new cookie
        /// </summary>
        /// <param name="name">name identifying the cookie</param>
        /// <param name="value">cookie value</param>
        /// <param name="expires">when the cookie expires. Setting <see cref="DateTime.MinValue"/> will delete the cookie when the session is closed.</param>
        /// <param name="path">Path to where the cookie is valid</param>
        /// <param name="domain">Domain that the cookie is valid for.</param>
        public ResponseCookie(string name, string value, DateTime expires, string path, string domain)
            : this(name, value, expires)
        {
            this.domain = domain;
            this.path = path;
        }

        /// <summary>
        /// Create a new cookie
        /// </summary>
        /// <param name="cookie">Name and value will be used</param>
        /// <param name="expires">when the cookie expires.</param>
        public ResponseCookie(RequestCookie cookie, DateTime expires)
            : this(cookie.Name, cookie.Value, expires)
        {
            /* stub */
        }

        /// <inheritdoc />
        public override string ToString()
        {
            string temp = string.Format("{0}={1}; ", HttpUtility.UrlEncode(Name), HttpUtility.UrlEncode(Value));
            if (expires != DateTime.MinValue)
            {
                // Fixed by Albert, Team MediaPortal
                temp += string.Format("expires={0};", expires.ToUniversalTime().ToString("r"));
            }
            temp += string.Format("path={0}; ", path);
            temp += string.Format("domain={0}; ", domain);

            return temp;
        }
    } // public class ResponseCookie : RequestCookie
} // namespace TridentFramework.RPC.Http.HttpMessages
