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
    public class DHPublicKeyParameters : DHKeyParameters
    {
        private readonly BigInteger y;

        /*
        ** Properties
        */

        /// <summary>
        ///
        /// </summary>
        public BigInteger Y
        {
            get { return y; }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="DHPublicKeyParameters"/> class.
        /// </summary>
        /// <param name="y"></param>
        /// <param name="parameters"></param>
		public DHPublicKeyParameters(BigInteger y, DHParameters parameters)
            : base(false, parameters)
        {
            if (y == null)
                throw new ArgumentNullException("y");
            this.y = y;
        }

        /// <inheritdoc />
		public override bool Equals(object obj)
        {
            if (obj == this)
                return true;

            DHPublicKeyParameters other = obj as DHPublicKeyParameters;

            if (other == null)
                return false;

            return Equals(other);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
		protected bool Equals(DHPublicKeyParameters other)
        {
            return y.Equals(other.y) && base.Equals(other);
        }

        /// <inheritdoc />
		public override int GetHashCode()
        {
            return y.GetHashCode() ^ base.GetHashCode();
        }
    } // public class DHPublicKeyParameters : DHKeyParameters
} // namespace TridentFramework.Cryptography.DiffieHellman.Parameters
