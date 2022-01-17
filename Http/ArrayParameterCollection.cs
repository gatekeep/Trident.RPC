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

namespace TridentFramework.RPC.Http
{
    /// <summary>
    /// Form parameters where form string arrays have been converted to real arrays.
    /// </summary>
    public class ArrayParameterCollection : Parameter, IParameterCollection
    {
        private readonly Dictionary<string, IParameter> items = new Dictionary<string, IParameter>(StringComparer.OrdinalIgnoreCase);

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="ArrayParameterCollection"/> class.
        /// </summary>
        public ArrayParameterCollection() : base("root", string.Empty)
        {
            /* stub */
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ArrayParameterCollection"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        protected ArrayParameterCollection(string name, string value)
            : base(name, value)
        {
            /* stub */
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ArrayParameterCollection"/> class.
        /// </summary>
        /// <param name="collection">Parse parameters from the this collection.</param>
        public ArrayParameterCollection(IEnumerable<IParameter> collection) : base("root", string.Empty)
        {
            foreach (var item in collection)
            {
                foreach (var value in item)
                    Add(item.Name, value);
            }
        }

        /// <summary>
        /// Gets first value of an item.
        /// </summary>
        /// <value></value>
        /// <returns>String if found; otherwise <c>null</c>.</returns>
        public ArrayParameterCollection this[string name]
        {
            get { return GetItem(name); }
        }

        private ArrayParameterCollection GetItem(string name)
        {
            IParameter parameter;
            if (!items.TryGetValue(name, out parameter))
                return null;

            return (ArrayParameterCollection)parameter;
        }

        /// <summary>
        /// Get a parameter.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public IParameter Get(string name)
        {
            return GetItem(name);
        }

        /// <summary>
        /// Add a parameter
        /// </summary>
        /// <param name="name">Name of parameter, can contain a string array.</param>
        /// <param name="value">Value</param>
        /// <example>
        /// <code>
        /// ArrayParameterCollection array = new ArrayParameterCollection();
        /// array.Add("user[FirstName]", "Jonas");
        /// array.Add("user[FirstName]", "Arne");
        /// string firstName = array["user"]["FirstName"].Value; // "Arne" is returned
        /// foreach (string value in array["user"]["FirstName"])
        ///   Console.WriteLine(value);  // each name is displayed.
        /// </code>
        /// </example>
        public void Add(string name, string value)
        {
            int pos = name.IndexOf('[');
            if (pos != -1)
            {
                string myName = name.Substring(0, pos);
                name = name.Remove(0, pos + 1);
                pos = name.IndexOf(']');
                name = name.Remove(pos, 1);

                ArrayParameterCollection mine = GetItem(myName);
                if (mine == null)
                {
                    mine = new ArrayParameterCollection(myName, string.Empty);
                    items.Add(myName, mine);
                }

                mine.Add(name, value);
                return;
            }

            // new parameter
            ArrayParameterCollection current = GetItem(name);
            if (current == null)
            {
                current = new ArrayParameterCollection(name, value);
                items.Add(name, current);
                return;
            }

            // existing
            current.Values.Add(value);
        }

        /// <summary>
        /// Checks if the specified parameter exists
        /// </summary>
        /// <param name="name">Parameter name.</param>
        /// <returns><c>true</c> if found; otherwise <c>false</c>;</returns>
        public bool Exists(string name)
        {
            return items.ContainsKey(name);
        }

        /// <summary>
        /// Gets number of parameters.
        /// </summary>
        public int Count
        {
            get { return items.Count; }
        }

        /// <summary>
        /// Gets last value of an parameter.
        /// </summary>
        /// <param name="name">Parameter name</param>
        /// <returns>String if found; otherwise <c>null</c>.</returns>
        string IParameterCollection.this[string name]
        {
            get { return GetItem(name).Value; }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<IParameter> GetEnumerator()
        {
            return items.Values.GetEnumerator();
        }
    } // public class ArrayParameterCollection : Parameter, IParameterCollection
} // namespace TridentFramework.RPC.Http
