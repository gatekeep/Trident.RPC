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

using TridentFramework.RPC.Http;
using TridentFramework.RPC.Http.Service;

namespace TridentFramework.RPC
{
    /// <summary>
    /// Attribute that defines an OperationContract as a user-defined endpoint handler. Method arguments for such a handler, must be exactly (in specified order):
    /// <b>void Method(<see cref="RequestWorker"/>, <see cref="IHttpContext"/>)</b>.
    /// <para>
    ///   <i>NOTE: Any response from a user-defined endpoint handler is defined by the handler,
    ///   no automatic HTTP responses will be generated from the RPC services for a user-defined endpoint handler.</i>
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class UserEndpointHandlerAttribute : Attribute
    {
        /* stub */
    } // public class UserEndpointHandlerAttribute : Attribute
} // namespace TridentFramework.RPC
