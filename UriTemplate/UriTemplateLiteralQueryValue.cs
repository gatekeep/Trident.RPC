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
/*
 * Based on code from .NET Reference Source
 * Copyright (C) Microsoft Corporation., All Rights Reserved.
 * Licensed under MIT License.
 */

using System;
using System.Collections.Specialized;
using System.Text;
using System.Web;

namespace TridentFramework.RPC
{
    /// <summary>
    /// thin wrapper around string; use type system to help ensure we
    /// are doing canonicalization right/consistently.
    /// </summary>
    internal class UriTemplateLiteralQueryValue : UriTemplateQueryValue, IComparable<UriTemplateLiteralQueryValue>
    {
        private readonly string value; // an unescaped representation

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="UriTemplateLiteralQueryValue"/> class.
        /// </summary>
        /// <param name="value"></param>
        private UriTemplateLiteralQueryValue(string value) : base(UriTemplatePartType.Literal)
        {
            this.value = value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static UriTemplateLiteralQueryValue CreateFromUriTemplate(string value)
        {
            return new UriTemplateLiteralQueryValue(HttpUtility.UrlDecode(value, Encoding.UTF8));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string AsEscapedString()
        {
            return HttpUtility.UrlEncode(this.value, Encoding.UTF8);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string AsRawUnescapedString()
        {
            return this.value;
        }

        /// <inheritdoc />
        public override void Bind(string keyName, string[] values, ref int valueIndex, StringBuilder query)
        {
            query.AppendFormat("&{0}={1}", HttpUtility.UrlEncode(keyName, Encoding.UTF8), AsEscapedString());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(UriTemplateLiteralQueryValue other)
        {
            return string.Compare(this.value, other.value, StringComparison.Ordinal);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            UriTemplateLiteralQueryValue lqv = obj as UriTemplateLiteralQueryValue;
            if (lqv == null)
                return false;
            else
                return this.value == lqv.value;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return this.value.GetHashCode();
        }

        /// <inheritdoc />
        public override bool IsEquivalentTo(UriTemplateQueryValue other)
        {
            if (other == null)
                return false;

            if (other.Nature != UriTemplatePartType.Literal)
                return false;

            UriTemplateLiteralQueryValue otherAsLiteral = other as UriTemplateLiteralQueryValue;
            return (CompareTo(otherAsLiteral) == 0);
        }

        /// <inheritdoc />
        public override void Lookup(string value, NameValueCollection boundParameters)
        {
            /* stub */
        }
    } // internal class UriTemplateLiteralQueryValue : UriTemplateQueryValue, IComparable<UriTemplateLiteralQueryValue>
} // namespace TridentFramework.RPC.UriTemplate
