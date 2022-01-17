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
using System.Collections;
using System.Collections.Generic;

namespace TridentFramework.RPC.Http.HttpMessages
{
    /// <summary>
    /// Cookies that should be set.
    /// </summary>
    public sealed class ResponseCookieCollection : IEnumerable<ResponseCookie>
    {
        private readonly IDictionary<string, ResponseCookie> items = new Dictionary<string, ResponseCookie>();

        /*
        ** Properties
        */

        /// <summary>
        /// Gets the count of cookies in the collection.
        /// </summary>
        public int Count
        {
            get { return items.Count; }
        }

        /// <summary>
        /// Gets the cookie of a given identifier.
        /// </summary>
        /// <value>Cookie if found; otherwise <c>null</c>.</value>
        public ResponseCookie this[string id]
        {
            get { return items.ContainsKey(id) ? items[id] : null; }
            set
            {
                if (items.ContainsKey(id))
                    items[id] = value;
                else
                    Add(value);
            }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Adds a cookie in the collection.
        /// </summary>
        /// <param name="cookie">cookie to add</param>
        /// <exception cref="ArgumentNullException">cookie is <c>null</c></exception>
        /// <exception cref="ArgumentException">Name and Content must be specified.</exception>
        public void Add(ResponseCookie cookie)
        {
            // Verifies the parameter
            if (cookie == null)
                throw new ArgumentNullException("cookie");
            if (cookie.Name == null || cookie.Name.Trim() == string.Empty)
                throw new ArgumentException("Name must be specified.");
            if (cookie.Value == null)
                throw new ArgumentException("Content must be specified.");

            if (items.ContainsKey(cookie.Name))
                items[cookie.Name] = cookie;
            else items.Add(cookie.Name, cookie);
        }

        /// <summary>
        /// Copy a request cookie
        /// </summary>
        /// <param name="cookie"></param>
        /// <param name="expires">When the cookie should expire</param>
        public void Add(RequestCookie cookie, DateTime expires)
        {
            Add(new ResponseCookie(cookie, expires));
        }

        /// <summary>
        /// Remove all cookies
        /// </summary>
        public void Clear()
        {
            items.Clear();
        }

        /// <summary>
        /// Gets a collection enumerator on the cookie list.
        /// </summary>
        /// <returns>collection enumerator</returns>
        public IEnumerator GetEnumerator()
        {
            return items.Values.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"></see> that can be used to iterate through the collection.
        /// </returns>
        IEnumerator<ResponseCookie> IEnumerable<ResponseCookie>.GetEnumerator()
        {
            return items.Values.GetEnumerator();
        }
    } // public sealed class ResponseCookieCollection : IEnumerable<ResponseCookie>
} // namespace TridentFramework.RPC.Http.HttpMessages
