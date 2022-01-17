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

using System;

namespace TridentFramework.RPC
{
    /// <summary>
    /// Indicates that a method defines an operation that is part of a RPC contract that provides REST endpoints.
    /// </summary>
    /// <remarks>This is the library-equivilent to the .NET Framework <b>WebInvoke</b> attribute.</remarks>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class RestMethodAttribute : Attribute
    {
        private string method; // http verb
        private string uriTemplate; // Note: HttpTransferEndpointBehavior interprets uriTemplate as: null means 'no opinion', whereas string.Empty means relative path of ""

        /*
        ** Properties
        */

        /// <summary>
        /// Gets or sets the protocol (for example HTTP) method the service operation responds to.
        /// </summary>
        public string Method
        {
            get => this.method;
            set => this.method = value;
        }

        /// <summary>
        /// The Uniform Resource Identifier (URI) template for the service operation.
        /// </summary>
        public string UriTemplate
        {
            get => this.uriTemplate;
            set => this.uriTemplate = value;
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="RestMethodAttribute"/> class.
        /// </summary>
        public RestMethodAttribute()
        {
            /* stub */
        }
    } // public sealed class RestMethodAttribute : Attribute
} // namespace TridentFramework.RPC
