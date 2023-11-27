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
    public class DHParameters : ICipherParameters
    {
        private const int DefaultMinimumLength = 160;

        private readonly BigInteger p, g, q, j;
        private readonly int m, l;
        private readonly DHValidationParameters validation;

        /*
        ** Properties
        */

        /// <summary>
        ///
        /// </summary>
        public BigInteger P
        {
            get { return p; }
        }

        /// <summary>
        ///
        /// </summary>
        public BigInteger G
        {
            get { return g; }
        }

        /// <summary>
        ///
        /// </summary>
        public BigInteger Q
        {
            get { return q; }
        }

        /// <summary>
        ///
        /// </summary>
        public BigInteger J
        {
            get { return j; }
        }

        /// <summary>
        /// Get the minimum bit length of the private value.
        /// </summary>
        public int M
        {
            get { return m; }
        }

        /// <summary>
        /// Get the bit length of the private value.
        /// </summary>
        public int L
        {
            get { return l; }
        }

        /// <summary>
        ///
        /// </summary>
        public DHValidationParameters ValidationParameters
        {
            get { return validation; }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="DHParameters"/> class.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="g"></param>
		public DHParameters(BigInteger p, BigInteger g) : this(p, g, null, 0)
        {
            /* stub */
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DHParameters"/> class.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="g"></param>
        /// <param name="q"></param>
		public DHParameters(BigInteger p, BigInteger g, BigInteger q) : this(p, g, q, 0)
        {
            /* stub */
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DHParameters"/> class.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="g"></param>
        /// <param name="q"></param>
        /// <param name="l"></param>
		public DHParameters(BigInteger p, BigInteger g, BigInteger q, int l) : this(p, g, q, GetDefaultMParam(l), l, null, null)
        {
            /* stub */
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DHParameters"/> class.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="g"></param>
        /// <param name="q"></param>
        /// <param name="m"></param>
        /// <param name="l"></param>
        public DHParameters(BigInteger p, BigInteger g, BigInteger q, int m, int l) : this(p, g, q, m, l, null, null)
        {
            /* stub */
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DHParameters"/> class.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="g"></param>
        /// <param name="q"></param>
        /// <param name="j"></param>
        /// <param name="validation"></param>
        public DHParameters(BigInteger p, BigInteger g, BigInteger q, BigInteger j, DHValidationParameters validation) :
            this(p, g, q, DefaultMinimumLength, 0, j, validation)
        {
            /* stub */
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DHParameters"/> class.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="g"></param>
        /// <param name="q"></param>
        /// <param name="m"></param>
        /// <param name="l"></param>
        /// <param name="j"></param>
        /// <param name="validation"></param>
        public DHParameters(BigInteger p, BigInteger g, BigInteger q, int m, int l, BigInteger j, DHValidationParameters validation)
        {
            if (p == null)
                throw new ArgumentNullException("p");
            if (g == null)
                throw new ArgumentNullException("g");
            if (!p.TestBit(0))
                throw new ArgumentException("field must be an odd prime", "p");
            if (g.CompareTo(BigInteger.Two) < 0
                || g.CompareTo(p.Subtract(BigInteger.Two)) > 0)
                throw new ArgumentException("generator must in the range [2, p - 2]", "g");
            if (q != null && q.BitLength >= p.BitLength)
                throw new ArgumentException("q too big to be a factor of (p-1)", "q");
            if (m >= p.BitLength)
                throw new ArgumentException("m value must be < bitlength of p", "m");
            if (l != 0)
            {
                // TODO Check this against the Java version, which has 'l > p.BitLength' here
                if (l >= p.BitLength)
                    throw new ArgumentException("when l value specified, it must be less than bitlength(p)", "l");
                if (l < m)
                    throw new ArgumentException("when l value specified, it may not be less than m value", "l");
            }
            if (j != null && j.CompareTo(BigInteger.Two) < 0)
                throw new ArgumentException("subgroup factor must be >= 2", "j");

            // TODO If q, j both provided, validate p = jq + 1 ?

            this.p = p;
            this.g = g;
            this.q = q;
            this.m = m;
            this.l = l;
            this.j = j;
            this.validation = validation;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="lParam"></param>
        /// <returns></returns>
		private static int GetDefaultMParam(int lParam)
        {
            if (lParam == 0)
                return DefaultMinimumLength;

            return System.Math.Min(lParam, DefaultMinimumLength);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (obj == this)
                return true;

            DHParameters other = obj as DHParameters;

            if (other == null)
                return false;

            return Equals(other);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
		protected bool Equals(DHParameters other)
        {
            return p.Equals(other.p) && g.Equals(other.g) && Equals(q, other.q);
        }

        /// <inheritdoc />
		public override int GetHashCode()
        {
            int hc = p.GetHashCode() ^ g.GetHashCode();
            if (q != null)
                hc ^= q.GetHashCode();

            return hc;
        }
    } // public class DHParameters : ICipherParameters
} // namespace TridentFramework.Cryptography.DiffieHellman.Parameters
