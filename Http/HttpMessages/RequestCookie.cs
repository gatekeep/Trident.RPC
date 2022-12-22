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
using System.Web;

namespace TridentFramework.RPC.Http.HttpMessages
{
    /// <summary>
    /// cookie sent by the client/browser
    /// </summary>
    /// <seealso cref="ResponseCookie"/>
    public class RequestCookie
    {
        private readonly string name;
        private string value;

        /*
        ** Properties
        */

        /// <summary>
        /// Gets the cookie identifier.
        /// </summary>
        public string Name
        {
            get { return name; }
        }

        /// <summary>
        /// Gets value.
        /// </summary>
        /// <remarks>
        /// Set to <c>null</c> to remove cookie.
        /// </remarks>
        public string Value
        {
            get { return value; }
            set { this.value = value; }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="id">cookie identifier</param>
        /// <param name="content">cookie content</param>
        /// <exception cref="ArgumentNullException">id or content is null</exception>
        /// <exception cref="ArgumentException">id is empty</exception>
        public RequestCookie(string id, string content)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException("id");
            if (content == null) throw new ArgumentNullException("content");

            name = id;
            value = content;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Format("{0}={1}; ", HttpUtility.UrlEncode(name), HttpUtility.UrlEncode(value));
        }
    } // public class RequestCookie
} // namespace TridentFramework.RPC.Http.HttpMessages
