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

using System;
using System.Net;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TridentFramework.RPC
{
    /// <summary>
    /// Interface representing a result from the REST API to the API caller.
    /// </summary>
    public interface IRestResult
    {
        /*
        ** Properties
        */

        /// <summary>
        /// Flag indicating whether or not the result contains a successful response.
        /// </summary>
        bool Success { get; set; }
    } // public interface IResult

    /// <summary>
    /// Interface representing a result with data from the REST API to the API caller.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IRestResult<T> : IRestResult
    {
        /*
        ** Properties
        */

        /// <summary>
        ///
        /// </summary>
        T Data { get; set; }
    } // public interface IResult<T> : IResult

    /// <summary>
    /// Representation of a result with data from the REST API to the API caller.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [DataContract]
    public class RestResult<T> : IRestResult<T>
    {
        /*
        ** Properties
        */

        /// <summary>
        /// Flag indicating whether or not the result contains a successful response.
        /// </summary>
        [DataMember(Name = "success")]
        public bool Success { get; set; }

        /// <summary>
        ///
        /// </summary>
        [DataMember(Name = "data", EmitDefaultValue = false)]
        public T Data { get; set; }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="RestResult{T}"/> class.
        /// </summary>
        /// <param name="data"></param>
        public RestResult() : base()
        {
            Success = true;
            Data = default(T);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RestResult{T}"/> class.
        /// </summary>
        /// <param name="data"></param>
        public RestResult(T data) : base()
        {
            Success = true;
            Data = data;
        }
    } // public class RestResult<T> : IRestResult<T>
} // namespace TridentFramework.RPC
