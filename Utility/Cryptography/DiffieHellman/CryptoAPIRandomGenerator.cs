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
/*
 * Based on code from Bouncy Castle C# API
 * Copyright (c) 2000-2017 The Legion of the Bouncy Castle Inc. (http://www.bouncycastle.org)
 * Licensed under MIT X11 License.
 */

using System;
using System.Security.Cryptography;

namespace TridentFramework.Cryptography.DiffieHellman
{
    /// <summary>
    /// Uses Microsoft's RNGCryptoServiceProvider.
    /// </summary>
    public class CryptoAPIRandomGenerator : IRandomGenerator
    {
        private readonly RandomNumberGenerator rndProv;

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="CryptoAPIRandomGenerator"/> class.
        /// </summary>
        public CryptoAPIRandomGenerator() : this(new RNGCryptoServiceProvider())
        {
            /* stub */
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CryptoAPIRandomGenerator"/> class.
        /// </summary>
        /// <param name="rng"></param>
        public CryptoAPIRandomGenerator(RandomNumberGenerator rng)
        {
            this.rndProv = rng;
        }

        /// <summary>
        /// Add more seed material to the generator.
        /// </summary>
        /// <param name="seed">A byte array to be mixed into the generator's state.</param>
        public virtual void AddSeedMaterial(byte[] seed)
        {
            // We don't care about the seed
        }

        /// <summary>
        /// Add more seed material to the generator.
        /// </summary>
        /// <param name="seed">A long value to be mixed into the generator's state.</param>
        public virtual void AddSeedMaterial(long seed)
        {
            // We don't care about the seed
        }

        /// <summary>
        /// Fill byte array with random values.
        /// </summary>
        /// <param name="bytes">Array to be filled.</param>
        public virtual void NextBytes(byte[] bytes)
        {
            rndProv.GetBytes(bytes);
        }

        /// <summary>
        /// Fill byte array with random values.
        /// </summary>
        /// <param name="bytes">Array to receive bytes.</param>
        /// <param name="start">Index to start filling at.</param>
        /// <param name="len">Length of segment to fill.</param>
        public virtual void NextBytes(byte[] bytes, int start, int len)
        {
            if (start < 0)
                throw new ArgumentException("Start offset cannot be negative", "start");
            if (bytes.Length < (start + len))
                throw new ArgumentException("Byte array too small for requested offset and length");

            if (bytes.Length == len && start == 0)
            {
                NextBytes(bytes);
            }
            else
            {
                byte[] tmpBuf = new byte[len];
                NextBytes(tmpBuf);
                Array.Copy(tmpBuf, 0, bytes, start, len);
            }
        }
    } // public class CryptoAPIRandomGenerator : IRandomGenerator
} // namespace TridentFramework.Cryptography.DiffieHellman
