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
    /// Thin wrapper around string; use type system to help ensure we
    /// are doing canonicalization right/consistently.
    /// </summary>
    internal class UriTemplateLiteralPathSegment : UriTemplatePathSegment, IComparable<UriTemplateLiteralPathSegment>
    {
        // segment doesn't store trailing slash
        private readonly string segment;
        private static Uri dummyUri = new Uri("http://localhost");

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="UriTemplateLiteralPathSegment"/> class.
        /// </summary>
        /// <param name="segment"></param>
        private UriTemplateLiteralPathSegment(string segment) :
            base(segment, UriTemplatePartType.Literal, segment.EndsWith("/", StringComparison.Ordinal))
        {
            if (this.EndsWithSlash)
                this.segment = segment.Remove(segment.Length - 1);
            else
                this.segment = segment;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="segment"></param>
        /// <param name="template"></param>
        /// <returns></returns>
        public static new UriTemplateLiteralPathSegment CreateFromUriTemplate(string segment, UriTemplate template)
        {
            // run it through UriBuilder to escape-if-necessary it
            if (string.Compare(segment, "/", StringComparison.Ordinal) == 0)
            {
                // running an empty segment through UriBuilder has unexpected/wrong results
                return new UriTemplateLiteralPathSegment("/");
            }

            if (segment.IndexOf(UriTemplate.WildcardPath, StringComparison.Ordinal) != -1)
                throw new FormatException(string.Format("Invalid wildcard in variable or literal; {0} {1}", template.originalTemplate, UriTemplate.WildcardPath));

            // '*' is not usually escaped by the Uri\UriBuilder to %2a, since we forbid passing a
            // clear character and the workaroud is to pass the escaped form, we should replace the
            // escaped form with the regular one.
            segment = segment.Replace("%2a", "*").Replace("%2A", "*");
            UriBuilder ub = new UriBuilder(dummyUri);
            ub.Path = segment;
            string escapedIfNecessarySegment = ub.Uri.AbsolutePath.Substring(1);
            if (escapedIfNecessarySegment == string.Empty)
            {
                // This path through UriBuilder will sometimes '----' various segments
                // such as '../' and './'.  When this happens and the result is an empty
                // string, we should just throw and tell the user we don't handle that.
                throw new ArgumentException(string.Format("Invalid format of segment or query part; {0}", segment), "segment");
            }

            return new UriTemplateLiteralPathSegment(escapedIfNecessarySegment);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="segment"></param>
        /// <returns></returns>
        public static UriTemplateLiteralPathSegment CreateFromWireData(string segment)
        {
            return new UriTemplateLiteralPathSegment(segment);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string AsUnescapedString()
        {
            return Uri.UnescapeDataString(this.segment);
        }

        /// <inheritdoc />
        public override void Bind(string[] values, ref int valueIndex, StringBuilder path)
        {
            if (this.EndsWithSlash)
                path.AppendFormat("{0}/", AsUnescapedString());
            else
                path.Append(AsUnescapedString());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(UriTemplateLiteralPathSegment other)
        {
            return StringComparer.OrdinalIgnoreCase.Compare(this.segment, other.segment);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            UriTemplateLiteralPathSegment lps = obj as UriTemplateLiteralPathSegment;
            if (lps == null)
                return false;
            else
                return ((this.EndsWithSlash == lps.EndsWithSlash) && StringComparer.OrdinalIgnoreCase.Equals(this.segment, lps.segment));
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(this.segment);
        }

        /// <inheritdoc />
        public override bool IsEquivalentTo(UriTemplatePathSegment other, bool ignoreTrailingSlash)
        {
            if (other == null)
                return false;

            if (other.Nature != UriTemplatePartType.Literal)
                return false;

            UriTemplateLiteralPathSegment otherAsLiteral = other as UriTemplateLiteralPathSegment;
            return IsMatch(otherAsLiteral, ignoreTrailingSlash);
        }

        /// <inheritdoc />
        public override bool IsMatch(UriTemplateLiteralPathSegment segment, bool ignoreTrailingSlash)
        {
            if (!ignoreTrailingSlash && (segment.EndsWithSlash != this.EndsWithSlash))
                return false;

            return (CompareTo(segment) == 0);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool IsNullOrEmpty()
        {
            return string.IsNullOrEmpty(this.segment);
        }

        /// <inheritdoc />
        public override void Lookup(string segment, NameValueCollection boundParameters)
        {
            /* stub */
        }
    } // internal class UriTemplateLiteralPathSegment : UriTemplatePathSegment, IComparable<UriTemplateLiteralPathSegment>
} // namespace TridentFramework.RPC
