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
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace TridentFramework.RPC.Http
{
    /// <summary>
    /// Collection of parameters.
    /// </summary>
    /// <remarks>
    /// <see cref="Dictionary{TKey,TValue}"/> or <see cref="NameValueCollection"/> is not used since each parameter can
    /// have multiple values.
    /// </remarks>
    public class ParameterCollection : IParameterCollection
    {
        private readonly Dictionary<string, IParameter> items = new Dictionary<string, IParameter>(StringComparer.OrdinalIgnoreCase);

        /*
        ** Properties
        */

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
        public string this[string name]
        {
            get
            {
                IParameter param = Get(name);
                return param != null ? param.Value : null;
            }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterCollection"/> class.
        /// </summary>
        /// <param name="collections">Collections to merge.</param>
        /// <remarks>
        /// Later collections will overwrite parameters from earlier collections.
        /// </remarks>
        public ParameterCollection(params IParameterCollection[] collections)
        {
            foreach (IParameterCollection collection in collections)
            {
                if (collection == null)
                    continue;
                foreach (IParameter p in collection)
                {
                    foreach (string value in p)
                        Add(p.Name, value);
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterCollection"/> class.
        /// </summary>
        public ParameterCollection()
        {
            /* stub */
        }

        /// <summary>
        /// Get a list of string arrays.
        /// </summary>
        /// <returns></returns>
        public string[] GetArrayNames()
        {
            var names = new List<string>();
            foreach (var item in items)
            {
                int pos = item.Key.IndexOf("[");
                if (pos == -1)
                    continue;

                names.Add(item.Key.Substring(0, pos));
            }

            return names.ToArray();
        }

        /// <summary>
        /// Get parameters
        /// </summary>
        /// <param name="arrayName">Sub array (text array)</param>
        /// <returns></returns>
        public IParameterCollection GetParameters(string arrayName)
        {
            ParameterCollection collection = new ParameterCollection();
            arrayName = arrayName + "[";
            foreach (KeyValuePair<string, IParameter> item in items)
            {
                if (!item.Key.StartsWith(arrayName)) continue;
                int pos = arrayName.IndexOf("]");
                if (pos == -1) continue;

                string name = arrayName.Substring(arrayName.Length, pos - arrayName.Length);
                foreach (string value in item.Value)
                    collection.Add(name, value);
            }

            return collection;
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

        /// <summary>
        /// Get a parameter.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public IParameter Get(string name)
        {
            IParameter parameter;
            return items.TryGetValue(name, out parameter) ? parameter : null;
        }

        /// <summary>
        /// Add a query string parameter.
        /// </summary>
        /// <param name="name">Parameter name</param>
        /// <param name="value">Value</param>
        public void Add(string name, string value)
        {
            IParameter parameter;
            if (!items.TryGetValue(name, out parameter))
            {
                parameter = new Parameter(name, value);
                items.Add(name, parameter);
            }
            else
                parameter.Values.Add(value);
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
    } // public class ParameterCollection : IParameterCollection
} // namespace TridentFramework.RPC.Http
