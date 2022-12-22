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
    /// Interface that a message digest conforms to.
    /// </summary>
    public interface IDigest
    {
        /*
        ** Properties
        */

        /// <summary>
        /// Gets the algorithm name.
        /// </summary>
        string AlgorithmName { get; }

        /*
        ** Methods
        */

        /// <summary>
        /// Return the size, in bytes, of the digest produced by this message digest.
        /// </summary>
        /// <returns></returns>
        int GetDigestSize();

        /// <summary>
        /// Return the size, in bytes, of the internal buffer used by this digest.
        /// </summary>
        /// <returns></returns>
        int GetByteLength();

        /// <summary>
        /// Update the message digest with a single byte.
        /// </summary>
        /// <param name="input">Input byte to be entered.</param>
        void Update(byte input);

        /// <summary>
        /// Update the message digest with a block of bytes.
        /// </summary>
        /// <param name="input">Byte array containing the data.</param>
        /// <param name="offset">Offset into the byte array where the data starts.</param>
        /// <param name="length">Length of the data.</param>
        void BlockUpdate(byte[] input, int offset, int length);

        /// <summary>
        /// Close the digest, producing the final digest value. The DoFinal
        /// call leaves the digest reset.
        /// </summary>
        /// <param name="output">Array the digest is to be copied into.</param>
        /// <param name="offset">Offset into the out array the digest is to start at.</param>
        int DoFinal(byte[] output, int offset);

        /// <summary>
        /// Reset the digest back to it's initial state.
        /// </summary>
        void Reset();
    } // public interface IDigest
} // namespace TridentFramework.Cryptography.DiffieHellman.Digests
