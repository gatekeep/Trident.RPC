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

namespace TridentFramework.RPC.Http.Headers
{
    /// <summary>
    /// Collection of headers.
    /// </summary>
    public class HeaderCollection : IHeaderCollection
    {
        private readonly HeaderFactory factory;
        private readonly Dictionary<string, IHeader> headers = new Dictionary<string, IHeader>(StringComparer.OrdinalIgnoreCase);

        /*
        ** Properties
        */

        /// <summary>
        /// Gets a header
        /// </summary>
        /// <param name="name">header name.</param>
        /// <returns>header if found; otherwise <c>null</c>.</returns>
        public IHeader this[string name]
        {
            get
            {
                IHeader header;
                return headers.TryGetValue(name, out header) ? header : null;
            }
            set
            {
                if (value == null)
                    headers.Remove(name);
                else
                    headers[name] = value;
            }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="HeaderCollection"/> class.
        /// </summary>
        /// <param name="factory">Factory used to created headers.</param>
        public HeaderCollection(HeaderFactory factory)
        {
            this.factory = factory;
        }

        /// <summary>
        /// Adds a header
        /// </summary>
        /// <remarks>
        /// Will replace any existing header with the same name.
        /// </remarks>
        /// <param name="header">header to add</param>
        /// <exception cref="ArgumentNullException"><c>header</c> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Header name cannot be <c>null</c>.</exception>
        public void Add(IHeader header)
        {
            if (header == null)
                throw new ArgumentNullException("header");
            if (header.Name == null)
                throw new ArgumentException("Header name cannot be null.");
            headers[header.Name] = header;
        }

        /// <summary>
        /// Add a header.
        /// </summary>
        /// <param name="name">Header name</param>
        /// <param name="value">Header value</param>
        /// <remarks>
        /// Will try to parse the header and create a <see cref="IHeader"/> object.
        /// </remarks>
        /// <exception cref="FormatException">Header value is not correctly formatted.</exception>
        /// <exception cref="ArgumentNullException"><c>name</c> or <c>value</c> is <c>null</c>.</exception>
        public void Add(string name, string value)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (value == null)
                throw new ArgumentNullException("value");
            IHeader header = factory.Parse(name, value);
            if (header == null)
                throw new FormatException("Failed to parse header " + name + "/" + value + ".");
            Add(header);
        }

        /// <summary>
        /// Add a header.
        /// </summary>
        /// <param name="name">Header name</param>
        /// <param name="value">Header value</param>
        /// <remarks>
        /// Will try to parse the header and create a <see cref="IHeader"/> object.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><c>name</c> or <c>value</c> is <c>null</c>.</exception>
        public void Add(string name, IHeader value)
        {
            if (name == null)
                throw new ArgumentNullException("value");
            if (value == null || value.Name == null)
                throw new ArgumentNullException("value");

            headers[name] = value;
        }

        /// <summary>
        /// Get a header
        /// </summary>
        /// <typeparam name="T">Type that it should be cast to</typeparam>
        /// <param name="headerName">Name of header</param>
        /// <returns>Header if found and casted properly; otherwise <c>null</c>.</returns>
        public T Get<T>(string headerName) where T : class, IHeader
        {
            IHeader header;
            if (headers.TryGetValue(headerName, out header))
                return header as T;
            return null;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<IHeader> GetEnumerator()
        {
            return headers.Values.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    } // public class HeaderCollection : IHeaderCollection
} // namespace TridentFramework.RPC.Http.Headers
