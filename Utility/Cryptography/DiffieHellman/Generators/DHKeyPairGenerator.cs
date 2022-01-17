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
    /// Diffie-Hellman key pair generator. This generates keys consistent for use in the
    /// MTI/A0 key agreement protocol as described in "Handbook of Applied Cryptography",
    /// Pages 516-519.
    /// </summary>
    public class DHKeyPairGenerator : IAsymmetricCipherKeyPairGenerator
    {
        private DHKeyGenerationParameters param;

        /*
        ** Methods
        */

        /// <summary>
        ///
        /// </summary>
        /// <param name="parameters"></param>
		public virtual void Init(KeyGenerationParameters parameters)
        {
            this.param = (DHKeyGenerationParameters)parameters;
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
		public virtual AsymmetricCipherKeyPair GenerateKeyPair()
        {
            DHKeyGeneratorHelper helper = DHKeyGeneratorHelper.Instance;
            DHParameters dhp = param.Parameters;

            BigInteger x = helper.CalculatePrivate(dhp, param.Random);
            BigInteger y = helper.CalculatePublic(dhp, x);

            return new AsymmetricCipherKeyPair(new DHPublicKeyParameters(y, dhp), new DHPrivateKeyParameters(x, dhp));
        }
    } // public class DHKeyPairGenerator : IAsymmetricCipherKeyPairGenerator
} // namespace TridentFramework.Cryptography.DiffieHellman.Generators
