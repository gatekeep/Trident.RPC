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
using System.Threading;

using TridentFramework.Cryptography.DiffieHellman.Digests;

namespace TridentFramework.Cryptography.DiffieHellman
{
    /// <summary>
    ///
    /// </summary>
    public class SecureRandom : Random
    {
        private static long NanosecondsPerTick = 100L;
        private static long counter = NanoTime();

        protected readonly IRandomGenerator generator;

        private static readonly SecureRandom master = new SecureRandom(new CryptoAPIRandomGenerator());
        private static readonly double DoubleScale = System.Math.Pow(2.0, 64.0);

        /*
        ** Properties
        */

        /// <summary>
        ///
        /// </summary>
        private static SecureRandom Master
        {
            get { return master; }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="SecureRandom"/> class.
        /// </summary>
        public SecureRandom() : this(CreatePrng(false, true))
        {
            /* stub */
        }

        /// <summary>
        /// Use the specified instance of <see cref="IRandomGenerator"/> as random source.
        /// </summary>
        /// <remarks>
        /// This constructor performs no seeding of either the <see cref="IRandomGenerator"/> or the
        /// constructed <see cref="SecureRandom"/>. It is the responsibility of the client to provide
        /// proper seed material as necessary/appropriate for the given <see cref="IRandomGenerator"/>
        /// implementation.
        /// </remarks>
        /// <param name="generator">The source to generate all random bytes from.</param>
        public SecureRandom(IRandomGenerator generator)
            : base(0)
        {
            this.generator = generator;
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public static long NanoTime()
        {
            return DateTime.UtcNow.Ticks * NanosecondsPerTick;
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        private static long NextCounterValue()
        {
            return Interlocked.Increment(ref counter);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="useSHA1"></param>
        /// <param name="autoSeed"></param>
        /// <returns></returns>
        private static DigestRandomGenerator CreatePrng(bool useSHA1, bool autoSeed)
        {
            IDigest digest = null;
            if (useSHA1)
                digest = new SHA1Digest();
            else
                digest = new SHA256Digest();

            if (digest == null)
                return null;

            DigestRandomGenerator prng = new DigestRandomGenerator(digest);
            if (autoSeed)
            {
                prng.AddSeedMaterial(NextCounterValue());
                prng.AddSeedMaterial(GetNextBytes(Master, digest.GetDigestSize()));
            }

            return prng;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="secureRandom"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static byte[] GetNextBytes(SecureRandom secureRandom, int length)
        {
            byte[] result = new byte[length];
            secureRandom.NextBytes(result);
            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public virtual byte[] GenerateSeed(int length)
        {
            return GetNextBytes(Master, length);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="seed"></param>
        public virtual void SetSeed(byte[] seed)
        {
            generator.AddSeedMaterial(seed);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="seed"></param>
        public virtual void SetSeed(long seed)
        {
            generator.AddSeedMaterial(seed);
        }

        /// <summary>
        /// Returns a non-negative random integer.
        /// </summary>
        /// <returns></returns>
        public override int Next()
        {
            return NextInt() & int.MaxValue;
        }

        /// <summary>
        /// Returns a non-negative random integer that is less than the specified maximum.
        /// </summary>
        /// <param name="maxValue"></param>
        /// <returns></returns>
        public override int Next(int maxValue)
        {
            if (maxValue < 2)
            {
                if (maxValue < 0)
                    throw new ArgumentOutOfRangeException("maxValue", "cannot be negative");

                return 0;
            }

            // Test whether maxValue is a power of 2
            if ((maxValue & (maxValue - 1)) == 0)
                return NextInt() & (maxValue - 1);

            int bits, result;
            do
            {
                bits = NextInt() & int.MaxValue;
                result = bits % maxValue;
            }
            while (bits - result + (maxValue - 1) < 0); // Ignore results near overflow
            return result;
        }

        /// <summary>
        /// Returns a random integer that is within a specified range.
        /// </summary>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <returns></returns>
        public override int Next(int minValue, int maxValue)
        {
            if (maxValue <= minValue)
            {
                if (maxValue == minValue)
                    return minValue;

                throw new ArgumentException("maxValue cannot be less than minValue");
            }

            int diff = maxValue - minValue;
            if (diff > 0)
                return minValue + Next(diff);

            for (; ; )
            {
                int i = NextInt();
                if (i >= minValue && i < maxValue)
                    return i;
            }
        }

        /// <summary>
        /// Fills the elements of a specified array of bytes with random numbers.
        /// </summary>
        /// <param name="buf"></param>
        public override void NextBytes(byte[] buf)
        {
            generator.NextBytes(buf);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        public virtual void NextBytes(byte[] buffer, int offset, int length)
        {
            generator.NextBytes(buffer, offset, length);
        }

        /// <summary>
        /// Returns a random floating-point number that is greater than or equal to 0.0, and less than 1.0.
        /// </summary>
        /// <returns></returns>
        public override double NextDouble()
        {
            return Convert.ToDouble((ulong)NextLong()) / DoubleScale;
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public virtual int NextInt()
        {
            byte[] bytes = new byte[4];
            NextBytes(bytes);

            uint result = bytes[0];
            result <<= 8;
            result |= bytes[1];
            result <<= 8;
            result |= bytes[2];
            result <<= 8;
            result |= bytes[3];
            return (int)result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public virtual long NextLong()
        {
            return ((long)(uint)NextInt() << 32) | (long)(uint)NextInt();
        }
    } // public class SecureRandom : Random
} // namespace TridentFramework.Cryptography.DiffieHellman
