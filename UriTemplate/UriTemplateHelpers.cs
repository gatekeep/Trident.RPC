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
using System.Web;

namespace TridentFramework.RPC
{
    /// <summary>
    /// 
    /// </summary>
    internal static class UriTemplateHelpers
    {
        private static UriTemplateQueryComparer queryComparer = new UriTemplateQueryComparer();
        private static UriTemplateQueryKeyComparer queryKeyComperar = new UriTemplateQueryKeyComparer();

        /*
        ** Classes
        */

        /// <summary>
        /// 
        /// </summary>
        private class UriTemplateQueryComparer : IComparer<UriTemplate>
        {
            /// <inheritdoc />
            public int Compare(UriTemplate x, UriTemplate y)
            {
                // sort the empty queries to the front
                return Comparer<int>.Default.Compare(x.queries.Count, y.queries.Count);
            }
        } // private class UriTemplateQueryComparer : IComparer<UriTemplate>


        /// <summary>
        /// 
        /// </summary>
        private class UriTemplateQueryKeyComparer : IEqualityComparer<string>
        {
            /// <inheritdoc />
            public bool Equals(string x, string y)
            {
                return (string.Compare(x, y, StringComparison.OrdinalIgnoreCase) == 0);
            }

            /// <inheritdoc />
            public int GetHashCode(string obj)
            {
                if (obj == null)
                    throw new ArgumentNullException("obj");

                return obj.ToUpperInvariant().GetHashCode();
            }
        } // private class UriTemplateQueryKeyComparer : IEqualityComparer<string>

