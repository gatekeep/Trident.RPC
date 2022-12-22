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

namespace TridentFramework.RPC.Http.HttpMessages.Parser
{
    /// <summary>
    /// Event arguments used when a new header have been parsed.
    /// </summary>
    public class HeaderEventArgs : EventArgs
    {
        /*
        ** Properties
        */

        /// <summary>
        /// Gets or sets header name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets header value.
        /// </summary>
        public string Value { get; set; }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="HeaderEventArgs"/> class.
        /// </summary>
        /// <param name="name">Name of header.</param>
        /// <param name="value">Header value.</param>
        /// <exception cref="ArgumentException">Name cannot be empty</exception>
        /// <exception cref="ArgumentNullException"><c>value</c> is <c>null</c>.</exception>
        public HeaderEventArgs(string name, string value)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Name cannot be empty", "name");
            if (value == null)
                throw new ArgumentNullException("value");

            Name = name;
            Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HeaderEventArgs"/> class.
        /// </summary>
        public HeaderEventArgs()
        {
            /* stub */
        }
    } // public class HeaderEventArgs : EventArgs
} // namespace TridentFramework.RPC.Http.HttpMessages.Parser
