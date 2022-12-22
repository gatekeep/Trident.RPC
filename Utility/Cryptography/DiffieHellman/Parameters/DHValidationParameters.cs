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

namespace TridentFramework.Cryptography.DiffieHellman.Parameters
{
    /// <summary>
    ///
    /// </summary>
    public class DHValidationParameters
    {
        private readonly byte[] seed;
        private readonly int counter;

        /*
        ** Properties
        */

        /// <summary>
        ///
        /// </summary>
		public int Counter
        {
            get { return counter; }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="DHValidationParameters"/> class.
        /// </summary>
        /// <param name="seed"></param>
        /// <param name="counter"></param>
		public DHValidationParameters(byte[] seed, int counter)
        {
            if (seed == null)
                throw new ArgumentNullException("seed");

            this.seed = (byte[])seed.Clone();
            this.counter = counter;
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
		public byte[] GetSeed()
        {
            return (byte[])seed.Clone();
        }

        /// <inheritdoc />
		public override bool Equals(object obj)
        {
            if (obj == this)
                return true;

            DHValidationParameters other = obj as DHValidationParameters;

            if (other == null)
                return false;

            return Equals(other);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static bool AreEqual(byte[] a, byte[] b)
        {
            if (a == b)
                return true;

            if (a == null || b == null)
                return false;

            return HaveSameContents(a, b);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static bool HaveSameContents(byte[] a, byte[] b)
        {
            int i = a.Length;
            if (i != b.Length)
                return false;
            while (i != 0)
            {
                --i;
                if (a[i] != b[i])
                    return false;
            }

            return true;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
		protected bool Equals(DHValidationParameters other)
        {
            return counter == other.counter && AreEqual(this.seed, other.seed);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private static int GetHashCode(byte[] data)
        {
            if (data == null)
                return 0;

            int i = data.Length;
            int hc = i + 1;

            while (--i >= 0)
            {
                hc *= 257;
                hc ^= data[i];
            }

            return hc;
        }

        /// <inheritdoc />
		public override int GetHashCode()
        {
            return counter.GetHashCode() ^ GetHashCode(seed);
        }
    } // public class DHValidationParameters
} // namespace TridentFramework.Cryptography.DiffieHellman.Parameters
