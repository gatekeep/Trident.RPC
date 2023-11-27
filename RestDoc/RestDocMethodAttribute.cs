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

namespace TridentFramework.RPC.RestDoc
{
    /// <summary>
    /// Attribute to hold the description of the service method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class RestDocMethodAttribute : Attribute
    {
        /*
        ** Properties
        */

        /// <summary>
        /// Pretty name for method.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description for the method.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Flag indicating the method metadata should be ignored for documentation generation.
        /// </summary>
        /// <remarks>This will allow for documentation that does not match the method signature.</remarks>
        public bool IgnoreMetadata { get; set; } = false;
    } // public class WebDocMethodAttribute : Attribute
} // namespace TridentFramework.RPC.RestDoc
