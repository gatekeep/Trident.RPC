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

using System;

namespace TridentFramework.RPC
{
    /// <summary>
    /// Indicates that an interface or a class defines a service contract for RPC operations.
    /// </summary>
    /// <remarks>This is the library-equivilent to the .NET Framework <b>ServiceContract</b> attribute.</remarks>
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class RPCContractAttribute : Attribute
    {
        private Type callbackContract = null;
        private string ns;

        /*
        ** Properties
        */

        /// <summary>
        /// Gets or sets the namespace of the contract.
        /// </summary>
        public string Namespace
        {
            get { return ns; }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    Uri uri;
                    if (!Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out uri))
                        throw new ArgumentException(string.Format("Invalid URI for service contract namespace"));
                }
                ns = value;
            }
        }

        /// <summary>
        /// Gets or sets the type of callback contract when the contract is a duplex contract.
        /// </summary>
        public Type CallbackContract
        {
            get => this.callbackContract;
            set => this.callbackContract = value;
        }
    } // public sealed class RPCContractAttribute : Attribute
} // namespace TridentFramework.RPC