        /*
        ** Methods
        */

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ut"></param>
        /// <param name="query"></param>
        /// <param name="mustBeEspeciallyInteresting"></param>
        /// <returns></returns>
        public static bool CanMatchQueryInterestingly(UriTemplate ut, NameValueCollection query, bool mustBeEspeciallyInteresting)
        {
            if (ut.queries.Count == 0)
                return false; // trivial, not interesting

            string[] queryKeys = query.AllKeys;
            foreach (KeyValuePair<string, UriTemplateQueryValue> kvp in ut.queries)
            {
                string queryKeyName = kvp.Key;
                if (kvp.Value.Nature == UriTemplatePartType.Literal)
                {
                    bool queryKeysContainsQueryVarName = false;
                    for (int i = 0; i < queryKeys.Length; ++i)
                    {
                        if (StringComparer.OrdinalIgnoreCase.Equals(queryKeys[i], queryKeyName))
                        {
                            queryKeysContainsQueryVarName = true;
                            break;
                        }
                    }

                    if (!queryKeysContainsQueryVarName)
                        return false;

                    if (kvp.Value == UriTemplateQueryValue.Empty)
                    {
                        if (!string.IsNullOrEmpty(query[queryKeyName]))
                            return false;
                    }
                    else
                    {
                        if (((UriTemplateLiteralQueryValue)(kvp.Value)).AsRawUnescapedString() != query[queryKeyName])
                            return false;
                    }
                }
                else
                {
                    if (mustBeEspeciallyInteresting && Array.IndexOf(queryKeys, queryKeyName) == -1)
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ut"></param>
        /// <returns></returns>
        public static bool CanMatchQueryTrivially(UriTemplate ut)
        {
            return (ut.queries.Count == 0);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="array"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="allowDuplicateEquivalentUriTemplates"></param>
        public static void DisambiguateSamePath(UriTemplate[] array, int a, int b, bool allowDuplicateEquivalentUriTemplates)
        {
            // [a,b) all have same path
            // ensure queries make them unambiguous

            // sort empty queries to front
            Array.Sort<UriTemplate>(array, a, b - a, queryComparer);
            if (b - a == 1)
                return; // if only one, cannot be ambiguous

            if (!allowDuplicateEquivalentUriTemplates)
            {
                // ensure at most one empty query and ignore it
                if (array[a].queries.Count == 0)
                    a++;

                if (array[a].queries.Count == 0)
                    throw new InvalidOperationException(string.Format("Duplicate path entry; {0} {1}", array[a].ToString(), array[a - 1].ToString()));

                if (b - a == 1)
                    return; // if only one, cannot be ambiguous
            }
            else
            {
                while (a < b && array[a].queries.Count == 0)  // all equivalent
                    a++;

                if (b - a <= 1)
                    return;
            }

            // now consider non-empty queries
            // more than one, so enforce that
            // forall
            //   exist set of querystringvars S where
            //     every op has literal value foreach var in S, and
            //     those literal tuples are different
            EnsureQueriesAreDistinct(array, a, b, allowDuplicateEquivalentUriTemplates);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static IEqualityComparer<string> GetQueryKeyComparer()
        {
            return queryKeyComperar;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public static string GetUriPath(Uri uri)
        {
            return uri.GetComponents(UriComponents.Path | UriComponents.KeepDelimiter, UriFormat.Unescaped);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ut"></param>
        /// <returns></returns>
        public static bool HasQueryLiteralRequirements(UriTemplate ut)
        {
            foreach (UriTemplateQueryValue utqv in ut.queries.Values)
                if (utqv.Nature == UriTemplatePartType.Literal)
                    return true;

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="part"></param>
        /// <returns></returns>
        public static UriTemplatePartType IdentifyPartType(string part)
        {
            // Identifying the nature of a string - Literal|Compound|Variable
            // Algorithem is based on the following steps:
            // - Finding the position of the first open curlly brace ('{') and close curlly brace ('}') 
            //    in the string
            // - If we don't find any this is a Literal
            // - otherwise, we validate that position of the close brace is at least two characters from 
            //    the position of the open brace
            // - Then we identify if we are dealing with a compound string or a single variable string
            //    + var name is not at the string start --> Compound
            //    + var name is shorter then the entire string (End < Length-2 or End==Length-2 
            //       and string ends with '/') --> Compound
            //    + otherwise --> Variable
            int varStartIndex = part.IndexOf("{", StringComparison.Ordinal);
            int varEndIndex = part.IndexOf("}", StringComparison.Ordinal);
            if (varStartIndex == -1)
            {
                if (varEndIndex != -1)
                    throw new FormatException(string.Format("Invalid format in segment or query part; {0}", part));

                return UriTemplatePartType.Literal;
            }
            else
            {
                if (varEndIndex < varStartIndex + 2)
                    throw new FormatException(string.Format("Invalid format in segment or query part; {0}", part));

                if (varStartIndex > 0)
                    return UriTemplatePartType.Compound;
                else if ((varEndIndex < part.Length - 2) || ((varEndIndex == part.Length - 2) && !part.EndsWith("/", StringComparison.Ordinal)))
                    return UriTemplatePartType.Compound;
                else
                    return UriTemplatePartType.Variable;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool IsWildcardPath(string path)
        {
            if (path.IndexOf('/') != -1)
                return false;

            UriTemplatePartType partType;
            return IsWildcardSegment(path, out partType);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="segment"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsWildcardSegment(string segment, out UriTemplatePartType type)
        {
            type = IdentifyPartType(segment);
            switch (type)
            {
                case UriTemplatePartType.Literal:
                    return (string.Compare(segment, UriTemplate.WildcardPath, StringComparison.Ordinal) == 0);

                case UriTemplatePartType.Compound:
                    return false;

                case UriTemplatePartType.Variable:
                    return ((segment.IndexOf(UriTemplate.WildcardPath, StringComparison.Ordinal) == 1) &&
                        !segment.EndsWith("/", StringComparison.Ordinal) &&
                        (segment.Length > UriTemplate.WildcardPath.Length + 2));

                default:
                    return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static NameValueCollection ParseQueryString(string query)
        {
            // We are adjusting the parsing of UrlUtility.ParseQueryString, which identify
            //  ?wsdl as a null key with wsdl as a value
            NameValueCollection result = HttpUtility.ParseQueryString(query);

            string nullKeyValuesString = result[(string)null];
            if (!string.IsNullOrEmpty(nullKeyValuesString))
            {
                result.Remove(null);
                string[] nullKeyValues = nullKeyValuesString.Split(',');
                for (int i = 0; i < nullKeyValues.Length; i++)
                    result.Add(nullKeyValues[i], null);
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="array"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static bool AllTemplatesAreEquivalent(IList<UriTemplate> array, int a, int b)
        {
            for (int i = a; i < b - 1; ++i)
                if (!array[i].IsEquivalentTo(array[i + 1]))
                    return false;

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="array"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="allowDuplicateEquivalentUriTemplates"></param>
        private static void EnsureQueriesAreDistinct(UriTemplate[] array, int a, int b, bool allowDuplicateEquivalentUriTemplates)
        {
            Dictionary<string, byte> queryVarNamesWithLiteralVals = new Dictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
            for (int i = a; i < b; ++i)
                foreach (KeyValuePair<string, UriTemplateQueryValue> kvp in array[i].queries)
                    if (kvp.Value.Nature == UriTemplatePartType.Literal)
                        if (!queryVarNamesWithLiteralVals.ContainsKey(kvp.Key))
                            queryVarNamesWithLiteralVals.Add(kvp.Key, 0);

            // now we have set of possibilities:
            // further refine to only those for whom all templates have literals
            Dictionary<string, byte> queryVarNamesAllLiterals = new Dictionary<string, byte>(queryVarNamesWithLiteralVals);
            for (int i = a; i < b; ++i)
                foreach (string s in queryVarNamesWithLiteralVals.Keys)
                    if (!array[i].queries.ContainsKey(s) || (array[i].queries[s].Nature != UriTemplatePartType.Literal))
                        queryVarNamesAllLiterals.Remove(s);

            queryVarNamesWithLiteralVals = null; // ensure we don't reference this variable any more

            // now we have the set of names that every operation has as a literal
            if (queryVarNamesAllLiterals.Count == 0)
            {
                if (allowDuplicateEquivalentUriTemplates && AllTemplatesAreEquivalent(array, a, b))
                {
                    // we're ok, do nothing
                }
                else
                    throw new InvalidOperationException(string.Format("Other ambiguous queries; {0}", array[a].ToString()));
            }

            // now just ensure that each template has a unique tuple of values for the names
            string[][] upsLits = new string[b - a][];
            for (int i = 0; i < b - a; ++i)
                upsLits[i] = GetQueryLiterals(array[i + a], queryVarNamesAllLiterals);

            for (int i = 0; i < b - a; ++i)
            {
                for (int j = i + 1; j < b - a; ++j)
                {
                    if (Same(upsLits[i], upsLits[j]))
                    {
                        if (!array[i + a].IsEquivalentTo(array[j + a]))
                            throw new InvalidOperationException(string.Format("Template contains duplicate equivalent ambiguous queries; {0} {1}", array[a + i].ToString(), array[j + a].ToString()));

                        if (!allowDuplicateEquivalentUriTemplates)
                            throw new InvalidOperationException(string.Format("Duplicate equivalent templates; {0} {1}", array[a + i].ToString(), array[j + a].ToString()));
                    }
                }
            }

            // we're good.  whew!
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="up"></param>
        /// <param name="queryVarNames"></param>
        /// <returns></returns>
        private static string[] GetQueryLiterals(UriTemplate up, Dictionary<string, byte> queryVarNames)
        {
            string[] queryLitVals = new string[queryVarNames.Count];
            int i = 0;
            foreach (string queryVarName in queryVarNames.Keys)
            {
                UriTemplateQueryValue utqv = up.queries[queryVarName];
                if (utqv == UriTemplateQueryValue.Empty)
                    queryLitVals[i] = null;
                else
                    queryLitVals[i] = ((UriTemplateLiteralQueryValue)(utqv)).AsRawUnescapedString();

                ++i;
            }

            return queryLitVals;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static bool Same(string[] a, string[] b)
        {
            for (int i = 0; i < a.Length; ++i)
                if (a[i] != b[i])
                    return false;

            return true;
        }
    } // internal static class UriTemplateHelpers
} // namespace TridentFramework.RPC
