﻿/*
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

namespace TridentFramework.RPC.RestDoc
{
    /// <summary>
    /// Attribute to hold the description of service method arguments.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
    public class RestDocMethodArgAttribute : Attribute
    {
        /*
        ** Properties
        */

        /// <summary>
        /// Argument name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description for the method argument value.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Argument type.
        /// </summary>
        /// <remarks>This is only used when <see cref="RestDocMethodAttribute.IgnoreMetadata"/> is used.</remarks>
        public Type ArgumentType { get; set; } = null;

        /// <summary>
        /// Flag indicating this argument is output from the method.
        /// </summary>
        /// <remarks>This is only used when <see cref="RestDocMethodAttribute.IgnoreMetadata"/> is used.</remarks>
        public bool OutputArgument { get; set; } = false;
    } // public class WebDocMethodArgAttribute : Attribute
} // namespace TridentFramework.RPC.RestDoc
