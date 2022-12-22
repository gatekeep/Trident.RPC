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

namespace TridentFramework.Cryptography.DiffieHellman
{
    /// <summary>
    ///
    /// </summary>
    internal sealed class BitOrder
    {
        /*
        ** Methods
        */

        /// <summary>
        ///
        /// </summary>
        /// <param name="n"></param>
        /// <param name="bs"></param>
        /// <param name="off"></param>
        internal static void UInt32_To_BE(uint n, byte[] bs, int off)
        {
            bs[off] = (byte)(n >> 24);
            bs[off + 1] = (byte)(n >> 16);
            bs[off + 2] = (byte)(n >> 8);
            bs[off + 3] = (byte)(n);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="bs"></param>
        /// <param name="off"></param>
        /// <returns></returns>
        internal static uint BE_To_UInt32(byte[] bs, int off)
        {
            return (uint)bs[off] << 24 | (uint)bs[off + 1] << 16 | (uint)bs[off + 2] << 8 | (uint)bs[off + 3];
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="n"></param>
        /// <param name="bs"></param>
        internal static void UInt32_To_LE(uint n, byte[] bs)
        {
            bs[0] = (byte)(n);
            bs[1] = (byte)(n >> 8);
            bs[2] = (byte)(n >> 16);
            bs[3] = (byte)(n >> 24);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="n"></param>
        /// <param name="bs"></param>
        /// <param name="off"></param>
        internal static void UInt32_To_LE(uint n, byte[] bs, int off)
        {
            bs[off] = (byte)(n);
            bs[off + 1] = (byte)(n >> 8);
            bs[off + 2] = (byte)(n >> 16);
            bs[off + 3] = (byte)(n >> 24);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="n"></param>
        /// <param name="bs"></param>
        internal static void UInt64_To_LE(ulong n, byte[] bs)
        {
            UInt32_To_LE((uint)(n), bs);
            UInt32_To_LE((uint)(n >> 32), bs, 4);
        }
    }
} // namespace TridentFramework.Cryptography.DiffieHellman
