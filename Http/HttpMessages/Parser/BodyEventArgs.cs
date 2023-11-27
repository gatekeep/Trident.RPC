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
//
// Based on code from the C# WebServer project. (http://webserver.codeplex.com/)
// Copyright (c) 2012 Jonas Gauffin
// Licensed under the Apache 2.0 License (http://opensource.org/licenses/Apache-2.0)
//

using System;

namespace TridentFramework.RPC.Http.HttpMessages.Parser
{
    /// <summary>
    /// Arguments used when more body bytes have come.
    /// </summary>
    public class BodyEventArgs : EventArgs
    {
        /*
        ** Properties
        */

        /// <summary>
        /// Gets or sets buffer that contains the received bytes.
        /// </summary>
        public byte[] Buffer { get; internal set; }

        /// <summary>
        /// Gets or sets number of bytes from <see cref="Offset"/> that should be parsed.
        /// </summary>
        public int Count { get; internal set; }

        /// <summary>
        /// Gets or sets offset in buffer where to start processing.
        /// </summary>
        public int Offset { get; internal set; }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="BodyEventArgs"/> class.
        /// </summary>
        /// <param name="buffer">buffer that contains the received bytes.</param>
        /// <param name="offset">offset in buffer where to start processing.</param>
        /// <param name="count">number of bytes from <paramref name="offset"/> that should be parsed.</param>
        /// <exception cref="ArgumentNullException"><c>buffer</c> is <c>null</c>.</exception>
        public BodyEventArgs(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");

            Buffer = buffer;
            Offset = offset;
            Count = count;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BodyEventArgs"/> class.
        /// </summary>
        public BodyEventArgs()
        {
            /* stub */
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        internal void AssignInternal(byte[] bytes, int offset, int count)
        {
            Buffer = bytes;
            Offset = offset;
            Count = count;
        }
    } // public class BodyEventArgs : EventArgs
} // namespace TridentFramework.RPC.Http.HttpMessages.Parser
