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
using System.Collections;
using System.Collections.Generic;

namespace TridentFramework.RPC.Http.HttpMessages
{
    /// <summary>
    /// A list of request cookies.
    /// </summary>
    public sealed class RequestCookieCollection : IEnumerable<RequestCookie>
    {
        private readonly IDictionary<string, RequestCookie> items = new Dictionary<string, RequestCookie>();

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
        /// Gets the cookie of a given identifier (<c>null</c> if not existing).
        /// </summary>
        public RequestCookie this[string id]
        {
            get { return items.ContainsKey(id) ? items[id] : null; }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Let's copy all the cookies.
        /// </summary>
        /// <param name="cookies">value from cookie header.</param>
        public RequestCookieCollection(string cookies)
        {
            if (string.IsNullOrEmpty(cookies))
                return;

            string name = string.Empty;
            int state = 0;
            int start = -1;
            for (int i = 0; i < cookies.Length; ++i)
            {
                char ch = cookies[i];

                // searching for start of cookie name
                switch (state)
                {
                    case 0:
                        if (char.IsWhiteSpace(ch))
                            continue;
                        start = i;
                        ++state;
                        break;

                    case 1:
                        if (char.IsWhiteSpace(ch) || ch == '=')
                        {
                            if (start == -1)
                                return; // todo: decide if an exception should be thrown.
                            name = cookies.Substring(start, i - start);
                            start = -1;
                            ++state;
                        }
                        break;

                    case 2:
                        if (!char.IsWhiteSpace(ch) && ch != '=')
                        {
                            start = i;
                            ++state;
                        }
                        break;

                    case 3:
                        if (ch == ';')
                        {
                            if (start != -1)
                                Add(new RequestCookie(name, cookies.Substring(start, i - start)));
                            start = -1;
                            state = 0;
                            name = string.Empty;
                        }
                        break;
                }
            }

            // last cookie
            if (name == string.Empty)
                return;

            if (start == -1)
                Add(new RequestCookie(name, string.Empty));
            else
                Add(new RequestCookie(name, cookies.Substring(start, cookies.Length - start)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestCookieCollection"/> class.
        /// </summary>
        public RequestCookieCollection()
        {
            /* stub */
        }

        /// <summary>
        /// Adds a cookie in the collection.
        /// </summary>
        /// <param name="cookie">cookie to add</param>
        /// <exception cref="ArgumentNullException">cookie is <c>null</c></exception>
        /// <exception cref="ArgumentException">Name must be specified.</exception>
        internal void Add(RequestCookie cookie)
        {
            // Verifies the parameter
            if (cookie == null)
                throw new ArgumentNullException("cookie");
            if (cookie.Name == null || cookie.Name.Trim() == string.Empty)
                throw new ArgumentException("Name must be specified.");

            if (items.ContainsKey(cookie.Name))
                items[cookie.Name] = cookie;
            else items.Add(cookie.Name, cookie);
        }

        /// <summary>
        /// Remove all cookies.
        /// </summary>
        public void Clear()
        {
            items.Clear();
        }

        /// <summary>
        /// Remove a cookie from the collection.
        /// </summary>
        /// <param name="cookieName">Name of cookie.</param>
        public void Remove(string cookieName)
        {
            lock (items)
            {
                if (!items.ContainsKey(cookieName))
                    return;

                items.Remove(cookieName);
            }
        }

        /// <summary>
        /// Gets a collection enumerator on the cookie list.
        /// </summary>
        /// <returns>collection enumerator</returns>
        public IEnumerator GetEnumerator()
        {
            return items.Values.GetEnumerator();
        }

        ///<summary>
        ///Returns an enumerator that iterates through the collection.
        ///</summary>
        ///
        ///<returns>
        ///A <see cref="T:System.Collections.Generic.IEnumerator`1"></see> that can be used to iterate through the collection.
        ///</returns>
        ///<filterpriority>1</filterpriority>
        IEnumerator<RequestCookie> IEnumerable<RequestCookie>.GetEnumerator()
        {
            return items.Values.GetEnumerator();
        }
    } // public sealed class RequestCookieCollection : IEnumerable<RequestCookie>
} // namespace TridentFramework.RPC.Http.HttpMessages
