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

namespace TridentFramework.RPC.Http.Tools
{
    /// <summary>
    /// Parses query string
    /// </summary>
    public static class UrlParser
    {
        /*
        ** Methods
        */

        /// <summary>
        /// Parse a query string
        /// </summary>
        /// <param name="reader">string to parse</param>
        /// <returns>A collection</returns>
        /// <exception cref="ArgumentNullException"><c>reader</c> is <c>null</c>.</exception>
        public static ParameterCollection Parse(ITextReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException("reader");

            ParameterCollection parameters = new ParameterCollection();
            while (!reader.EOF)
            {
                string name = HttpUtility.UrlDecode(reader.ReadToEnd("&="));
                char current = reader.Current;
                reader.Consume();
                switch (current)
                {
                    case '&':
                        parameters.Add(name, string.Empty);
                        break;

                    case '=':
                        {
                            string value = reader.ReadToEnd("&");
                            reader.Consume();
                            parameters.Add(name, HttpUtility.UrlDecode(value));
                        }
                        break;

                    default:
                        parameters.Add(name, string.Empty);
                        break;
                }
            }

            return parameters;
        }

        /// <summary>
        /// Parse a query string
        /// </summary>
        /// <param name="queryString">string to parse</param>
        /// <returns>A collection</returns>
        /// <exception cref="ArgumentNullException"><c>queryString</c> is <c>null</c>.</exception>
        public static ParameterCollection Parse(string queryString)
        {
            if (queryString == null)
                throw new ArgumentNullException("queryString");
            if (queryString.Length == 0)
                return new ParameterCollection();

            StringReader reader = new StringReader(queryString);
            return Parse(reader);
        }

        /// <summary>
        /// Parse a query string
        /// </summary>
        /// <param name="uri">URI to parse</param>
        /// <returns>A collection</returns>
        /// <exception cref="ArgumentNullException"><c>queryString</c> is <c>null</c>.</exception>
        public static ParameterCollection Parse(Uri uri)
        {
            if (uri == null)
                throw new ArgumentNullException("uri");

            string queryString = uri.GetComponents(UriComponents.Query, UriFormat.SafeUnescaped);
            StringReader reader = new StringReader(queryString);
            return Parse(reader);
        }
    } // public static class UrlParser
} // namespace TridentFramework.RPC.Http.Tools
