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
/*
 * Based on code from Lidgren Network v3
 * Copyright (C) 2010 Michael Lidgren., All Rights Reserved.
 * Licensed under MIT License.
 */

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;

namespace TridentFramework.RPC.Net
{
    /// <summary>
    /// Thread safe (blocking) expanding queue with TryDequeue() and EnqueueFirst()
    /// </summary>
    /// <typeparam name="T">The type of object to contain in queue.</typeparam>
    public sealed class ThreadSafeQueue<T>
    {
        // Example:
        // m_capacity = 8
        // m_size = 6
        // m_head = 4
        //
        // [0] item
        // [1] item (tail = ((head + size - 1) % capacity)
        // [2]
        // [3]
        // [4] item (head)
        // [5] item
        // [6] item
        // [7] item
        //
        private T[] items;

        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private int size;
        private int head;

        /*
        ** Properties
        */

        /// <summary>
        /// Gets the number of items in the queue
        /// </summary>
        public int Count
        {
            get
            {
                _lock.EnterReadLock();
                int count = size;
                _lock.ExitReadLock();
                return count;
            }
        }

        /// <summary>
        /// Gets the current capacity for the queue
        /// </summary>
        public int Capacity
        {
            get
            {
                _lock.EnterReadLock();
                int capacity = items.Length;
                _lock.ExitReadLock();
                return capacity;
            }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadSafeQueue{T}"/> class.
        /// </summary>
        /// <param name="initialCapacity"></param>
        public ThreadSafeQueue(int initialCapacity)
        {
            items = new T[initialCapacity];
        }

        /// <summary>
        /// Adds an item last/tail of the queue
        /// </summary>
        public void Enqueue(T item)
        {
            _lock.EnterWriteLock();
            try
            {
                if (size == items.Length)
                    SetCapacity(items.Length + 8);

                int slot = (head + size) % items.Length;
                items[slot] = item;
                size++;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Adds an item last/tail of the queue
        /// </summary>
        public void Enqueue(IEnumerable<T> items)
        {
            _lock.EnterWriteLock();
            try
            {
                foreach (var item in items)
                {
                    if (size == this.items.Length)
                        SetCapacity(this.items.Length + 8); // @TODO move this out of loop

                    int slot = (head + size) % this.items.Length;
                    this.items[slot] = item;
                    size++;
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Places an item first, at the head of the queue
        /// </summary>
        public void EnqueueFirst(T item)
        {
            _lock.EnterWriteLock();
            try
            {
                if (size >= items.Length)
                    SetCapacity(items.Length + 8);

                head--;
                if (head < 0)
                    head = items.Length - 1;
                items[head] = item;
                size++;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        // must be called from within a write locked m_lock!
        private void SetCapacity(int newCapacity)
        {
            if (size == 0)
            {
                if (size == 0)
                {
                    items = new T[newCapacity];
                    head = 0;
                    return;
                }
            }

            T[] newItems = new T[newCapacity];

            if (head + size - 1 < items.Length)
            {
                Array.Copy(items, head, newItems, 0, size);
            }
            else
            {
                Array.Copy(items, head, newItems, 0, items.Length - head);
                Array.Copy(items, 0, newItems, items.Length - head, (size - (items.Length - head)));
            }

            items = newItems;
            head = 0;
        }

        /// <summary>
        /// Gets an item from the head of the queue, or returns default(T) if empty
        /// </summary>
        public bool TryDequeue(out T item)
        {
            if (size == 0)
            {
                item = default(T);
                return false;
            }

            _lock.EnterWriteLock();
            try
            {
                if (size == 0)
                {
                    item = default(T);
                    return false;
                }

                item = items[head];
                items[head] = default(T);

                head = (head + 1) % items.Length;
                size--;

                return true;
            }
            catch
            {
#if DEBUG
                throw;
#else
				item = default(T);
				return false;
#endif
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Gets all items from the head of the queue, or returns number of items popped
        /// </summary>
        public int TryDrain(IList<T> addTo)
        {
            if (size == 0)
                return 0;

            _lock.EnterWriteLock();
            try
            {
                int added = size;
                while (size > 0)
                {
                    var item = items[head];
                    addTo.Add(item);

                    items[head] = default(T);
                    head = (head + 1) % items.Length;
                    size--;
                }
                return added;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Returns default(T) if queue is empty
        /// </summary>
        public T TryPeek(int offset)
        {
            if (size == 0)
                return default(T);

            _lock.EnterReadLock();
            try
            {
                if (size == 0)
                    return default(T);
                return items[(head + offset) % items.Length];
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        /// Determines whether an item is in the queue
        /// </summary>
        public bool Contains(T item)
        {
            _lock.EnterReadLock();
            try
            {
                int ptr = head;
                for (int i = 0; i < size; i++)
                {
                    if (items[ptr] == null)
                    {
                        if (item == null)
                            return true;
                    }
                    else
                    {
                        if (items[ptr].Equals(item))
                            return true;
                    }
                    ptr = (ptr + 1) % items.Length;
                }
                return false;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        /// Copies the queue items to a new array
        /// </summary>
        public T[] ToArray()
        {
            _lock.EnterReadLock();
            try
            {
                T[] retval = new T[size];
                int ptr = head;
                for (int i = 0; i < size; i++)
                {
                    retval[i] = items[ptr++];
                    if (ptr >= items.Length)
                        ptr = 0;
                }
                return retval;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        /// Removes all objects from the queue
        /// </summary>
        public void Clear()
        {
            _lock.EnterWriteLock();
            try
            {
                for (int i = 0; i < items.Length; i++)
                    items[i] = default(T);
                head = 0;
                size = 0;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
    } // public sealed class ThreadSafeQueue<T>
} // namespace TridentFramework.RPC.Net
