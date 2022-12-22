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

using System;
using System.Collections;
using System.Collections.Generic;

namespace TridentFramework.RPC
{
    /// <summary>
    /// Header in a message
    /// </summary>
    public interface IMessageHeader
    {
        /*
        ** Properties
        */

        /// <summary>
        /// Gets header name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets value as it would be sent back to client.
        /// </summary>
        string HeaderValue { get; }
    } // public interface IMessageHeader

    /// <summary>
    /// Used to store all message headers.
    /// </summary>
    public class MessageHeader : IMessageHeader
    {
        /*
        ** Properties
        */

        /// <summary>
        /// Gets or sets value
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Gets header name
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        ///
        /// </summary>
        public string HeaderValue
        {
            get { return Value; }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageHeader"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public MessageHeader(string name, string value)
        {
            Name = name;
            Value = value;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Value;
        }
    } // public class MessageHeader : IMessageHeader

    /// <summary>
    /// Collection of headers.
    /// </summary>
    public sealed class MessageHeaders : IEnumerable<IMessageHeader>
    {
        private readonly Dictionary<string, IMessageHeader> headers = new Dictionary<string, IMessageHeader>(StringComparer.OrdinalIgnoreCase);

        /*
        ** Properties
        */

        /// <summary>
        /// Gets a header
        /// </summary>
        /// <param name="name">header name.</param>
        /// <returns>header if found; otherwise <c>null</c>.</returns>
        public IMessageHeader this[string name]
        {
            get
            {
                IMessageHeader header;
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

        /// <summary>
        /// Gets the count of headers
        /// </summary>
        public int Count
        {
            get { return headers.Count; }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageHeaders"/> class.
        /// </summary>
        public MessageHeaders()
        {
            /* stub */
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
        public void Add(IMessageHeader header)
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
        /// Will try to parse the header and create a <see cref="IMessageHeader"/> object.
        /// </remarks>
        /// <exception cref="FormatException">Header value is not correctly formatted.</exception>
        /// <exception cref="ArgumentNullException"><c>name</c> or <c>value</c> is <c>null</c>.</exception>
        public void Add(string name, string value)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (value == null)
                throw new ArgumentNullException("value");
            IMessageHeader header = new MessageHeader(name, value);
            if (header == null)
                throw new FormatException("Failed to add header " + name + "/" + value + ".");
            Add(header);
        }

        /// <summary>
        /// Add a header.
        /// </summary>
        /// <param name="name">Header name</param>
        /// <param name="value">Header value</param>
        /// <remarks>
        /// Will try to parse the header and create a <see cref="IMessageHeader"/> object.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><c>name</c> or <c>value</c> is <c>null</c>.</exception>
        public void Add(string name, IMessageHeader value)
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
        public T Get<T>(string headerName) where T : class, IMessageHeader
        {
            IMessageHeader header;
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
        public IEnumerator<IMessageHeader> GetEnumerator()
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
} // namespace TridentFramework.RPC
