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
using System.Collections.Generic;
using System.Reflection;

namespace TridentFramework.RPC.Net.Message
{
    /// <summary>
    /// Base class for <see cref="IncomingMessage"/> and <see cref="OutgoingMessage"/>.
    /// </summary>
    public partial class MessageBuffer
    {
        /// <summary>
        /// Number of bytes to overallocate for each message to avoid resizing
        /// </summary>
        protected const int OverAllocateAmount = 4;

        private static readonly Dictionary<Type, MethodInfo> readMethods;
        private static readonly Dictionary<Type, MethodInfo> writeMethods;

        internal byte[] data;
        internal int bitLength;
        internal int readPosition;

        /*
        ** Properties
        */

        /// <summary>
        /// Gets or sets the internal data buffer
        /// </summary>
        public byte[] Data
        {
            get { return data; }
            set { data = value; }
        }

        /// <summary>
        /// Gets or sets the length of the used portion of the buffer in bytes
        /// </summary>
        public int LengthBytes
        {
            get { return ((bitLength + 7) >> 3); }
            set
            {
                bitLength = value * 8;
                InternalEnsureBufferSize(bitLength);
            }
        }

        /// <summary>
        /// Gets or sets the length of the used portion of the buffer in bits
        /// </summary>
        public int BitLength
        {
            get { return bitLength; }
            set
            {
                bitLength = value;
                InternalEnsureBufferSize(bitLength);
            }
        }

        /// <summary>
        /// Gets or sets the read position in the buffer, in bits (not bytes)
        /// </summary>
        public long Position
        {
            get { return (long)readPosition; }
            set { readPosition = (int)value; }
        }

        /// <summary>
        /// Gets the position in the buffer in bytes; note that the bits of the first returned byte may already have been read - check the Position property to make sure.
        /// </summary>
        public int PositionInBytes
        {
            get { return (int)(readPosition / 8); }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Static initializer for <see cref="MessageBuffer"/> class.
        /// </summary>
        static MessageBuffer()
        {
            readMethods = new Dictionary<Type, MethodInfo>();
            MethodInfo[] methods = typeof(IncomingMessage).GetMethods(BindingFlags.Instance | BindingFlags.Public);
            foreach (MethodInfo mi in methods)
            {
                if (mi.GetParameters().Length == 0 && mi.Name.StartsWith("Read", StringComparison.InvariantCulture) && mi.Name.Substring(4) == mi.ReturnType.Name)
                    readMethods[mi.ReturnType] = mi;
            }

            writeMethods = new Dictionary<Type, MethodInfo>();
            methods = typeof(OutgoingMessage).GetMethods(BindingFlags.Instance | BindingFlags.Public);
            foreach (MethodInfo mi in methods)
            {
                if (mi.Name.Equals("Write", StringComparison.InvariantCulture))
                {
                    ParameterInfo[] pis = mi.GetParameters();
                    if (pis.Length == 1)
                        writeMethods[pis[0].ParameterType] = mi;
                }
            }
        }
    } // public partial class MessageBuffer
} // namespace TridentFramework.RPC.Net.Message
