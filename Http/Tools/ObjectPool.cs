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

namespace TridentFramework.RPC.Http.Tools
{
    /// <summary>
    /// Flyweight design pattern implementation.
    /// </summary>
    /// <typeparam name="T">Type of object.</typeparam>
    public class ObjectPool<T> where T : class
    {
        private readonly CreateHandler<T> createMethod;
        private readonly Queue<T> items = new Queue<T>();

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectPool{T}"/> class.
        /// </summary>
        /// <param name="createHandler">How large buffers to allocate.</param>
        public ObjectPool(CreateHandler<T> createHandler)
        {
            createMethod = createHandler;
        }

        /// <summary>
        /// Get an object.
        /// </summary>
        /// <returns>Created object.</returns>
        /// <remarks>Will create one if queue is empty.</remarks>
        public T Dequeue()
        {
            lock (items)
            {
                if (items.Count > 0)
                    return items.Dequeue();
            }

            return createMethod();
        }

        /// <summary>
        /// Enqueues the specified buffer.
        /// </summary>
        /// <param name="value">Object to enqueue.</param>
        /// <exception cref="ArgumentOutOfRangeException">Buffer is is less than the minimum requirement.</exception>
        public void Enqueue(T value)
        {
            lock (items)
                items.Enqueue(value);
        }
    } // public class ObjectPool<T> where T : class

    /// <summary>
    /// Used to create new objects.
    /// </summary>
    /// <typeparam name="T">Type of objects to create.</typeparam>
    /// <returns>Newly created object.</returns>
    /// <seealso cref="ObjectPool{T}"/>.
    public delegate T CreateHandler<T>() where T : class;
} // namespace TridentFramework.RPC.Http.Tools
