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

namespace TridentFramework.Cryptography.DiffieHellman.Digests
{
    /// <summary>
    /// Implementation of SHA-1 as outlined in "Handbook of Applied Cryptography", pages 346 - 349.
    /// </summary>
    /// <remarks>
    /// It is interesting to ponder why the, apart from the extra IV, the other difference here from MD5
    /// is the "endianness" of the word processing!
    /// </remarks>
    public class SHA1Digest : GeneralDigest
    {
        private const int DigestLength = 20;

        private uint H1, H2, H3, H4, H5;

        private uint[] X = new uint[80];
        private int xOff;

        // Additive constants
        private const uint Y1 = 0x5a827999;
        private const uint Y2 = 0x6ed9eba1;
        private const uint Y3 = 0x8f1bbcdc;
        private const uint Y4 = 0xca62c1d6;

        /*
        ** Properties
        */

        /// <inheritdoc />
        public override string AlgorithmName
        {
            get { return "SHA-1"; }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="SHA1Digest"/> class.
        /// </summary>
        public SHA1Digest()
        {
            Reset();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SHA1Digest"/> class.
        /// </summary>
        /// <param name="t"></param>
        public SHA1Digest(SHA1Digest t) : base(t)
        {
            CopyIn(t);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="t"></param>
		private void CopyIn(SHA1Digest t)
        {
            base.CopyIn(t);

            H1 = t.H1;
            H2 = t.H2;
            H3 = t.H3;
            H4 = t.H4;
            H5 = t.H5;

            Array.Copy(t.X, 0, X, 0, t.X.Length);
            xOff = t.xOff;
        }

        /// <inheritdoc />
        public override int GetDigestSize()
        {
            return DigestLength;
        }

        /// <inheritdoc />
        internal override void ProcessWord(byte[] input, int offset)
        {
            X[xOff] = BitOrder.BE_To_UInt32(input, offset);
            if (++xOff == 16)
                ProcessBlock();
        }

        /// <inheritdoc />
        internal override void ProcessLength(long bitLength)
        {
            if (xOff > 14)
                ProcessBlock();

            X[14] = (uint)((ulong)bitLength >> 32);
            X[15] = (uint)((ulong)bitLength);
        }

        /// <inheritdoc />
        public override int DoFinal(byte[] output, int offset)
        {
            Finish();

            BitOrder.UInt32_To_BE(H1, output, offset);
            BitOrder.UInt32_To_BE(H2, output, offset + 4);
            BitOrder.UInt32_To_BE(H3, output, offset + 8);
            BitOrder.UInt32_To_BE(H4, output, offset + 12);
            BitOrder.UInt32_To_BE(H5, output, offset + 16);

            Reset();

            return DigestLength;
        }

        /// <inheritdoc />
        public override void Reset()
        {
            base.Reset();

            H1 = 0x67452301;
            H2 = 0xefcdab89;
            H3 = 0x98badcfe;
            H4 = 0x10325476;
            H5 = 0xc3d2e1f0;

            xOff = 0;
            Array.Clear(X, 0, X.Length);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="u"></param>
        /// <param name="v"></param>
        /// <param name="w"></param>
        /// <returns></returns>
        private static uint F(uint u, uint v, uint w)
        {
            return (u & v) | (~u & w);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="u"></param>
        /// <param name="v"></param>
        /// <param name="w"></param>
        /// <returns></returns>
        private static uint H(uint u, uint v, uint w)
        {
            return u ^ v ^ w;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="u"></param>
        /// <param name="v"></param>
        /// <param name="w"></param>
        /// <returns></returns>
        private static uint G(uint u, uint v, uint w)
        {
            return (u & v) | (u & w) | (v & w);
        }

        /// <inheritdoc />
        internal override void ProcessBlock()
        {
            // expand 16 word block into 80 word block.
            for (int i = 16; i < 80; i++)
            {
                uint t = X[i - 3] ^ X[i - 8] ^ X[i - 14] ^ X[i - 16];
                X[i] = t << 1 | t >> 31;
            }

            // set up working variables.
            uint A = H1;
            uint B = H2;
            uint C = H3;
            uint D = H4;
            uint E = H5;

            // round 1
            int idx = 0;
            for (int j = 0; j < 4; j++)
            {
                // E = rotateLeft(A, 5) + F(B, C, D) + E + X[idx++] + Y1
                // B = rotateLeft(B, 30)
                E += (A << 5 | (A >> 27)) + F(B, C, D) + X[idx++] + Y1;
                B = B << 30 | (B >> 2);

                D += (E << 5 | (E >> 27)) + F(A, B, C) + X[idx++] + Y1;
                A = A << 30 | (A >> 2);

                C += (D << 5 | (D >> 27)) + F(E, A, B) + X[idx++] + Y1;
                E = E << 30 | (E >> 2);

                B += (C << 5 | (C >> 27)) + F(D, E, A) + X[idx++] + Y1;
                D = D << 30 | (D >> 2);

                A += (B << 5 | (B >> 27)) + F(C, D, E) + X[idx++] + Y1;
                C = C << 30 | (C >> 2);
            }

            // round 2
            for (int j = 0; j < 4; j++)
            {
                // E = rotateLeft(A, 5) + H(B, C, D) + E + X[idx++] + Y2
                // B = rotateLeft(B, 30)
                E += (A << 5 | (A >> 27)) + H(B, C, D) + X[idx++] + Y2;
                B = B << 30 | (B >> 2);

                D += (E << 5 | (E >> 27)) + H(A, B, C) + X[idx++] + Y2;
                A = A << 30 | (A >> 2);

                C += (D << 5 | (D >> 27)) + H(E, A, B) + X[idx++] + Y2;
                E = E << 30 | (E >> 2);

                B += (C << 5 | (C >> 27)) + H(D, E, A) + X[idx++] + Y2;
                D = D << 30 | (D >> 2);

                A += (B << 5 | (B >> 27)) + H(C, D, E) + X[idx++] + Y2;
                C = C << 30 | (C >> 2);
            }

            // round 3
            for (int j = 0; j < 4; j++)
            {
                // E = rotateLeft(A, 5) + G(B, C, D) + E + X[idx++] + Y3
                // B = rotateLeft(B, 30)
                E += (A << 5 | (A >> 27)) + G(B, C, D) + X[idx++] + Y3;
                B = B << 30 | (B >> 2);

                D += (E << 5 | (E >> 27)) + G(A, B, C) + X[idx++] + Y3;
                A = A << 30 | (A >> 2);

                C += (D << 5 | (D >> 27)) + G(E, A, B) + X[idx++] + Y3;
                E = E << 30 | (E >> 2);

                B += (C << 5 | (C >> 27)) + G(D, E, A) + X[idx++] + Y3;
                D = D << 30 | (D >> 2);

                A += (B << 5 | (B >> 27)) + G(C, D, E) + X[idx++] + Y3;
                C = C << 30 | (C >> 2);
            }

            // round 4
            for (int j = 0; j < 4; j++)
            {
                // E = rotateLeft(A, 5) + H(B, C, D) + E + X[idx++] + Y4
                // B = rotateLeft(B, 30)
                E += (A << 5 | (A >> 27)) + H(B, C, D) + X[idx++] + Y4;
                B = B << 30 | (B >> 2);

                D += (E << 5 | (E >> 27)) + H(A, B, C) + X[idx++] + Y4;
                A = A << 30 | (A >> 2);

                C += (D << 5 | (D >> 27)) + H(E, A, B) + X[idx++] + Y4;
                E = E << 30 | (E >> 2);

                B += (C << 5 | (C >> 27)) + H(D, E, A) + X[idx++] + Y4;
                D = D << 30 | (D >> 2);

                A += (B << 5 | (B >> 27)) + H(C, D, E) + X[idx++] + Y4;
                C = C << 30 | (C >> 2);
            }

            H1 += A;
            H2 += B;
            H3 += C;
            H4 += D;
            H5 += E;

            // reset start of the buffer.
            xOff = 0;
            Array.Clear(X, 0, 16);
        }
    } // public class SHA1Digest : GeneralDigest
} // namespace TridentFramework.Cryptography.DiffieHellman.Digests
