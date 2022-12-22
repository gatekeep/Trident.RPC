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

using TridentFramework.Cryptography.DiffieHellman.Digests;

namespace TridentFramework.Cryptography.DiffieHellman
{
    /// <summary>
    /// Random generation based on the digest with counter. Calling <see cref="AddSeedMaterial(byte[])"/> will
    /// always increase the entropy of the hash.
    /// </summary>
    /// <remarks>
    /// Internal access to the digest is synchronized so a single one of these can be shared.
    /// </remarks>
    public class DigestRandomGenerator : IRandomGenerator
    {
        private const long CYCLE_COUNT = 10;

        private long stateCounter;
        private long seedCounter;
        private IDigest digest;
        private byte[] state;
        private byte[] seed;

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="DigestRandomGenerator"/> class.
        /// </summary>
        /// <param name="digest"></param>
		public DigestRandomGenerator(IDigest digest)
        {
            this.digest = digest;

            this.seed = new byte[digest.GetDigestSize()];
            this.seedCounter = 1;

            this.state = new byte[digest.GetDigestSize()];
            this.stateCounter = 1;
        }

        /// <summary>
        /// Add more seed material to the generator.
        /// </summary>
        /// <param name="inSeed">A byte array to be mixed into the generator's state.</param>
        public void AddSeedMaterial(byte[] inSeed)
        {
            lock (this)
            {
                DigestUpdate(inSeed);
                DigestUpdate(seed);
                DigestDoFinal(seed);
            }
        }

        /// <summary>
        /// Add more seed material to the generator.
        /// </summary>
        /// <param name="rSeed">A long value to be mixed into the generator's state.</param>
        public void AddSeedMaterial(long rSeed)
        {
            lock (this)
            {
                DigestAddCounter(rSeed);
                DigestUpdate(seed);
                DigestDoFinal(seed);
            }
        }

        /// <summary>
        /// Fill byte array with random values.
        /// </summary>
        /// <param name="bytes">Array to be filled.</param>
        public void NextBytes(byte[] bytes)
        {
            NextBytes(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// Fill byte array with random values.
        /// </summary>
        /// <param name="bytes">Array to receive bytes.</param>
        /// <param name="start">Index to start filling at.</param>
        /// <param name="len">Length of segment to fill.</param>
        public void NextBytes(byte[] bytes, int start, int len)
        {
            lock (this)
            {
                int stateOff = 0;

                GenerateState();

                int end = start + len;
                for (int i = start; i < end; ++i)
                {
                    if (stateOff == state.Length)
                    {
                        GenerateState();
                        stateOff = 0;
                    }
                    bytes[i] = state[stateOff++];
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
		private void CycleSeed()
        {
            DigestUpdate(seed);
            DigestAddCounter(seedCounter++);
            DigestDoFinal(seed);
        }

        /// <summary>
        ///
        /// </summary>
		private void GenerateState()
        {
            DigestAddCounter(stateCounter++);
            DigestUpdate(state);
            DigestUpdate(seed);
            DigestDoFinal(state);

            if ((stateCounter % CYCLE_COUNT) == 0)
            {
                CycleSeed();
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="seedVal"></param>
		private void DigestAddCounter(long seedVal)
        {
            byte[] bytes = new byte[8];
            BitOrder.UInt64_To_LE((ulong)seedVal, bytes);
            digest.BlockUpdate(bytes, 0, bytes.Length);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="inSeed"></param>
        private void DigestUpdate(byte[] inSeed)
        {
            digest.BlockUpdate(inSeed, 0, inSeed.Length);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="result"></param>
		private void DigestDoFinal(byte[] result)
        {
            digest.DoFinal(result, 0);
        }
    } // public class DigestRandomGenerator : IRandomGenerator
} // namespace TridentFramework.Cryptography.DiffieHellman
