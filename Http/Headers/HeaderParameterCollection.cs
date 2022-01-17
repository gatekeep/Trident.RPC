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
using TridentFramework.RPC.Http.Tools;

namespace TridentFramework.RPC.Http.Headers
{
    /// <summary>
    /// Contains parameters for HTTP headers.
    /// </summary>
    public class HeaderParameterCollection
    {
        private readonly Dictionary<string, string> items = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /*
        ** Properties
        */

        /// <summary>
        /// Gets or sets a value
        /// </summary>
        /// <param name="name">parameter name</param>
        /// <returns>value if found; otherwise <c>null</c>.</returns>
        public string this[string name]
        {
            get
            {
                string value;
                return items.TryGetValue(name, out value) ? value : null;
            }
            set { items[name] = value; }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Add a parameter
        /// </summary>
        /// <param name="name">name</param>
        /// <param name="value">value</param>
        /// <remarks>
        /// Existing parameter with the same name will be replaced.
        /// </remarks>
        public void Add(string name, string value)
        {
            items[name] = value;
        }

        /// <summary>
        /// Parse parameters.
        /// </summary>
        /// <param name="reader">Parser containing buffer to parse.</param>
        /// <returns>A collection with all parameters (or just a empty collection).</returns>
        /// <exception cref="FormatException">Expected a value after equal sign.</exception>
        public static HeaderParameterCollection Parse(ITextReader reader)
        {
            return Parse(reader, ';');
        }

        /// <summary>
        /// Parse parameters.
        /// </summary>
        /// <param name="reader">Parser containing buffer to parse.</param>
        /// <param name="delimiter">Parameter delimiter</param>
        /// <returns>A collection with all parameters (or just a empty collection).</returns>
        /// <exception cref="FormatException">Expected a value after equal sign.</exception>
        public static HeaderParameterCollection Parse(ITextReader reader, char delimiter)
        {
            if (reader.Current == delimiter)
                reader.Consume();
            reader.ConsumeWhiteSpaces();

            var collection = new HeaderParameterCollection();
            string name = reader.ReadToEnd("=" + delimiter);
            while (name != string.Empty && !reader.EOF)
            {
                // got a parameter value
                if (reader.Current == '=')
                {
                    reader.ConsumeWhiteSpaces('=');

                    string value = reader.Current == '"'
                                       ? reader.ReadQuotedString()
                                       : reader.ReadToEnd(delimiter);

                    reader.ConsumeWhiteSpaces();
                    if (value == string.Empty && reader.Current != delimiter)
                        throw new FormatException("Expected a value after equal sign.");

                    collection.Add(name, value);
                }
                else // got no value
                {
                    collection.Add(name, string.Empty);
                }

                reader.ConsumeWhiteSpaces(delimiter); // consume delimiter and white spaces
                name = reader.ReadToEnd("=" + delimiter);
            }
            return collection;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var pair in items)
                sb.AppendFormat("{0}={1};", pair.Key, pair.Value);
            return sb.ToString();
        }
    } // public class HeaderParameterCollection
} // namespace TridentFramework.RPC.Http.Headers
