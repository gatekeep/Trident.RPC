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
 * Based on code from Lidgren Network v3
 * Copyright (C) 2010 Michael Lidgren., All Rights Reserved.
 * Licensed under MIT License.
 */

using System;
using System.Diagnostics;

namespace TridentFramework.RPC.Net.PeerConnection
{
    /// <summary>
    /// Time service
    /// </summary>
    public static class NetTime
    {
        private static readonly long timeInitialized = Stopwatch.GetTimestamp();
        private static readonly double dInvFreq = 1.0 / (double)Stopwatch.Frequency;

        /*
        ** Properties
        */

        /// <summary>
        /// Gets number of seconds since the application started
        /// </summary>
        public static double Now
        {
            get { return (double)(Stopwatch.GetTimestamp() - timeInitialized) * dInvFreq; }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Given seconds it will output a human friendly readable string (milliseconds if less than 60 seconds)
        /// </summary>
        /// <returns></returns>
        public static string ToReadable(double seconds)
        {
            if (seconds > 60)
                return TimeSpan.FromSeconds(seconds).ToString();
            return (seconds * 1000.0).ToString("N2") + " ms";
        }
    } // public static class NetTime
} // namespace TridentFramework.RPC.Net.PeerConnection
