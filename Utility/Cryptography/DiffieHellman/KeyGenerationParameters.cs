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
    /// Base class for parameters to key generators.
    /// </summary>
    public class KeyGenerationParameters
    {
        private SecureRandom random;
        private int strength;

        /*
        ** Properties
        */

        /// <summary>
        /// Gets the random source associated with this generator.
        /// </summary>
        public SecureRandom Random
        {
            get { return random; }
        }

        /// <summary>
        /// Gets the bit strength for keys produced by this generator,
        /// </summary>
        public int Strength
        {
            get { return strength; }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyGenerationParameters"/> class. Initialise
        /// the generator with a source of randomness and a strength(in bits).
        /// </summary>
        /// <param name="random"></param>
        /// <param name="strength"></param>
        public KeyGenerationParameters(SecureRandom random, int strength)
        {
            if (random == null)
                throw new ArgumentNullException("random");
            if (strength < 1)
                throw new ArgumentException("strength must be a positive value", "strength");

            this.random = random;
            this.strength = strength;
        }
    } // public class KeyGenerationParameters
} // namespace TridentFramework.Cryptography.DiffieHellman
