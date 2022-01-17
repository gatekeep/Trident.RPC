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
using System.Runtime.Serialization;

namespace TridentFramework.RPC
{
    /// <summary>
    ///
    /// </summary>
    [Serializable]
    public sealed class RPCException : Exception
    {
        private string faultString;
        private string faultActor;

        /*
        ** Properties
        */

        /// <summary>
        /// Gets the human readable explanation of the fault.
        /// </summary>
        public string FaultString
        {
            get { return faultString; }
        }

        /// <summary>
        /// Gets the fault actor.
        /// </summary>
        public string FaultActor
        {
            get { return faultActor; }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="RPCException"/> class.
        /// </summary>
        public RPCException()
            : base()
        {
            this.faultString = string.Empty;
            this.faultActor = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RPCException"/> class.
        /// </summary>
        public RPCException(string message)
            : base(message)
        {
            this.faultString = string.Empty;
            this.faultActor = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RPCException"/> class.
        /// </summary>
        public RPCException(string message, string faultString, string faultActor)
            : base(message)
        {
            this.faultString = faultString;
            this.faultActor = faultActor;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RPCException"/> class.
        /// </summary>
        public RPCException(string message, Exception inner)
            : base(message, inner)
        {
            this.faultString = string.Empty;
            this.faultActor = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RPCException"/> class.
        /// </summary>
        public RPCException(string message, string faultString, string faultActor, Exception inner)
            : base(message, inner)
        {
            this.faultString = faultString;
            this.faultActor = faultActor;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RPCException"/> class.
        /// </summary>
        private RPCException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            this.faultString = string.Empty;
            this.faultActor = string.Empty;
        }
    } // public sealed class RPCException : Exception
} // namespace TridentFramework.RPC
