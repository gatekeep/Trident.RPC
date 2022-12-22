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

using System.Collections.Generic;

namespace TridentFramework.RPC.Http
{
    /// <summary>
    /// HTTP methods.
    /// </summary>
    public static class Method
    {
        /// <summary>
        /// Unknown method
        /// </summary>
        public const string Unknown = "";

        /// <summary>
        /// Posting data
        /// </summary>
        public const string Post = "POST";

        /// <summary>
        /// Get data
        /// </summary>
        public const string Get = "GET";

        /// <summary>
        /// Update data
        /// </summary>
        public const string Put = "PUT";

        /// <summary>
        /// Remove data
        /// </summary>
        public const string Delete = "DELETE";

        /// <summary>
        /// Get only HTTP headers.
        /// </summary>
        public const string Head = "HEAD";

        /// <summary>
        /// Options HTTP 1.1 header.
        /// </summary>
        public const string Options = "OPTIONS";

        private static List<string> supportedMethods = new List<string>
        {
            Post,
            Get,
            Put,
            Delete,
            Head,
            Options
        };

        /*
        ** Properties
        */

        /// <summary>
        ///
        /// </summary>
        public static IEnumerable<string> Methods { get { return supportedMethods; } }

        /*
        ** Methods
        */

        /// <summary>
        ///
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool IsSupported(string name)
        {
            return supportedMethods.Contains(name);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="name"></param>
        public static void AddMethod(string name)
        {
            supportedMethods.Add(name);
        }
    } // public static class Method
} // namespace TridentFramework.RPC.Http
