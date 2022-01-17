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

using Newtonsoft.Json.Linq;

namespace TridentFramework.RPC
{
    /// <summary>
    /// Defines a message inspector object that can be added to a <see cref="RPCService"/> to handle exceptions
    /// and faults.
    /// </summary>
    public interface IRPCExceptionHandler
    {
        /*
        ** Methods
        */

        /// <summary>
        /// Enables error-related processing and returns a value that indicates the exception.
        /// </summary>
        /// <param name="ex"></param>
        bool HandleError(Exception ex);

        /// <summary>
        /// Enables the creation of custom fault message that is returned from an exception in the course
        /// of a service method.
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="fault"></param>
        /// <returns></returns>
        void ProvideFault(Exception ex, ref JObject fault);
    } // public interface IRPCExceptionHandler
} // namespace TridentFramework.RPC
