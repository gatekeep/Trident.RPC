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
/*
 * Based on code from .NET Reference Source
 * Copyright (C) Microsoft Corporation., All Rights Reserved.
 * Licensed under MIT License.
 */

using System;
using System.Collections.Generic;

namespace TridentFramework.RPC
{
    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class EmptyArray<T>
    {
        private static T[] instance;

        /*
        ** Properties
        */

        /// <summary>
        ///
        /// </summary>
        internal static T[] Instance
        {
            get
            {
                if (instance == null)
                    instance = new T[0];
                return instance;
            }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="EmptyArray"/> class.
        /// </summary>
        private EmptyArray()
        {
            /* stub */
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        internal static T[] Allocate(int n)
        {
            if (n == 0)
                return Instance;
            else
                return new T[n];
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="collection"></param>
        /// <returns></returns>
        internal static T[] ToArray(IList<T> collection)
        {
            if (collection.Count == 0)
                return EmptyArray<T>.Instance;
            else
            {
                T[] array = new T[collection.Count];
                collection.CopyTo(array, 0);
                return array;
            }
        }
    } // internal class EmptyArray<T>

    /// <summary>
    ///
    /// </summary>
    internal class EmptyArray
    {
        private static object[] instance = new object[0];

        /*
        ** Properties
        */

        /// <summary>
        ///
        /// </summary>
        internal static object[] Instance
        {
            get
            {
                return instance;
            }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="EmptyArray"/> class.
        /// </summary>
        private EmptyArray()
        {
            /* stub */
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        internal static object[] Allocate(int n)
        {
            if (n == 0)
                return Instance;
            else
                return new object[n];
        }
    } // internal class EmptyArray
} // namespace TridentFramework.RPC
