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
    internal class DHKeyGeneratorHelper
    {
        internal static readonly DHKeyGeneratorHelper Instance = new DHKeyGeneratorHelper();

        /*
        ** Methods
        */

        /// <summary>
        ///
        /// </summary>
        /// <param name="dhParams"></param>
        /// <param name="random"></param>
        /// <returns></returns>
        internal BigInteger CalculatePrivate(DHParameters dhParams, SecureRandom random)
        {
            int limit = dhParams.L;

            if (limit != 0)
            {
                int minWeight = limit >> 2;
                for (; ; )
                {
                    BigInteger x = new BigInteger(limit, random).SetBit(limit - 1);
                    if (BigInteger.GetNafWeight(x) >= minWeight)
                        return x;
                }
            }

            BigInteger min = BigInteger.Two;
            int m = dhParams.M;
            if (m != 0)
                min = BigInteger.One.ShiftLeft(m - 1);

            BigInteger q = dhParams.Q;
            if (q == null)
                q = dhParams.P;

            BigInteger max = q.Subtract(BigInteger.Two);

            {
                int minWeight = max.BitLength >> 2;
                for (; ; )
                {
                    BigInteger x = BigInteger.CreateRandomInRange(min, max, random);
                    if (BigInteger.GetNafWeight(x) >= minWeight)
                        return x;
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="dhParams"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        internal BigInteger CalculatePublic(DHParameters dhParams, BigInteger x)
        {
            return dhParams.G.ModPow(x, dhParams.P);
        }
    } // internal class DHKeyGeneratorHelper
} // namespace TridentFramework.Cryptography.DiffieHellman.Generators
