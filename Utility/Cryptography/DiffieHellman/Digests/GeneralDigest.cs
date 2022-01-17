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

namespace TridentFramework.Cryptography.DiffieHellman.Digests
{
    /// <summary>
    /// Base implementation of MD4 family style digest as outlined in
    /// "Handbook of Applied Cryptography", pages 344 - 347.
    /// </summary>
    public abstract class GeneralDigest : IDigest
    {
        private const int BYTE_LENGTH = 64;

        private byte[] xBuf;
        private int xBufOff;

        private long byteCount;

        /*
        ** Properties
        */

        /// <summary>
        /// Gets the algorithm name.
        /// </summary>
        public abstract string AlgorithmName
        {
            get;
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="GeneralDigest"/> class.
        /// </summary>
        internal GeneralDigest()
        {
            xBuf = new byte[4];
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GeneralDigest"/> class.
        /// </summary>
        /// <param name="t"></param>
        internal GeneralDigest(GeneralDigest t)
        {
            xBuf = new byte[t.xBuf.Length];
            CopyIn(t);
        }

        /// <summary>
        /// Return the size, in bytes, of the digest produced by this message digest.
        /// </summary>
        /// <returns></returns>
		public abstract int GetDigestSize();

        /// <summary>
        /// Return the size, in bytes, of the internal buffer used by this digest.
        /// </summary>
        /// <returns></returns>
		public int GetByteLength()
        {
            return BYTE_LENGTH;
        }

        /// <summary>
        /// Update the message digest with a single byte.
        /// </summary>
        /// <param name="input">Input byte to be entered.</param>
        public void Update(byte input)
        {
            xBuf[xBufOff++] = input;

            if (xBufOff == xBuf.Length)
            {
                ProcessWord(xBuf, 0);
                xBufOff = 0;
            }

            byteCount++;
        }

        /// <summary>
        /// Update the message digest with a block of bytes.
        /// </summary>
        /// <param name="input">Byte array containing the data.</param>
        /// <param name="offset">Offset into the byte array where the data starts.</param>
        /// <param name="length">Length of the data.</param>
        public void BlockUpdate(byte[] input, int offset, int length)
        {
            length = System.Math.Max(0, length);

            // fill the current word
            int i = 0;
            if (xBufOff != 0)
            {
                while (i < length)
                {
                    xBuf[xBufOff++] = input[offset + i++];
                    if (xBufOff == 4)
                    {
                        ProcessWord(xBuf, 0);
                        xBufOff = 0;
                        break;
                    }
                }
            }

            // process whole words.
            int limit = ((length - i) & ~3) + i;
            for (; i < limit; i += 4)
                ProcessWord(input, offset + i);

            // load in the remainder.
            while (i < length)
                xBuf[xBufOff++] = input[offset + i++];

            byteCount += length;
        }

        /// <summary>
        /// Close the digest, producing the final digest value. The DoFinal
        /// call leaves the digest reset.
        /// </summary>
        /// <param name="output">Array the digest is to be copied into.</param>
        /// <param name="offset">Offset into the out array the digest is to start at.</param>
        public abstract int DoFinal(byte[] output, int offset);

        /// <summary>
        /// Reset the digest back to it's initial state.
        /// </summary>
        public virtual void Reset()
        {
            byteCount = 0;
            xBufOff = 0;
            Array.Clear(xBuf, 0, xBuf.Length);
        }

        /// <summary>
        ///
        /// </summary>
        public void Finish()
        {
            long bitLength = (byteCount << 3);

            // add the pad bytes.
            Update((byte)128);

            while (xBufOff != 0)
                Update((byte)0);
            ProcessLength(bitLength);
            ProcessBlock();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="t"></param>
		protected void CopyIn(GeneralDigest t)
        {
            Array.Copy(t.xBuf, 0, xBuf, 0, t.xBuf.Length);

            xBufOff = t.xBufOff;
            byteCount = t.byteCount;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="input"></param>
        /// <param name="offset"></param>
		internal abstract void ProcessWord(byte[] input, int offset);

        /// <summary>
        ///
        /// </summary>
        /// <param name="bitLength"></param>
        internal abstract void ProcessLength(long bitLength);

        /// <summary>
        ///
        /// </summary>
        internal abstract void ProcessBlock();
    } // public abstract class GeneralDigest : IDigest
} // namespace TridentFramework.Cryptography.DiffieHellman.Digests
