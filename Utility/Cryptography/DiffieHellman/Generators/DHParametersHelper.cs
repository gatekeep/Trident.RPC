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

namespace TridentFramework.Cryptography.DiffieHellman.Generators
{
    /// <summary>
    ///
    /// </summary>
    internal class DHParametersHelper
    {
        private static readonly BigInteger Six = BigInteger.ValueOf(6);

        private static readonly int[][] primeLists = BigInteger.primeLists;
        private static readonly int[] primeProducts = BigInteger.primeProducts;
        private static readonly BigInteger[] BigPrimeProducts = ConstructBigPrimeProducts(primeProducts);

        /*
        ** Methods
        */

        /// <summary>
        ///
        /// </summary>
        /// <param name="primeProducts"></param>
        /// <returns></returns>
        private static BigInteger[] ConstructBigPrimeProducts(int[] primeProducts)
        {
            BigInteger[] bpp = new BigInteger[primeProducts.Length];
            for (int i = 0; i < bpp.Length; ++i)
                bpp[i] = BigInteger.ValueOf(primeProducts[i]);

            return bpp;
        }

        /// <summary>
        /// Finds a pair of prime BigInteger's {p, q: p = 2q + 1}
        /// </summary>
        /// <remarks>(see: Handbook of Applied Cryptography 4.86)</remarks>
        /// <param name="size"></param>
        /// <param name="certainty"></param>
        /// <param name="random"></param>
        /// <returns></returns>
        internal static BigInteger[] GenerateSafePrimes(int size, int certainty, SecureRandom random)
        {
            BigInteger p, q;
            int qLength = size - 1;
            int minWeight = size >> 2;

            if (size <= 32)
            {
                for (; ; )
                {
                    q = new BigInteger(qLength, 2, random);

                    p = q.ShiftLeft(1).Add(BigInteger.One);

                    if (!p.IsProbablePrime(certainty, true))
                        continue;

                    if (certainty > 2 && !q.IsProbablePrime(certainty, true))
                        continue;

                    break;
                }
            }
            else
            {
                // Note: Modified from Java version for speed
                for (; ; )
                {
                    q = new BigInteger(qLength, 0, random);

                retry:
                    for (int i = 0; i < primeLists.Length; ++i)
                    {
                        int test = q.Remainder(BigPrimeProducts[i]).IntValue;

                        if (i == 0)
                        {
                            int rem3 = test % 3;
                            if (rem3 != 2)
                            {
                                int diff = 2 * rem3 + 2;
                                q = q.Add(BigInteger.ValueOf(diff));
                                test = (test + diff) % primeProducts[i];
                            }
                        }

                        int[] primeList = primeLists[i];
                        for (int j = 0; j < primeList.Length; ++j)
                        {
                            int prime = primeList[j];
                            int qRem = test % prime;
                            if (qRem == 0 || qRem == (prime >> 1))
                            {
                                q = q.Add(Six);
                                goto retry;
                            }
                        }
                    }

                    if (q.BitLength != qLength)
                        continue;

                    if (!q.RabinMillerTest(2, random, true))
                        continue;

                    p = q.ShiftLeft(1).Add(BigInteger.One);

                    if (!p.RabinMillerTest(certainty, random, true))
                        continue;

                    if (certainty > 2 && !q.RabinMillerTest(certainty - 2, random, true))
                        continue;

                    /**
                     * Require a minimum weight of the NAF representation, since low-weight primes may be
                     * weak against a version of the number-field-sieve for the discrete-logarithm-problem.
                     *
                     * See "The number field sieve for integers of low weight", Oliver Schirokauer.
                     */
                    if (BigInteger.GetNafWeight(p) < minWeight)
                        continue;

                    break;
                }
            }

            return new BigInteger[] { p, q };
        }

        /// <summary>
        /// Select a high order element of the multiplicative group Zp*
        /// </summary>
        /// <remarks>p and q must be s.t. p = 2*q + 1, where p and q are prime (see generateSafePrimes)</remarks>
        /// <param name="p"></param>
        /// <param name="q"></param>
        /// <param name="random"></param>
        /// <returns></returns>
        internal static BigInteger SelectGenerator(BigInteger p, BigInteger q, SecureRandom random)
        {
            BigInteger pMinusTwo = p.Subtract(BigInteger.Two);
            BigInteger g;

            /*
             * RFC 2631 2.2.1.2 (and see: Handbook of Applied Cryptography 4.81)
             */
            do
            {
                BigInteger h = BigInteger.CreateRandomInRange(BigInteger.Two, pMinusTwo, random);
                g = h.ModPow(BigInteger.Two, p);
            }
            while (g.Equals(BigInteger.One));

            return g;
        }
    } // internal class DHParametersHelper
} // namespace TridentFramework.Cryptography.DiffieHellman.Generators
