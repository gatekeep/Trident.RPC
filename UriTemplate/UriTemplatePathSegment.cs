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
    /// This represents a Path segment, which can either be a Literal, a Variable or a Compound.
    /// </summary>
    internal abstract class UriTemplatePathSegment
    {
        private readonly bool endsWithSlash;
        private readonly UriTemplatePartType nature;
        private readonly string originalSegment;

        /*
        ** Properties
        */

        /// <summary>
        /// 
        /// </summary>
        public bool EndsWithSlash
        {
            get { return this.endsWithSlash; }
        }

        /// <summary>
        /// 
        /// </summary>
        public UriTemplatePartType Nature
        {
            get { return this.nature; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string OriginalSegment
        {
            get { return this.originalSegment; }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="UriTemplatePathSegment"/> class.
        /// </summary>
        /// <param name="originalSegment"></param>
        /// <param name="nature"></param>
        /// <param name="endsWithSlash"></param>
        protected UriTemplatePathSegment(string originalSegment, UriTemplatePartType nature, bool endsWithSlash)
        {
            this.originalSegment = originalSegment;
            this.nature = nature;
            this.endsWithSlash = endsWithSlash;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="segment"></param>
        /// <param name="template"></param>
        /// <returns></returns>
        public static UriTemplatePathSegment CreateFromUriTemplate(string segment, UriTemplate template)
        {
            // Identifying the type of segment - Literal|Compound|Variable
            switch (UriTemplateHelpers.IdentifyPartType(segment))
            {
                case UriTemplatePartType.Literal:
                    return UriTemplateLiteralPathSegment.CreateFromUriTemplate(segment, template);

                case UriTemplatePartType.Compound:
                    return UriTemplateCompoundPathSegment.CreateFromUriTemplate(segment, template);

                case UriTemplatePartType.Variable:
                    if (segment.EndsWith("/", StringComparison.Ordinal))
                    {
                        string varName = template.AddPathVariable(UriTemplatePartType.Variable,
                            segment.Substring(1, segment.Length - 3));
                        return new UriTemplateVariablePathSegment(segment, true, varName);
                    }
                    else
                    {
                        string varName = template.AddPathVariable(UriTemplatePartType.Variable,
                            segment.Substring(1, segment.Length - 2));
                        return new UriTemplateVariablePathSegment(segment, false, varName);
                    }

                default:
                    return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="values"></param>
        /// <param name="valueIndex"></param>
        /// <param name="path"></param>
        public abstract void Bind(string[] values, ref int valueIndex, StringBuilder path);

        /// <summary>
        /// Indicates whether a <see cref="UriTemplatePathSegment"/> is structurally equivalent to another.
        /// </summary>
        /// <param name="other">The UriTemplatePathSegment to compare.</param>
        /// <param name="ignoreTrailingSlash"></param>
        /// <returns></returns>
        public abstract bool IsEquivalentTo(UriTemplatePathSegment other, bool ignoreTrailingSlash);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="segment"></param>
        /// <returns></returns>
        public bool IsMatch(UriTemplateLiteralPathSegment segment)
        {
            return IsMatch(segment, false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="segment"></param>
        /// <param name="ignoreTrailingSlash"></param>
        /// <returns></returns>
        public abstract bool IsMatch(UriTemplateLiteralPathSegment segment, bool ignoreTrailingSlash);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="segment"></param>
        /// <param name="boundParameters"></param>
        public abstract void Lookup(string segment, NameValueCollection boundParameters);
    } // internal abstract class UriTemplatePathSegment
} // namespace TridentFramework.RPC
