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
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Schema;

namespace TridentFramework.RPC
{
    /// <summary>
    /// Helper class used to contain the data in an <see cref="DataItem{TKey, TValue}"/>
    /// </summary>
    /// <typeparam name="TKey">Type of the Key</typeparam>
    /// <typeparam name="TValue">Type of the Value</typeparam>
    public class DataItem<TKey, TValue>
    {
        /// <summary>
        ///
        /// </summary>
        public TKey Key;

        /// <summary>
        ///
        /// </summary>
        public TValue Value;

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="DataItem{TKey, TValue}"/> class.
        /// </summary>
        public DataItem()
        {
            /* stub */
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataItem{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public DataItem(TKey key, TValue value)
        {
            this.Key = key;
            this.Value = value;
        }
    } // public class DataItem<TKey, TValue>

    /// <summary>
    /// Represents a collection of keys and values.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the dictionary</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary</typeparam>
    public class DictionarySerializable<TKey, TValue> : IXmlSerializable, IDictionary
    {
        /// <summary>
        /// Gets a collection containing the key-value pair items in the dictionary.
        /// </summary>
        public List<DataItem<TKey, TValue>> Items;

        private object _syncRoot;

        /*
        ** Properties
        */

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get or set</param>
        /// <returns></returns>
        public TValue this[TKey key]
        {
            get { return Items.Find(x => (x.Key.Equals(key))).Value; }
            set { Items.Find(x => (x.Key.Equals(key))).Value = value; }
        }

        /// <summary>
        /// Gets the number of key/value pairs contained in the <see cref="DictionarySerializable{TKey, TValue}"/>
        /// </summary>
        public int Count
        {
            get { return Items.Count; }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="IDictionary"/> object has a fixed size.
        /// </summary>
        bool IDictionary.IsFixedSize
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="IDictionary"/> object is read-only.
        /// </summary>
        bool IDictionary.IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Gets an <see cref="ICollection"/> object containing the keys of the <see cref="IDictionary"/> object.
        /// </summary>
        ICollection IDictionary.Keys
        {
            get { throw new NotSupportedException(); }
        }

        /// <summary>
        /// Gets an <see cref="ICollection"/> object containing the values in the <see cref="IDictionary"/> object.
        /// </summary>
        ICollection IDictionary.Values
        {
            get { throw new NotSupportedException(); }
        }

        /// <summary>
        /// Gets or sets the element with the specified key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        object IDictionary.this[object key]
        {
            get
            {
                if (IsCompatibleKey(key))
                    return Items.Find(x => (x.Key.Equals(key)));
                return null;
            }
            set
            {
                if (key == null)
                    throw new ArgumentNullException("key");

                try
                {
                    TKey tempKey = (TKey)key;
                    try
                    {
                        this[tempKey] = (TValue)value;
                    }
                    catch (InvalidCastException)
                    {
                        throw new ArgumentException("TValue is the wrong type");
                    }
                }
                catch (InvalidCastException)
                {
                    throw new ArgumentException("TKey is the wrong type");
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        /// <summary>
        ///
        /// </summary>
        object ICollection.SyncRoot
        {
            get
            {
                if (_syncRoot == null)
                    Interlocked.CompareExchange<Object>(ref _syncRoot, new Object(), null);
                return _syncRoot;
            }
        }

        /*
        ** Operators
        */

        /// <summary>
        /// Converts a <see cref="DictionarySerializable{TKey, TValue}"/> to a <see cref="Dictionary{TKey, TValue}"/>
        /// </summary>
        /// <param name="xmlDict"></param>
        public static implicit operator Dictionary<TKey, TValue>(DictionarySerializable<TKey, TValue> xmlDict)
        {
            Dictionary<TKey, TValue> dict = new Dictionary<TKey, TValue>(xmlDict.Count);

            foreach (DataItem<TKey, TValue> item in xmlDict.Items)
                dict.Add(item.Key, item.Value);

            return dict;
        }

        /// <summary>
        /// Converts a <see cref="Dictionary{TKey, TValue}"/> to a <see cref="DictionarySerializable{TKey, TValue}"/>
        /// </summary>
        /// <param name="dict"></param>
        public static implicit operator DictionarySerializable<TKey, TValue>(Dictionary<TKey, TValue> dict)
        {
            DictionarySerializable<TKey, TValue> xmlDict = new DictionarySerializable<TKey, TValue>(dict.Count);

            foreach (KeyValuePair<TKey, TValue> kvp in dict)
                xmlDict.Add(kvp.Key, kvp.Value);

            return xmlDict;
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="DictionarySerializable{TKey, TValue}"/> class.
        /// </summary>
        public DictionarySerializable()
        {
            this.Items = new List<DataItem<TKey, TValue>>(16);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DictionarySerializable{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="capacity"></param>
        public DictionarySerializable(int capacity)
        {
            this.Items = new List<DataItem<TKey, TValue>>(capacity);
        }

        /// <summary>
        /// Converts a <see cref="Dictionary{TKey, TValue}"/> to a <see cref="DictionarySerializable{TKey, TValue}"/>
        /// </summary>
        /// <param name="dict"></param>
        /// <returns></returns>
        public static DictionarySerializable<TKey, TValue> FromDictionary(Dictionary<TKey, TValue> dict)
        {
            if (dict == null)
                throw new ArgumentNullException("dict");

            DictionarySerializable<TKey, TValue> dstDict = new DictionarySerializable<TKey, TValue>(dict.Count);

            foreach (KeyValuePair<TKey, TValue> kvp in dict)
                dstDict.Add(kvp.Key, kvp.Value);

            return dstDict;
        }

        /// <summary>
        /// Converts a <see cref="DictionarySerializable{TKey, TValue}"/> to a <see cref="Dictionary{TKey, TValue}"/>
        /// </summary>
        /// <param name="dict"></param>
        /// <returns></returns>
        public static Dictionary<TKey, TValue> ToDictionary(DictionarySerializable<TKey, TValue> dict)
        {
            if (dict == null)
                throw new ArgumentNullException("dict");

            Dictionary<TKey, TValue> dstDict = new Dictionary<TKey, TValue>(dict.Count);

            foreach (DataItem<TKey, TValue> item in dict.Items)
                dstDict.Add(item.Key, item.Value);

            return dstDict;
        }

        /// <summary>
        /// Determines whether the <see cref="DictionarySerializable{TKey, TValue}"/> contains the specified key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsKey(TKey key)
        {
            return Items.Find(x => (x.Key.Equals(key))) != null;
        }

        /// <summary>
        /// Determines whether the <see cref="DictionarySerializable{TKey, TValue}"/> contains a specific value.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool ContainsValue(TValue value)
        {
            if (value == null)
            {
                for (int i = 0; i < Items.Count; i++)
                    if (Items[i].GetHashCode() >= 0 && Items[i].Value == null)
                        return true;
            }
            else
            {
                EqualityComparer<TValue> c = EqualityComparer<TValue>.Default;
                for (int i = 0; i < Items.Count; i++)
                    if (Items[i].GetHashCode() >= 0 && c.Equals(Items[i].Value, value))
                        return true;
            }
            return false;
        }

        /// <summary>
        /// Removes all keys and values from the <see cref="DictionarySerializable{TKey, TValue}"/>
        /// </summary>
        public void Clear()
        {
            Items.Clear();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private static bool IsCompatibleKey(object key)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            return (key is TKey);
        }

        /// <summary>
        /// Returns an <see cref="IEnumerator"/> object for the <see cref="DictionarySerializable{TKey, TValue}"/> object.
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Adds an element with the provided key and value to the <see cref="IDictionary"/> object.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        void IDictionary.Add(object key, object value)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            try
            {
                TKey tempKey = (TKey)key;

                try
                {
                    Add(tempKey, (TValue)value);
                }
                catch (InvalidCastException)
                {
                    throw new ArgumentException("TValue is the wrong type");
                }
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException("TKey is the wrong type");
            }
        }

        /// <summary>
        /// Determines whether the <see cref="IDictionary"/> object contains an element with the specified key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        bool IDictionary.Contains(object key)
        {
            if (IsCompatibleKey(key))
                return ContainsKey((TKey)key);

            return false;
        }

        /// <summary>
        /// Returns an <see cref="IDictionaryEnumerator"/> object for the <see cref="IDictionary"/> object.
        /// </summary>
        /// <returns></returns>
        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Removes the element with the specified key from the <see cref="IDictionary"/> object.
        /// </summary>
        /// <param name="key"></param>
        void IDictionary.Remove(object key)
        {
            if (IsCompatibleKey(key))
                Remove((TKey)key);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="array"></param>
        /// <param name="index"></param>
        void ICollection.CopyTo(Array array, int index)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Adds the specified key and value to the dictionary.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Add(TKey key, TValue value)
        {
            DataItem<TKey, TValue> item = new DataItem<TKey, TValue>(key, value);
            Items.Add(item);
        }

        /// <summary>
        /// Removes the value with the specified key from the <see cref="DictionarySerializable{TKey, TValue}"/>
        /// </summary>
        /// <param name="key"></param>
        public void Remove(TKey key)
        {
            DataItem<TKey, TValue> item = Items.Find(x => (x.Key.Equals(key)));
            Items.Remove(item);
        }

        /// <summary>
        /// Generates an object from its XML representation.
        /// </summary>
        /// <param name="reader"></param>
        public void ReadXml(XmlReader reader)
        {
            string xmlString = reader.ReadInnerXml();

            XmlSerializer xs = new XmlSerializer(typeof(List<DataItem<TKey, TValue>>));
            StringReader strReader = new StringReader(xmlString);

            Items = (List<DataItem<TKey, TValue>>)xs.Deserialize(strReader);

            strReader.Close();
        }

        /// <summary>
        /// Converts an object into its XML representation.
        /// </summary>
        /// <param name="writer"></param>
        public void WriteXml(XmlWriter writer)
        {
            // write the xml out to a raw string
            StringWriter strWriter = new StringWriter();
            XmlWriterSettings xmlWSettings = new XmlWriterSettings();
            xmlWSettings.OmitXmlDeclaration = true;
            xmlWSettings.ConformanceLevel = ConformanceLevel.Auto;
            xmlWSettings.Indent = true;

            XmlWriter xmlWriter = XmlWriter.Create(strWriter, xmlWSettings);
            XmlSerializer serializer = new XmlSerializer(typeof(List<DataItem<TKey, TValue>>));

            serializer.Serialize(xmlWriter, Items, null);

            // we need to read it back in so we can inject it as nodes
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.IgnoreWhitespace = true;
            XmlReader reader = XmlReader.Create(new StringReader(strWriter.ToString()), settings);

            writer.WriteNode(reader, true);
            strWriter.Close();
        }

        /// <summary>
        /// This method is reserved and should not be used. When implementing the <see cref="IXmlSerializable"/> interface,
        /// you should return null from this method, and instead, if specifying a custom schema is required, apply
        /// the <see cref="XmlSchemaProviderAttribute"/> to the class.
        /// </summary>
        /// <returns></returns>
        public XmlSchema GetSchema()
        {
            return (null);
        }
    } // public class DictionarySerializable<TKey, TValue> : IXmlSerializable, IDictionary
} // namespace TridentFramework.RPC
