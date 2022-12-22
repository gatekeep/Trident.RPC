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
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace TridentFramework.RPC
{
    /// <summary>
    /// Thin wrapper around formatted string; use type system to help ensure we
    /// are doing canonicalization right/consistently - the literal sections are held in an
    /// un-escaped format. We are assuming that the string will be always built as Lit{Var}Lit[{Var}Lit[{Var}Lit[...]]],
    /// when the first and last literals may be empty.
    /// </summary>
    internal class UriTemplateCompoundPathSegment : UriTemplatePathSegment, IComparable<UriTemplateCompoundPathSegment>
    {
        private readonly string firstLiteral;
        private readonly List<VarAndLitPair> varLitPairs;
        private CompoundSegmentClass csClass;

        /// <summary>
        /// 
        /// </summary>
        private enum CompoundSegmentClass
        {
            /// <summary>
            /// 
            /// </summary>
            Undefined,
            /// <summary>
            /// 
            /// </summary>
            HasPrefixAndSuffix,
            /// <summary>
            /// 
            /// </summary>
            HasOnlyPrefix,
            /// <summary>
            /// 
            /// </summary>
            HasOnlySuffix,
            /// <summary>
            /// 
            /// </summary>
            HasNoPrefixNorSuffix
        } // private enum CompoundSegmentClass

        /*
        ** Classes
        */

        /// <summary>
        /// 
        /// </summary>
        private struct VarAndLitPair
        {
            private readonly string literal;
            private readonly string varName;

            /*
            ** Properties
            */

            /// <summary>
            /// 
            /// </summary>
            public string Literal
            {
                get { return this.literal; }
            }

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
            /// Initializes a new instance of the <see cref="VarAndLitPair"/> struct.
            /// </summary>
            /// <param name="varName"></param>
            /// <param name="literal"></param>
            public VarAndLitPair(string varName, string literal)
            {
                this.varName = varName;
                this.literal = literal;
            }
        } // private struct VarAndLitPair

        /*
        ** Methods
        */

        UriTemplateCompoundPathSegment(string originalSegment, bool endsWithSlash, string firstLiteral)
            : base(originalSegment, UriTemplatePartType.Compound, endsWithSlash)
        {
            this.firstLiteral = firstLiteral;
            this.varLitPairs = new List<VarAndLitPair>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="segment"></param>
        /// <param name="template"></param>
        /// <returns></returns>
        public static new UriTemplateCompoundPathSegment CreateFromUriTemplate(string segment, UriTemplate template)
        {
            string origSegment = segment;
            bool endsWithSlash = segment.EndsWith("/", StringComparison.Ordinal);
            if (endsWithSlash)
                segment = segment.Remove(segment.Length - 1);

            int nextVarStart = segment.IndexOf("{", StringComparison.Ordinal);
            string firstLiteral = ((nextVarStart > 0) ? segment.Substring(0, nextVarStart) : string.Empty);
            if (firstLiteral.IndexOf(UriTemplate.WildcardPath, StringComparison.Ordinal) != -1)
                throw new FormatException(string.Format("Invalid wildcard in variable or literal; {0} {1}", template.originalTemplate, UriTemplate.WildcardPath));

            UriTemplateCompoundPathSegment result = new UriTemplateCompoundPathSegment(origSegment, endsWithSlash,
                ((firstLiteral != string.Empty) ? Uri.UnescapeDataString(firstLiteral) : string.Empty));
            do
            {
                int nextVarEnd = segment.IndexOf("}", nextVarStart + 1, StringComparison.Ordinal);
                if (nextVarEnd < nextVarStart + 2)
                    throw new FormatException(string.Format("Invalid format in segment or query part; {0}", segment));

                bool hasDefault;
                string varName = template.AddPathVariable(UriTemplatePartType.Compound,
                    segment.Substring(nextVarStart + 1, nextVarEnd - nextVarStart - 1), out hasDefault);
                if (hasDefault)
                    throw new InvalidOperationException(string.Format("Default value to compaony segment variable; {0} {1} {2}", template, origSegment, varName));

                nextVarStart = segment.IndexOf("{", nextVarEnd + 1, StringComparison.Ordinal);
                string literal;
                if (nextVarStart > 0)
                {
                    if (nextVarStart == nextVarEnd + 1)
                        throw new ArgumentException(string.Format("Does not support adjacent variables in compound segment; {0} {1}", template, segment), "template");
                    literal = segment.Substring(nextVarEnd + 1, nextVarStart - nextVarEnd - 1);
                }
                else if (nextVarEnd + 1 < segment.Length)
                    literal = segment.Substring(nextVarEnd + 1);
                else
                    literal = string.Empty;

                if (literal.IndexOf(UriTemplate.WildcardPath, StringComparison.Ordinal) != -1)
                    throw new FormatException(string.Format("Invalid wildcard in variable or literal; {0} {1}", template.originalTemplate, UriTemplate.WildcardPath));

                if (literal.IndexOf('}') != -1)
                    throw new FormatException(string.Format("Invalid format in segment or query part; {0}", segment));

                result.varLitPairs.Add(new VarAndLitPair(varName, ((literal == string.Empty) ? string.Empty : Uri.UnescapeDataString(literal))));
            } while (nextVarStart > 0);

            if (string.IsNullOrEmpty(result.firstLiteral))
            {
                if (string.IsNullOrEmpty(result.varLitPairs[result.varLitPairs.Count - 1].Literal))
                    result.csClass = CompoundSegmentClass.HasNoPrefixNorSuffix;
                else
                    result.csClass = CompoundSegmentClass.HasOnlySuffix;
            }
            else
            {
                if (string.IsNullOrEmpty(result.varLitPairs[result.varLitPairs.Count - 1].Literal))
                    result.csClass = CompoundSegmentClass.HasOnlyPrefix;
                else
                    result.csClass = CompoundSegmentClass.HasPrefixAndSuffix;
            }

            return result;
        }

        /// <inheritdoc />
        public override void Bind(string[] values, ref int valueIndex, StringBuilder path)
        {
            path.Append(this.firstLiteral);
            for (int pairIndex = 0; pairIndex < this.varLitPairs.Count; pairIndex++)
            {
                path.Append(values[valueIndex++]);
                path.Append(this.varLitPairs[pairIndex].Literal);
            }

            if (this.EndsWithSlash)
                path.Append("/");
        }

        /// <inheritdoc />
        public override bool IsEquivalentTo(UriTemplatePathSegment other, bool ignoreTrailingSlash)
        {
            if (other == null)
                return false;

            if (!ignoreTrailingSlash && (this.EndsWithSlash != other.EndsWithSlash))
                return false;

            UriTemplateCompoundPathSegment otherAsCompound = other as UriTemplateCompoundPathSegment;
            if (otherAsCompound == null)
            {
                // if other can't be cast as a compound then it can't be equivalent
                return false;
            }

            if (this.varLitPairs.Count != otherAsCompound.varLitPairs.Count)
                return false;

            if (StringComparer.OrdinalIgnoreCase.Compare(this.firstLiteral, otherAsCompound.firstLiteral) != 0)
                return false;

            for (int pairIndex = 0; pairIndex < this.varLitPairs.Count; pairIndex++)
            {
                if (StringComparer.OrdinalIgnoreCase.Compare(this.varLitPairs[pairIndex].Literal,
                    otherAsCompound.varLitPairs[pairIndex].Literal) != 0)
                    return false;
            }

            return true;
        }

        /// <inheritdoc />
        public override bool IsMatch(UriTemplateLiteralPathSegment segment, bool ignoreTrailingSlash)
        {
            if (!ignoreTrailingSlash && (this.EndsWithSlash != segment.EndsWithSlash))
                return false;

            return TryLookup(segment.AsUnescapedString(), null);
        }

        /// <inheritdoc />
        public override void Lookup(string segment, NameValueCollection boundParameters)
        {
            if (!TryLookup(segment, boundParameters))
                throw new InvalidOperationException("CSR lookup before match");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="segment"></param>
        /// <param name="boundParameters"></param>
        /// <returns></returns>
        private bool TryLookup(string segment, NameValueCollection boundParameters)
        {
            int segmentPosition = 0;
            if (!string.IsNullOrEmpty(this.firstLiteral))
            {
                if (segment.StartsWith(this.firstLiteral, StringComparison.Ordinal))
                    segmentPosition = this.firstLiteral.Length;
                else
                    return false;
            }

            for (int pairIndex = 0; pairIndex < this.varLitPairs.Count - 1; pairIndex++)
            {
                int nextLiteralPosition = segment.IndexOf(this.varLitPairs[pairIndex].Literal, segmentPosition, StringComparison.Ordinal);
                if (nextLiteralPosition < segmentPosition + 1)
                    return false;

                if (boundParameters != null)
                {
                    string varValue = segment.Substring(segmentPosition, nextLiteralPosition - segmentPosition);
                    boundParameters.Add(this.varLitPairs[pairIndex].VarName, varValue);
                }

                segmentPosition = nextLiteralPosition + this.varLitPairs[pairIndex].Literal.Length;
            }

            if (segmentPosition < segment.Length)
            {
                if (string.IsNullOrEmpty(this.varLitPairs[varLitPairs.Count - 1].Literal))
                {
                    if (boundParameters != null)
                        boundParameters.Add(this.varLitPairs[varLitPairs.Count - 1].VarName,
                            segment.Substring(segmentPosition));

                    return true;
                }
                else if ((segmentPosition + this.varLitPairs[varLitPairs.Count - 1].Literal.Length < segment.Length) &&
                    segment.EndsWith(this.varLitPairs[varLitPairs.Count - 1].Literal, StringComparison.Ordinal))
                {
                    if (boundParameters != null)
                        boundParameters.Add(this.varLitPairs[varLitPairs.Count - 1].VarName,
                            segment.Substring(segmentPosition, segment.Length - segmentPosition - this.varLitPairs[varLitPairs.Count - 1].Literal.Length));

                    return true;
                }
                else
                    return false;
            }
            else
                return false;
        }

        // A note about comparing compound segments:
        //  We are using this for generating the sorted collections at the nodes of the UriTemplateTrieNode.
        //  The idea is that we are sorting the segments based on preferred matching, when we have two
        //  compound segments matching the same wire segment, we will give preference to the preceding one.
        //  The order is based on the following concepts:
        //   - We are defining four classes of compound segments: prefix+suffix, prefix-only, suffix-only 
        //      and none
        //   - Whenever we are comparing segments from different class the preferred one is the segment with
        //      the prefared class, based on the order we defined them (p+s \ p \ s \ n).
        //   - Within each class the preference is based on the prefix\suffix, while prefix has precedence 
        //      over suffix if both exists.
        //   - If after comparing the class, as well as the prefix\suffix, we didn't reach to a conclusion,
        //      the preference is given to the segment with more variables parts.
        //  This order mostly follows the intuitive common sense; the major issue comes from preferring the
        //  prefix over the suffix in the case where both exist. This is derived from the problematic of any
        //  other type of solution that don't prefere the prefix over the suffix or vice versa. To better 
        //  understanding lets considered the following example:
        //   In comparing 'foo{x}bar' and 'food{x}ar', unless we are preferring prefix or suffix, we have
        //   to state that they have the same order. So is the case with 'foo{x}babar' and 'food{x}ar', which
        //   will lead us to claiming the 'foo{x}bar' and 'foo{x}babar' are from the same order, which they
        //   clearly are not. 
        //  Taking other approaches to this problem results in similar cases. The only solution is preferring
        //  either the prefix or the suffix over the other; since we already preferred prefix over suffix
        //  implicitly (we preferred the prefix only class over the suffix only, we also prefared literal
        //  over variable, if in the same path segment) that still maintain consistency.
        //  Therefore:
        //    - 'food{var}' should be before 'foo{var}'; '{x}.{y}.{z}' should be before '{x}.{y}'.
        //    - the order between '{var}bar' and '{var}qux' is not important
        //    - '{x}.{y}' and '{x}_{y}' should have the same order
        //    - 'foo{x}bar' is less preferred than 'food{x}ar'
        //  In the above third case - if we are opening the table with allowDuplicate=false, we will throw;
        //  if we are opening it with allowDuplicate=true we will let it go and might match both templates
        //  for certain wire candidates.
        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        int IComparable<UriTemplateCompoundPathSegment>.CompareTo(UriTemplateCompoundPathSegment other)
        {
            switch (this.csClass)
            {
                case CompoundSegmentClass.HasPrefixAndSuffix:
                    switch (other.csClass)
                    {
                        case CompoundSegmentClass.HasPrefixAndSuffix:
                            return CompareToOtherThatHasPrefixAndSuffix(other);

                        case CompoundSegmentClass.HasOnlyPrefix:
                        case CompoundSegmentClass.HasOnlySuffix:
                        case CompoundSegmentClass.HasNoPrefixNorSuffix:
                            return -1;

                        default:
                            return 0;
                    }

                case CompoundSegmentClass.HasOnlyPrefix:
                    switch (other.csClass)
                    {
                        case CompoundSegmentClass.HasPrefixAndSuffix:
                            return 1;

                        case CompoundSegmentClass.HasOnlyPrefix:
                            return CompareToOtherThatHasOnlyPrefix(other);

                        case CompoundSegmentClass.HasOnlySuffix:
                        case CompoundSegmentClass.HasNoPrefixNorSuffix:
                            return -1;

                        default:
                            return 0;
                    }

                case CompoundSegmentClass.HasOnlySuffix:
                    switch (other.csClass)
                    {
                        case CompoundSegmentClass.HasPrefixAndSuffix:
                        case CompoundSegmentClass.HasOnlyPrefix:
                            return 1;

                        case CompoundSegmentClass.HasOnlySuffix:
                            return CompareToOtherThatHasOnlySuffix(other);

                        case CompoundSegmentClass.HasNoPrefixNorSuffix:
                            return -1;

                        default:
                            return 0;
                    }

                case CompoundSegmentClass.HasNoPrefixNorSuffix:
                    switch (other.csClass)
                    {
                        case CompoundSegmentClass.HasPrefixAndSuffix:
                        case CompoundSegmentClass.HasOnlyPrefix:
                        case CompoundSegmentClass.HasOnlySuffix:
                            return 1;

                        case CompoundSegmentClass.HasNoPrefixNorSuffix:
                            return CompareToOtherThatHasNoPrefixNorSuffix(other);

                        default:
                            return 0;
                    }

                default:
                    return 0;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        private int CompareToOtherThatHasPrefixAndSuffix(UriTemplateCompoundPathSegment other)
        {
            // In this case we are determining the order based on the prefix of the two segments,
            //  then by their suffix and then based on the number of variables
            int prefixOrder = ComparePrefixToOtherPrefix(other);
            if (prefixOrder == 0)
            {
                int suffixOrder = CompareSuffixToOtherSuffix(other);
                if (suffixOrder == 0)
                    return (other.varLitPairs.Count - this.varLitPairs.Count);
                else
                    return suffixOrder;
            }
            else
                return prefixOrder;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        private int CompareToOtherThatHasOnlyPrefix(UriTemplateCompoundPathSegment other)
        {
            // In this case we are determining the order based on the prefix of the two segments,
            //  then based on the number of variables
            int prefixOrder = ComparePrefixToOtherPrefix(other);
            if (prefixOrder == 0)
                return (other.varLitPairs.Count - this.varLitPairs.Count);
            else
                return prefixOrder;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        private int CompareToOtherThatHasOnlySuffix(UriTemplateCompoundPathSegment other)
        {
            // In this case we are determining the order based on the suffix of the two segments,
            //  then based on the number of variables
            int suffixOrder = CompareSuffixToOtherSuffix(other);
            if (suffixOrder == 0)
                return (other.varLitPairs.Count - this.varLitPairs.Count);
            else
                return suffixOrder;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        private int CompareToOtherThatHasNoPrefixNorSuffix(UriTemplateCompoundPathSegment other)
        {
            // In this case the order is determined by the number of variables
            return (other.varLitPairs.Count - this.varLitPairs.Count);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        private int ComparePrefixToOtherPrefix(UriTemplateCompoundPathSegment other)
        {
            return string.Compare(other.firstLiteral, this.firstLiteral, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        private int CompareSuffixToOtherSuffix(UriTemplateCompoundPathSegment other)
        {
            string reversedSuffix = ReverseString(this.varLitPairs[this.varLitPairs.Count - 1].Literal);
            string reversedOtherSuffix = ReverseString(other.varLitPairs[other.varLitPairs.Count - 1].Literal);
            return string.Compare(reversedOtherSuffix, reversedSuffix, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stringToReverse"></param>
        /// <returns></returns>
        private static string ReverseString(string stringToReverse)
        {
            char[] reversedString = new char[stringToReverse.Length];
            for (int i = 0; i < stringToReverse.Length; i++)
                reversedString[i] = stringToReverse[stringToReverse.Length - i - 1];

            return new string(reversedString);
        }
    } // internal class UriTemplateCompoundPathSegment : UriTemplatePathSegment, IComparable<UriTemplateCompoundPathSegment>
} // namespace TridentFramework.RPC
