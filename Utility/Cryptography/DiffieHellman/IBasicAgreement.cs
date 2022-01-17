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

namespace TridentFramework.Cryptography.DiffieHellman
{
    /// <summary>
    /// The basic interface that basic Diffie-Hellman implementations conforms to.
    /// </summary>
    public interface IBasicAgreement
    {
        /*
        ** Methods
        */

        /// <summary>
        /// Initialize the agreement engine.
        /// </summary>
        /// <param name="parameters"></param>
        void Init(ICipherParameters parameters);

        /// <summary>
        /// Return the field size for the agreement algorithm in bytes.
        /// </summary>
        /// <returns></returns>
        int GetFieldSize();

        /// <summary>
        /// Given a public key from a given party calculate the next
        /// message in the agreement sequence.
        /// </summary>
        /// <param name="pubKey"></param>
        /// <returns></returns>
        BigInteger CalculateAgreement(ICipherParameters pubKey);
    } // public interface IBasicAgreement
} // namespace TridentFramework.Cryptography.DiffieHellman
