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

using TridentFramework.Cryptography.DiffieHellman.Parameters;

namespace TridentFramework.Cryptography.DiffieHellman.Generators
{
    /// <summary>
    ///
    /// </summary>
    public class DHParametersGenerator
    {
        private int size;
        private int certainty;
        private SecureRandom random;

        /*
        ** Methods
        */

        /// <summary>
        ///
        /// </summary>
        /// <param name="size"></param>
        /// <param name="certainty"></param>
        /// <param name="random"></param>
        public virtual void Init(int size, int certainty, SecureRandom random)
        {
            this.size = size;
            this.certainty = certainty;
            this.random = random;
        }

        /// <summary>
        /// Generates the p and g values from the given parameters,
        /// returning the <see cref="DHParameters"/> object.
        /// </summary>
        public virtual DHParameters GenerateParameters()
        {
            // find a safe prime p where p = 2*q + 1, where p and q are prime.
            BigInteger[] safePrimes = DHParametersHelper.GenerateSafePrimes(size, certainty, random);

            BigInteger p = safePrimes[0];
            BigInteger q = safePrimes[1];
            BigInteger g = DHParametersHelper.SelectGenerator(p, q, random);

            return new DHParameters(p, g, q, BigInteger.Two, null);
        }
    } // public class DHParametersGenerator
} // namespace TridentFramework.Cryptography.DiffieHellman.Generators
