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

namespace TridentFramework.Cryptography.DiffieHellman
{
    /// <summary>
    /// Holding class for public/private parameter pairs.
    /// </summary>
    public class AsymmetricCipherKeyPair
    {
        private readonly AsymmetricKeyParameter publicParameter;
        private readonly AsymmetricKeyParameter privateParameter;

        /*
        ** Properties
        */

        /// <summary>
        /// Get the public key parameters.
        /// </summary>
        public AsymmetricKeyParameter Public
        {
            get { return publicParameter; }
        }

        /// <summary>
        /// Get the private key parameters.
        /// </summary>
        public AsymmetricKeyParameter Private
        {
            get { return privateParameter; }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="AsymmetricCipherKeyPair"/> class.
        /// </summary>
        /// <param name="publicParameter"></param>
        /// <param name="privateParameter"></param>
        public AsymmetricCipherKeyPair(AsymmetricKeyParameter publicParameter, AsymmetricKeyParameter privateParameter)
        {
            if (publicParameter.IsPrivate)
                throw new ArgumentException("Expected a public key", "publicParameter");
            if (!privateParameter.IsPrivate)
                throw new ArgumentException("Expected a private key", "privateParameter");

            this.publicParameter = publicParameter;
            this.privateParameter = privateParameter;
        }
    } // public class AsymmetricCipherKeyPair
} // namespace TridentFramework.Cryptography.DiffieHellman
