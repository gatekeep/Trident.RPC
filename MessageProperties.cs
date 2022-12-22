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

using System;
using System.Collections;
using System.Collections.Generic;

using Pair = System.Collections.Generic.KeyValuePair<string, object>;

namespace TridentFramework.RPC
{
    /// <summary>
    /// Represents a set of properties for a message. This class cannot be inherited.
    /// </summary>
    public sealed class MessageProperties : IDictionary<string, object>, ICollection<Pair>, IEnumerable<Pair>, IEnumerable
    {
        private List<Pair> list;

        /*
        ** Classes
        */

        /// <summary>
        ///
        /// </summary>
        private class ParameterKeyCollection : ICollection<string>
        {
            private List<Pair> source;

            /**
             * Properties
             */

            /// <summary>
            ///
            /// </summary>
            public int Count
            {
                get { return source.Count; }
            }

            /// <summary>
            ///
            /// </summary>
            public bool IsReadOnly
            {
                get { return true; }
            }

            /**
             * Methods
             */

            /// <summary>
            ///
            /// </summary>
            /// <param name="source"></param>
            public ParameterKeyCollection(List<Pair> source)
            {
                this.source = source;
            }

            /// <summary>
            ///
            /// </summary>
            /// <param name="item"></param>
            public void Add(string item)
            {
                throw new InvalidOperationException();
            }

            /// <summary>
            ///
            /// </summary>
            public void Clear()
            {
                throw new InvalidOperationException();
            }

            /// <summary>
            ///
            /// </summary>
            /// <param name="item"></param>
            /// <returns></returns>
            public bool Contains(string item)
            {
                for (int i = 0; i < source.Count; i++)
                    if (source[i].Key == item)
                        return true;
                return false;
            }

            /// <summary>
            ///
            /// </summary>
            /// <param name="array"></param>
            /// <param name="index"></param>
            public void CopyTo(string[] array, int index)
            {
                for (int i = 0; i < source.Count; i++)
                    array[index + i] = source[i].Key;
            }

            /// <summary>
            ///
            /// </summary>
            /// <returns></returns>
            public IEnumerator<string> GetEnumerator()
            {
                foreach (Pair p in source)
                    yield return p.Key;
            }

            /// <summary>
            ///
            /// </summary>
            /// <returns></returns>
            IEnumerator IEnumerable.GetEnumerator()
            {
                foreach (Pair p in source)
                    yield return p.Key;
            }

            /// <summary>
            ///
            /// </summary>
            /// <param name="item"></param>
            /// <returns></returns>
            public bool Remove(string item)
            {
                throw new InvalidOperationException();
            }
        } // private class ParameterKeyCollection : ICollection<string>

        /// <summary>
        ///
        /// </summary>
        private class ParameterValueCollection : ICollection<object>
        {
            private List<Pair> source;

            /**
             * Properties
             */

            /// <summary>
            ///
            /// </summary>
            public int Count
            {
                get { return source.Count; }
            }

            /// <summary>
            ///
            /// </summary>
            public bool IsReadOnly
            {
                get { return true; }
            }

            /**
             * Methods
             */

            /// <summary>
            ///
            /// </summary>
            /// <param name="source"></param>
            public ParameterValueCollection(List<Pair> source)
            {
                this.source = source;
            }

            /// <summary>
            ///
            /// </summary>
            /// <param name="item"></param>
            public void Add(object item)
            {
                throw new InvalidOperationException();
            }

            /// <summary>
            ///
            /// </summary>
            public void Clear()
            {
                throw new InvalidOperationException();
            }

            /// <summary>
            ///
            /// </summary>
            /// <param name="item"></param>
            /// <returns></returns>
            public bool Contains(object item)
            {
                for (int i = 0; i < source.Count; i++)
                    if (source[i].Value == item)
                        return true;
                return false;
            }

            /// <summary>
            ///
            /// </summary>
            /// <param name="array"></param>
            /// <param name="index"></param>
            public void CopyTo(object[] array, int index)
            {
                for (int i = 0; i < source.Count; i++)
                    array[index + i] = source[i].Value;
            }

            /// <summary>
            ///
            /// </summary>
            /// <returns></returns>
            public IEnumerator<object> GetEnumerator()
            {
                foreach (Pair p in source)
                    yield return p.Value;
            }

            /// <summary>
            ///
            /// </summary>
            /// <returns></returns>
            IEnumerator IEnumerable.GetEnumerator()
            {
                foreach (Pair p in source)
                    yield return p.Key;
            }

            /// <summary>
            ///
            /// </summary>
            /// <param name="item"></param>
            /// <returns></returns>
            public bool Remove(object item)
            {
                throw new InvalidOperationException();
            }
        } // private class ParameterValueCollection : ICollection<object>

        /*
        ** Properties
        */

        /// <summary>
        /// Gets the total number of properties in the <see cref="MessageProperties"/>.
        /// </summary>
        public int Count
        {
            get { return list.Count; }
        }

