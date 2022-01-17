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
using System.Collections.Specialized;
using System.Text;

namespace TridentFramework.RPC
{
    /// <summary>
    /// 
    /// </summary>
    internal class UriTemplateVariablePathSegment : UriTemplatePathSegment
    {
        private readonly string varName;

        /*
        ** Properties
        */

        /// <summary>
        /// 
        /// </summary>
        public string VarName
        {
            get { return this.varName; }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="UriTemplateVariablePathSegment"/> class.
        /// </summary>
        /// <param name="originalSegment"></param>
        /// <param name="endsWithSlash"></param>
        /// <param name="varName"></param>
        public UriTemplateVariablePathSegment(string originalSegment, bool endsWithSlash, string varName)
            : base(originalSegment, UriTemplatePartType.Variable, endsWithSlash)
        {
            this.varName = varName;
        }

        /// <inheritdoc />
        public override void Bind(string[] values, ref int valueIndex, StringBuilder path)
        {
            if (this.EndsWithSlash)
                path.AppendFormat("{0}/", values[valueIndex++]);
            else
                path.Append(values[valueIndex++]);
        }

        /// <inheritdoc />
        public override bool IsEquivalentTo(UriTemplatePathSegment other, bool ignoreTrailingSlash)
        {
            if (other == null)
                return false;

            if (!ignoreTrailingSlash && (this.EndsWithSlash != other.EndsWithSlash))
                return false;

            return (other.Nature == UriTemplatePartType.Variable);
        }

        /// <inheritdoc />
        public override bool IsMatch(UriTemplateLiteralPathSegment segment, bool ignoreTrailingSlash)
        {
            if (!ignoreTrailingSlash && (this.EndsWithSlash != segment.EndsWithSlash))
                return false;

            return (!segment.IsNullOrEmpty());
        }

        /// <inheritdoc />
        public override void Lookup(string segment, NameValueCollection boundParameters)
        {
            boundParameters.Add(this.varName, segment);
        }
    } // internal class UriTemplateVariablePathSegment : UriTemplatePathSegment
} // namespace TridentFramework.RPC
