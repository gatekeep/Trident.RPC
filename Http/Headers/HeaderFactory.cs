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
using System.Collections.Generic;
using System.Reflection;
using TridentFramework.RPC.Http.Headers.Parsers;
using TridentFramework.RPC.Http.Tools;

namespace TridentFramework.RPC.Http.Headers
{
    /// <summary>
    /// Used to build headers.
    /// </summary>
    [Component]
    public class HeaderFactory
    {
        private readonly Dictionary<string, IHeaderParser> parsers = new Dictionary<string, IHeaderParser>(StringComparer.OrdinalIgnoreCase);
        private readonly ObjectPool<StringReader> readers = new ObjectPool<StringReader>(() => new StringReader(string.Empty));
        private readonly IHeaderParser stringParser = new StringParser();

        /*
        ** Methods
        */

        /// <summary>
        /// Add a parser
        /// </summary>
        /// <param name="name">Header that the parser is for.</param>
        /// <param name="parser">Parser implementation</param>
        /// <remarks>
        /// Will replace any existing parser for the specified header.
        /// </remarks>
        public void Add(string name, IHeaderParser parser)
        {
            parsers[name] = parser;
        }

        /// <summary>
        /// Add all default (built-in) parsers.
        /// </summary>
        /// <remarks>
        /// Will not replace previously added parsers.
        /// </remarks>
        public void AddDefaultParsers()
        {
            Type interfaceType = typeof(IHeaderParser);
            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (type.IsInterface || type.IsAbstract)
                    continue;

                if (!interfaceType.IsAssignableFrom(type))
                    continue;

                CreateParser(type);
            }
        }

        /// <summary>
        /// Create a header parser
        /// </summary>
        /// <param name="type"><see cref="IHeaderParser"/> implementation.</param>
        /// <remarks>
        /// <para>
        /// Uses <see cref="ParserForAttribute"/> attribute to find which headers
        /// the parser is for.
        /// </para>
        /// <para>Will not replace previously added parsers.</para>
        /// </remarks>
        private void CreateParser(Type type)
        {
            var parser = (IHeaderParser)Activator.CreateInstance(type);

            object[] attributes = type.GetCustomAttributes(true);
            foreach (object attr in attributes)
            {
                var attribute = attr as ParserForAttribute;
                if (attribute == null)
                    continue;

                // do not replace already added parsers.
                if (parsers.ContainsKey(attribute.HeaderName))
                    continue;

                parsers[attribute.HeaderName] = parser;
            }
        }

        /// <summary>
        /// Parse a header.
        /// </summary>
        /// <param name="name">Name of header</param>
        /// <param name="value">Header value</param>
        /// <returns>Header.</returns>
        /// <exception cref="FormatException">Value is not a well formatted header value.</exception>
        public IHeader Parse(string name, string value)
        {
            IHeaderParser parser;
            if (!parsers.TryGetValue(name, out parser))
                parser = stringParser;

            StringReader reader = readers.Dequeue();
            reader.Assign(value);
            try
            {
                return parser.Parse(name, reader);
            }
            finally
            {
                readers.Enqueue(reader);
            }
        }
    } // public class HeaderFactory
} // namespace TridentFramework.RPC.Http.Headers
