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
 * Based on code from Lidgren Network v3
 * Copyright (C) 2010 Michael Lidgren., All Rights Reserved.
 * Licensed under MIT License.
 */

using System;
using System.Text;

namespace TridentFramework.RPC.Net.Channel
{
    /// <summary>
    /// Fixed size vector of booleans
    /// </summary>
    public sealed class BitVector
    {
        private readonly int[] data;
        private int numBitsSet;

        /*
        ** Properties
        */

        /// <summary>
        /// Gets the bit/bool at the specified index
        /// </summary>
        /// <returns></returns>
        [System.Runtime.CompilerServices.IndexerName("Bit")]
        public bool this[int index]
        {
            get { return Get(index); }
            set { Set(index, value); }
        }

        /// <summary>
        /// Gets the number of bits/booleans stored in this vector
        /// </summary>
        public int Capacity
        {
            get;
            private set;
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="BitVector"/> class.
        /// </summary>
        /// <param name="bitsCapacity">Size of this vector</param>
        public BitVector(int bitsCapacity)
        {
            Capacity = bitsCapacity;
            data = new int[(bitsCapacity + 31) / 32];
        }

        /// <summary>
        /// Returns true if all bits/booleans are set to zero/false
        /// </summary>
        /// <returns>True, if vector is empty, otherwise false.</returns>
        public bool IsEmpty()
        {
            return numBitsSet == 0;
        }

        /// <summary>
        /// Returns the number of bits/booleans set to one/true
        /// </summary>
        /// <returns></returns>
        public int Count()
        {
            return numBitsSet;
        }

        /// <summary>
        /// Shift all bits one step down, cycling the first bit to the top
        /// </summary>
        public void RotateDown()
        {
            int lenMinusOne = data.Length - 1;

            int firstBit = data[0] & 1;
            for (int i = 0; i < lenMinusOne; i++)
                data[i] = ((data[i] >> 1) & ~(1 << 31)) | data[i + 1] << 31;

            int lastIndex = Capacity - 1 - (32 * lenMinusOne);

            // special handling of last int
            int cur = data[lenMinusOne];
            cur = cur >> 1;
            cur |= firstBit << lastIndex;

            data[lenMinusOne] = cur;
        }

        /// <summary>
        /// Gets the first (lowest) index set to true
        /// </summary>
        /// <returns></returns>
        public int GetFirstSetIndex()
        {
            int idx = 0;

            int data = this.data[0];
            while (data == 0)
            {
                idx++;
                data = this.data[idx];
            }

            int a = 0;
            while (((data >> a) & 1) == 0)
                a++;

            return (idx * 32) + a;
        }

        /// <summary>
        /// Gets the bit/bool at the specified index
        /// </summary>
        /// <returns></returns>
        public bool Get(int bitIndex)
        {
            NetworkException.Assert(bitIndex >= 0 && bitIndex < Capacity);

            return (data[bitIndex / 32] & (1 << (bitIndex % 32))) != 0;
        }

        /// <summary>
        /// Sets or clears the bit/bool at the specified index
        /// </summary>
        public void Set(int bitIndex, bool value)
        {
            NetworkException.Assert(bitIndex >= 0 && bitIndex < Capacity);

            int idx = bitIndex / 32;
            if (value)
            {
                if ((data[idx] & (1 << (bitIndex % 32))) == 0)
                    numBitsSet++;
                data[idx] |= 1 << (bitIndex % 32);
            }
            else
            {
                if ((data[idx] & (1 << (bitIndex % 32))) != 0)
                    numBitsSet--;
                data[idx] &= ~(1 << (bitIndex % 32));
            }
        }

        /// <summary>
        /// Sets all bits/booleans to zero/false
        /// </summary>
        public void Clear()
        {
            Array.Clear(data, 0, data.Length);
            numBitsSet = 0;
            NetworkException.Assert(this.IsEmpty());
        }

        /// <inheritdoc />
        public override string ToString()
        {
            StringBuilder bdr = new StringBuilder(Capacity + 2);
            bdr.Append('[');
            for (int i = 0; i < Capacity; i++)
                bdr.Append(Get(Capacity - i - 1) ? '1' : '0');
            bdr.Append(']');
            return bdr.ToString();
        }
    } // public sealed class BitVector
} // namespace TridentFramework.RPC.Net.Channel