        /// <summary>
        /// Gets a value that indicates whether the <see cref="MessageProperties"/> has a fixed size.
        /// </summary>
        public bool IsFixedSize
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value that indicates whether this set of properties is read-only.
        /// </summary>
        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Retrieves a property with the specified name, identifier, or key value.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public object this[string name]
        {
            get
            {
                for (int i = 0; i < list.Count; i++)
                    if (list[i].Key == name)
                        return list[i].Value;
                return null;
            }
            set
            {
                for (int i = 0; i < list.Count; i++)
                    if (list[i].Key == name)
                    {
                        list[i] = new Pair(name, value);
                        return;
                    }
                list.Add(new Pair(name, value));
            }
        }

        /// <summary>
        /// Gets an <see cref="ICollection"/> that contains the keys in the <see cref="MessageProperties"/>.
        /// </summary>
        public ICollection<string> Keys
        {
            get { return new ParameterKeyCollection(list); }
        }

        /// <summary>
        /// Gets an <see cref="ICollection"/> that contains the values in the <see cref="MessageProperties"/>.
        /// </summary>
        public ICollection<object> Values
        {
            get { return new ParameterValueCollection(list); }
        }

        /// <summary>
        /// Gets or sets the transport address that is used to send messages.
        /// </summary>
        public Uri Via
        {
            get { return (Uri)this["Via"]; }
            set { this["Via"] = value; }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageProperties"/> class.
        /// </summary>
        public MessageProperties()
        {
            list = new List<Pair>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageProperties"/> class.
        /// </summary>
        /// <param name="properties"></param>
        public MessageProperties(MessageProperties properties)
        {
            list = new List<Pair>();
            CopyProperties(properties);
        }

        /// <summary>
        /// Adds an element with the specified name and property into the <see cref="MessageProperties"/> collection.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="property"></param>
        public void Add(string name, object property)
        {
            list.Add(new Pair(name, property));
        }

        /// <summary>
        /// Removes all elements from the <see cref="MessageProperties"/> collection.
        /// </summary>
        public void Clear()
        {
            list.Clear();
        }

        /// <summary>
        /// Determines whether the <see cref="MessageProperties"/> contains a specific name, key, or identifier.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool ContainsKey(string name)
        {
            for (int i = 0; i < list.Count; i++)
                if (list[i].Key == name)
                    return true;
            return false;
        }

        /// <summary>
        /// Copies the content of the specified <see cref="MessageProperties"/> to this instance.
        /// </summary>
        /// <param name="properties"></param>
        public void CopyProperties(MessageProperties properties)
        {
            list.AddRange(properties.list);
        }

        /// <summary>
        /// Removes the element with the specified name from the <see cref="MessageProperties"/>.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool Remove(string name)
        {
            for (int i = 0; i < list.Count; i++)
                if (list[i].Key == name)
                {
                    list.RemoveAt(i);
                    return true;
                }
            return false;
        }

        /// <summary>
        /// Determines whether the <see cref="MessageProperties"/> contains a specific name, and retrieves its value.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGetValue(string name, out object value)
        {
            for (int i = 0; i < list.Count; i++)
                if (list[i].Key == name)
                {
                    value = list[i].Value;
                    return true;
                }
            value = null;
            return false;
        }

        /// <summary>
        /// Adds an element with the specified name and property into the <see cref="MessageProperties"/>.
        /// </summary>
        /// <param name="pair"></param>
        void ICollection<Pair>.Add(Pair pair)
        {
            list.Add(pair);
        }

        /// <summary>
        /// Determines whether the <see cref="MessageProperties"/> contains a specific name.
        /// </summary>
        /// <param name="pair"></param>
        /// <returns></returns>
        bool ICollection<Pair>.Contains(Pair pair)
        {
            return list.Contains(pair);
        }

        /// <summary>
        /// Copies the content of the specified <see cref="MessageProperties"/> to an array, starting at the specified index.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="index"></param>
        void ICollection<Pair>.CopyTo(Pair[] array, int index)
        {
            list.CopyTo(array, index);
        }

        /// <summary>
        /// Removes the element with the specified name from the <see cref="MessageProperties"/>.
        /// </summary>
        /// <param name="pair"></param>
        /// <returns></returns>
        bool ICollection<Pair>.Remove(Pair pair)
        {
            return list.Remove(pair);
        }

        /// <summary>
        /// Returns an enumerator that can iterate through a collection.
        /// </summary>
        /// <returns></returns>
        IEnumerator<Pair> IEnumerable<Pair>.GetEnumerator()
        {
            return list.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that can iterate through a collection.
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)((IEnumerable<Pair>)this).GetEnumerator();
        }
    } // public sealed class MessageProperties : IDictionary<string, object>, ICollection<Pair>, IEnumerable<Pair>, IEnumerable
} // namespace TridentFramework.RPC
