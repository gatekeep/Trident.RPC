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

using TridentFramework.RPC.Http.Tools;

namespace TridentFramework.RPC.Http.Headers.Parsers
{
    /// <summary>
    /// Parses <see cref="ConnectionHeader"/>.
    /// </summary>
    [ParserFor(ConnectionHeader.NAME)]
    public class ConnectionParser : IHeaderParser
    {
        /*
        ** Methods
        */

        /// <summary>
        /// Parse a header
        /// </summary>
        /// <param name="name">Name of header.</param>
        /// <param name="reader">Reader containing value.</param>
        /// <returns>HTTP Header</returns>
        /// <exception cref="FormatException">Header value is not of the expected format.</exception>
        public IHeader Parse(string name, ITextReader reader)
        {
            string typeStr = reader.ReadToEnd(",;");
            if (reader.Current == ',') // to get rid of the TE header.
                reader.ReadToEnd(';');

            ConnectionType type;

            try
            {
                type = (ConnectionType)Enum.Parse(typeof(ConnectionType), typeStr.Replace("-", string.Empty), true);
            }
            catch (ArgumentException err)
            {
                throw new FormatException("Unknown connection type '" + typeStr + "'.", err);
            }

            // got parameters
            if (reader.Current == ';')
            {
                HeaderParameterCollection parameters = HeaderParameterCollection.Parse(reader);
                return new ConnectionHeader(type, parameters);
            }

            return new ConnectionHeader(type);
        }
    } // public class ConnectionParser : IHeaderParser
} // namespace TridentFramework.RPC.Http.Headers.Parsers
