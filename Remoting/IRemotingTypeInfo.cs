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
 * Based on code from .NET Reference Source
 * Copyright (C) Microsoft Corporation., All Rights Reserved.
 * Licensed under MIT License.
 */

using System;
using System.Security;
using System.Runtime.InteropServices;

namespace TridentFramework.RPC.Remoting
{
    /// <summary>
    /// Provides type information for an object.
    /// </summary>
    [ComVisible(true)]
    public interface IRemotingTypeInfo
    {
        /*
        ** Properties
        */

        /// <summary>
        /// Gets or sets the fully qualified type name of the server object in a ObjRef.
        /// </summary>
        string TypeName
        {
            [SecurityCritical] get;
            [SecurityCritical] set;
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Checks whether the proxy that represents the specified object type can be cast to the type represented by the
        /// <see cref="IRemotingTypeInfo"/> interface.
        /// </summary>
        /// <param name="fromType">The type to cast to.</param>
        /// <param name="o">The object for which to check casting.</param>
        /// <returns>true if cast will succeed; otherwise, false.</returns>
        [SecurityCritical]
        bool CanCastTo(Type fromType, object o);
    } // public interface IRemotingTypeInfo
} // namespace TridentFramework.RPC.Remoting
