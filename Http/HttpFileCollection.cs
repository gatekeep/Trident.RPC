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
using System.Collections.Generic;
using System.IO;

namespace TridentFramework.RPC.Http
{
    /// <summary>
    /// Collection of files.
    /// </summary>
    public class HttpFileCollection
    {
        private readonly Dictionary<string, HttpFile> files = new Dictionary<string, HttpFile>(StringComparer.OrdinalIgnoreCase);

        /*
        ** Properties
        */

        /// <summary>
        /// Get a file
        /// </summary>
        /// <param name="name">Name in form</param>
        /// <returns>File if found; otherwise <c>null</c>.</returns>
        public HttpFile this[string name]
        {
            get
            {
                HttpFile file;
                return files.TryGetValue(name, out file) ? file : null;
            }
        }

        /// <summary>
        /// Gets number of files
        /// </summary>
        public int Count
        {
            get { return files.Count; }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Checks if a file exists.
        /// </summary>
        /// <param name="name">Name of the file (form item name)</param>
        /// <returns></returns>
        public bool Contains(string name)
        {
            return files.ContainsKey(name);
        }

        /// <summary>
        /// Add a new file.
        /// </summary>
        /// <param name="file">File to add.</param>
        public void Add(HttpFile file)
        {
            files.Add(file.Name, file);
        }

        /// <summary>
        /// Remove all files from disk.
        /// </summary>
        public void Clear()
        {
            foreach (HttpFile file in files.Values)
            {
                if (File.Exists(file.TempFileName))
                    File.Delete(file.TempFileName);
            }
        }
    } // public class HttpFileCollection
} // namespace TridentFramework.RPC.Http
