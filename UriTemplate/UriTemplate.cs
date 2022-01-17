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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Web;

namespace TridentFramework.RPC
{
    /// <summary>
    /// 
    /// </summary>
    internal enum UriTemplatePartType
    {
        /// <summary>
        /// 
        /// </summary>
        Literal,
        /// <summary>
        /// 
        /// </summary>
        Compound,
        /// <summary>
        /// 
        /// </summary>
        Variable
    } // internal enum UriTemplatePartType

    /// <summary>
    /// A class that represents a Uniform Resource Identifier (URI) template.
    /// </summary>
    public class UriTemplate
    {
        internal readonly int firstOptionalSegment;

        internal readonly string originalTemplate;
        internal readonly Dictionary<string, UriTemplateQueryValue> queries; // keys are original case specified in UriTemplate constructor, dictionary ignores case
        internal readonly List<UriTemplatePathSegment> segments;
        internal const string WildcardPath = "*";
        private readonly Dictionary<string, string> additionalDefaults; // keys are original case specified in UriTemplate constructor, dictionary ignores case
        private readonly string fragment;

        private readonly bool ignoreTrailingSlash;

        private const string NullableDefault = "null";
        private readonly WildcardInfo wildcard;
        private IDictionary<string, string> defaults;
        private ConcurrentDictionary<string, string> unescapedDefaults;

        private VariablesCollection variables;

        /*
        ** Classes
        */

        /// <summary>
        /// 
        /// </summary>
        private struct BindInformation
        {
            private IDictionary<string, string> additionalParameters;
            private int lastNonDefaultPathParameter;
            private int lastNonNullablePathParameter;
            private string[] normalizedParameters;

            /*
            ** Properties
            */

            /// <summary>
            /// 
            /// </summary>
            public IDictionary<string, string> AdditionalParameters
            {
                get => this.additionalParameters;
            }
            
            /// <summary>
            /// 
            /// </summary>
            public int LastNonDefaultPathParameter
            {
                get => this.lastNonDefaultPathParameter;
            }

            /// <summary>
            /// 
            /// </summary>
            public int LastNonNullablePathParameter
            {
                get => this.lastNonNullablePathParameter;
            }

            /// <summary>
            /// 
            /// </summary>
            public string[] NormalizedParameters
            {
                get => this.normalizedParameters;
            }

            /*
            ** Methods
            */

            /// <summary>
            /// Initializes a new instance of the <see cref="BindInformation"/> struct.
            /// </summary>
            /// <param name="normalizedParameters"></param>
            /// <param name="lastNonDefaultPathParameter"></param>
            /// <param name="lastNonNullablePathParameter"></param>
            /// <param name="additionalParameters"></param>
            public BindInformation(string[] normalizedParameters, int lastNonDefaultPathParameter,
                int lastNonNullablePathParameter, IDictionary<string, string> additionalParameters)
            {
                this.normalizedParameters = normalizedParameters;
                this.lastNonDefaultPathParameter = lastNonDefaultPathParameter;
                this.lastNonNullablePathParameter = lastNonNullablePathParameter;
                this.additionalParameters = additionalParameters;
            }
            /// <summary>
            /// Initializes a new instance of the <see cref="BindInformation"/> struct.
            /// </summary>
            /// <param name="additionalParameters"></param>
            public BindInformation(IDictionary<string, string> additionalParameters)
            {
                this.normalizedParameters = null;
                this.lastNonDefaultPathParameter = -1;
                this.lastNonNullablePathParameter = -1;
                this.additionalParameters = additionalParameters;
            }
        } // private struct BindInformation

        /// <summary>
        /// 
        /// </summary>
        private class UriTemplateDefaults : IDictionary<string, string>
        {
            private Dictionary<string, string> defaults;
            private ReadOnlyCollection<string> keys;
            private ReadOnlyCollection<string> values;

            /*
            ** Properties
            */

            /// <inheritdoc />
            public int Count
            {
                get => this.defaults.Count;
            }

            /// <inheritdoc />
            public bool IsReadOnly
            {
                get => true;
            }

            /// <inheritdoc />
            public ICollection<string> Keys
            {
                get => this.keys;
            }

            /// <inheritdoc />
            public ICollection<string> Values
            {
                get => this.values;
            }

            /// <inheritdoc />
            public string this[string key]
            {
                get => this.defaults[key];
                set
                {
                    throw new NotSupportedException();
                }
            }

            /*
            ** Methods
            */

            /// <summary>
            /// Initializes a new instance of the <see cref="UriTemplateDefaults"/> class.
            /// </summary>
            /// <param name="template"></param>
            public UriTemplateDefaults(UriTemplate template)
            {
                this.defaults = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if ((template.variables != null) && (template.variables.DefaultValues != null))
                    foreach (KeyValuePair<string, string> kvp in template.variables.DefaultValues)
                        this.defaults.Add(kvp.Key, kvp.Value);

                if (template.additionalDefaults != null)
                    foreach (KeyValuePair<string, string> kvp in template.additionalDefaults)
                        this.defaults.Add(kvp.Key.ToUpperInvariant(), kvp.Value);

                this.keys = new ReadOnlyCollection<string>(new List<string>(this.defaults.Keys));
                this.values = new ReadOnlyCollection<string>(new List<string>(this.defaults.Values));
            }

            /// <inheritdoc />
            public void Add(string key, string value)
            {
                throw new NotSupportedException();
            }

            /// <inheritdoc />
            public void Add(KeyValuePair<string, string> item)
            {
                throw new NotSupportedException();
            }

            /// <inheritdoc />
            public void Clear()
            {
                throw new NotSupportedException();
            }

            /// <inheritdoc />
            public bool Contains(KeyValuePair<string, string> item)
            {
                return (this.defaults as ICollection<KeyValuePair<string, string>>).Contains(item);
            }

            /// <inheritdoc />
            public bool ContainsKey(string key)
            {
                return this.defaults.ContainsKey(key);
            }

            /// <inheritdoc />
            public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
            {
                (this.defaults as ICollection<KeyValuePair<string, string>>).CopyTo(array, arrayIndex);
            }

            /// <inheritdoc />
            public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
            {
                return this.defaults.GetEnumerator();
            }

            /// <inheritdoc />
            public bool Remove(string key)
            {
                throw new NotSupportedException();
            }

            /// <inheritdoc />
            public bool Remove(KeyValuePair<string, string> item)
            {
                throw new NotSupportedException();
            }

            /// <inheritdoc />
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return this.defaults.GetEnumerator();
            }

            /// <inheritdoc />
            public bool TryGetValue(string key, out string value)
            {
                return this.defaults.TryGetValue(key, out value);
            }
        } // private class UriTemplateDefaults : IDictionary<string, string>

        /// <summary>
        /// 
        /// </summary>
        private class VariablesCollection
        {
            private readonly UriTemplate owner;
            private static ReadOnlyCollection<string> emptyStringCollection = null;
            private Dictionary<string, string> defaultValues; // key is the variable name (in uppercase; as appear in the variable names lists)
            private int firstNullablePathVariable;
            private List<string> pathSegmentVariableNames; // ToUpperInvariant, in order they occur in the original template string
            private ReadOnlyCollection<string> pathSegmentVariableNamesSnapshot = null;
            private List<UriTemplatePartType> pathSegmentVariableNature;
            private List<string> queryValueVariableNames; // ToUpperInvariant, in order they occur in the original template string
            private ReadOnlyCollection<string> queryValueVariableNamesSnapshot = null;

            /*
            ** Properties
            */

            /// <summary>
            /// 
            /// </summary>
            public static ReadOnlyCollection<string> EmptyCollection
            {
                get
                {
                    if (emptyStringCollection == null)
                        emptyStringCollection = new ReadOnlyCollection<string>(new List<string>());

                    return emptyStringCollection;
                }
            }

            /// <summary>
            /// 
            /// </summary>
            public Dictionary<string, string> DefaultValues
            {
                get => this.defaultValues;
            }

            /// <summary>
            /// 
            /// </summary>
            public ReadOnlyCollection<string> PathSegmentVariableNames
            {
                get
                {
                    if (this.pathSegmentVariableNamesSnapshot == null)
                        Interlocked.CompareExchange<ReadOnlyCollection<string>>(ref this.pathSegmentVariableNamesSnapshot, new ReadOnlyCollection<string>(
                            this.pathSegmentVariableNames), null);

                    return this.pathSegmentVariableNamesSnapshot;
                }
            }

            /// <summary>
            /// 
            /// </summary>
            public ReadOnlyCollection<string> QueryValueVariableNames
            {
                get
                {
                    if (this.queryValueVariableNamesSnapshot == null)
                        Interlocked.CompareExchange<ReadOnlyCollection<string>>(ref this.queryValueVariableNamesSnapshot, new ReadOnlyCollection<string>(
                            this.queryValueVariableNames), null);

                    return this.queryValueVariableNamesSnapshot;
                }
            }

            /*
            ** Methods
            */

            /// <summary>
            /// Initializes a new instance of the <see cref="VariablesCollection"/> class.
            /// </summary>
            /// <param name="owner"></param>
            public VariablesCollection(UriTemplate owner)
            {
                this.owner = owner;
                this.pathSegmentVariableNames = new List<string>();
                this.pathSegmentVariableNature = new List<UriTemplatePartType>();
                this.queryValueVariableNames = new List<string>();
                this.firstNullablePathVariable = -1;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="varName"></param>
            /// <param name="value"></param>
            public void AddDefaultValue(string varName, string value)
            {
                int varIndex = this.pathSegmentVariableNames.IndexOf(varName);

                if ((this.owner.wildcard != null) && this.owner.wildcard.HasVariable &&
                    (varIndex == this.pathSegmentVariableNames.Count - 1))
                    throw new InvalidOperationException(string.Format("Star variable with defaults from addtional defaults; {0} {1}", owner.originalTemplate, varName));
                if (this.pathSegmentVariableNature[varIndex] != UriTemplatePartType.Variable)
                    throw new InvalidOperationException(string.Format("Default value to compound segment variable from additional defaults; {0} {1}", owner.originalTemplate, varName));

                if (string.IsNullOrEmpty(value) ||
                    (string.Compare(value, UriTemplate.NullableDefault, StringComparison.OrdinalIgnoreCase) == 0))
                    value = null;

                if (this.defaultValues == null)
                    this.defaultValues = new Dictionary<string, string>();

                this.defaultValues.Add(varName, value);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="sourceNature"></param>
            /// <param name="varDeclaration"></param>
            /// <param name="hasDefaultValue"></param>
            /// <returns></returns>
            public string AddPathVariable(UriTemplatePartType sourceNature, string varDeclaration, out bool hasDefaultValue)
            {
                string varName;
                string defaultValue;
                ParseVariableDeclaration(varDeclaration, out varName, out defaultValue);
                hasDefaultValue = (defaultValue != null);
                if (varName.IndexOf(UriTemplate.WildcardPath, StringComparison.Ordinal) != -1)
                    throw new FormatException(string.Format("Invalid wildcard in variable or literal; {0} {1}", owner.originalTemplate, UriTemplate.WildcardPath));

                string uppercaseVarName = varName.ToUpperInvariant();
                if (this.pathSegmentVariableNames.Contains(uppercaseVarName) || this.queryValueVariableNames.Contains(uppercaseVarName))
                    throw new InvalidOperationException(string.Format("Variable names must be unique; {0} {1}", owner.originalTemplate, varName));

                this.pathSegmentVariableNames.Add(uppercaseVarName);
                this.pathSegmentVariableNature.Add(sourceNature);
                if (hasDefaultValue)
                {
                    if (defaultValue == string.Empty)
                        throw new InvalidOperationException(string.Format("Invalid default path value; {0} {1} {2}", owner.originalTemplate, varDeclaration, varName));

                    if (string.Compare(defaultValue, UriTemplate.NullableDefault, StringComparison.OrdinalIgnoreCase) == 0)
                        defaultValue = null;
                    if (this.defaultValues == null)
                        this.defaultValues = new Dictionary<string, string>();

                    this.defaultValues.Add(uppercaseVarName, defaultValue);
                }

                return uppercaseVarName;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="varDeclaration"></param>
            /// <returns></returns>
            public string AddQueryVariable(string varDeclaration)
            {
                string varName;
                string defaultValue;
                ParseVariableDeclaration(varDeclaration, out varName, out defaultValue);
                if (varName.IndexOf(UriTemplate.WildcardPath, StringComparison.Ordinal) != -1)
                    throw new FormatException(string.Format("Invalid wildcard in variable or literal; {0} {1}", owner.originalTemplate, UriTemplate.WildcardPath));

                if (defaultValue != null)
                    throw new InvalidOperationException(string.Format("Default value to query variable; {0} {1} {2}", owner.originalTemplate, varDeclaration, varName));

                string uppercaseVarName = varName.ToUpperInvariant();
                if (this.pathSegmentVariableNames.Contains(uppercaseVarName) || this.queryValueVariableNames.Contains(uppercaseVarName))
                    throw new InvalidOperationException(string.Format("Variable names must be unqiue; {0} {1}", owner.originalTemplate, varName));

                this.queryValueVariableNames.Add(uppercaseVarName);
                return uppercaseVarName;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="varName"></param>
            /// <param name="boundParameters"></param>
            public void LookupDefault(string varName, NameValueCollection boundParameters)
            {
                boundParameters.Add(varName, owner.UnescapeDefaultValue(this.defaultValues[varName]));
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="parameters"></param>
            /// <param name="omitDefaults"></param>
            /// <returns></returns>
            public BindInformation PrepareBindInformation(IDictionary<string, string> parameters, bool omitDefaults)
            {
                if (parameters == null)
                    throw new ArgumentNullException("parameters");

                string[] normalizedParameters = PrepareNormalizedParameters();
                IDictionary<string, string> extraParameters = null;
                foreach (string key in parameters.Keys)
                    ProcessBindParameter(key, parameters[key], normalizedParameters, ref extraParameters);

                BindInformation bindInfo;
                ProcessDefaultsAndCreateBindInfo(omitDefaults, normalizedParameters, extraParameters, out bindInfo);
                return bindInfo;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="parameters"></param>
            /// <param name="omitDefaults"></param>
            /// <returns></returns>
            public BindInformation PrepareBindInformation(NameValueCollection parameters, bool omitDefaults)
            {
                if (parameters == null)
                    throw new ArgumentNullException("parameters");

                string[] normalizedParameters = PrepareNormalizedParameters();
                IDictionary<string, string> extraParameters = null;
                foreach (string key in parameters.AllKeys)
                    ProcessBindParameter(key, parameters[key], normalizedParameters, ref extraParameters);

                BindInformation bindInfo;
                ProcessDefaultsAndCreateBindInfo(omitDefaults, normalizedParameters, extraParameters, out bindInfo);
                return bindInfo;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="parameters"></param>
            /// <returns></returns>
            public BindInformation PrepareBindInformation(params string[] parameters)
            {
                if (parameters == null)
                    throw new ArgumentNullException("parameters");

                if ((parameters.Length < this.pathSegmentVariableNames.Count) ||
                    (parameters.Length > this.pathSegmentVariableNames.Count + this.queryValueVariableNames.Count))
                    throw new FormatException(string.Format("Bind by position wrong count; {0} {1} {2} {3}", owner.originalTemplate, pathSegmentVariableNames.Count, queryValueVariableNames.Count, parameters.Length));

                string[] normalizedParameters;
                if (parameters.Length == this.pathSegmentVariableNames.Count + this.queryValueVariableNames.Count)
                    normalizedParameters = parameters;
                else
                {
                    normalizedParameters = new string[this.pathSegmentVariableNames.Count + this.queryValueVariableNames.Count];
                    parameters.CopyTo(normalizedParameters, 0);
                    for (int i = parameters.Length; i < normalizedParameters.Length; i++)
                        normalizedParameters[i] = null;
                }

                int lastNonDefaultPathParameter;
                int lastNonNullablePathParameter;

                LoadDefaultsAndValidate(normalizedParameters, out lastNonDefaultPathParameter,
                    out lastNonNullablePathParameter);
                return new BindInformation(normalizedParameters, lastNonDefaultPathParameter,
                    lastNonNullablePathParameter, this.owner.additionalDefaults);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="firstOptionalSegment"></param>
            public void ValidateDefaults(out int firstOptionalSegment)
            {
                // Finding the first valid nullable defaults
                for (int i = this.pathSegmentVariableNames.Count - 1; (i >= 0) && (this.firstNullablePathVariable == -1); i--)
                {
                    string varName = this.pathSegmentVariableNames[i];
                    string defaultValue;
                    if (!this.defaultValues.TryGetValue(varName, out defaultValue))
                        this.firstNullablePathVariable = i + 1;
                    else if (defaultValue != null)
                        this.firstNullablePathVariable = i + 1;
                }

                if (this.firstNullablePathVariable == -1)
                    this.firstNullablePathVariable = 0;

                // Making sure that there are no nullables to the left of the first valid nullable
                if (this.firstNullablePathVariable > 1)
                {
                    for (int i = this.firstNullablePathVariable - 2; i >= 0; i--)
                    {
                        string varName = this.pathSegmentVariableNames[i];
                        string defaultValue;
                        if (this.defaultValues.TryGetValue(varName, out defaultValue))
                        {
                            if (defaultValue == null)
                                throw new InvalidOperationException(string.Format("Nullable default must be followed with nullables; {0} {1} {2}", owner.originalTemplate, varName, pathSegmentVariableNames[i + 1]));
                        }
                    }
                }

                // Making sure that there are no Literals\WildCards to the right
                // Based on the fact that only Variable Path Segments support default values,
                //  if firstNullablePathVariable=N and pathSegmentVariableNames.Count=M then
                //  the nature of the last M-N path segments should be StringNature.Variable; otherwise,
                //  there was a literal segment in between. Also, there shouldn't be a wildcard.
                if (this.firstNullablePathVariable < this.pathSegmentVariableNames.Count)
                {
                    if (this.owner.HasWildcard)
                        throw new InvalidOperationException(string.Format("Nullable default must not be followed with wildcard; {0} {1}", owner.originalTemplate, pathSegmentVariableNames[firstNullablePathVariable]));

                    for (int i = this.pathSegmentVariableNames.Count - 1; i >= this.firstNullablePathVariable; i--)
                    {
                        int segmentIndex = this.owner.segments.Count - (this.pathSegmentVariableNames.Count - i);
                        if (this.owner.segments[segmentIndex].Nature != UriTemplatePartType.Variable)
                            throw new InvalidOperationException(string.Format("Nullable default must not be followed with literal; {0} {1} {2}", owner.originalTemplate, pathSegmentVariableNames[firstNullablePathVariable],
                                owner.segments[segmentIndex].OriginalSegment));
                    }
                }

                // Now that we have the firstNullablePathVariable set, lets calculate the firstOptionalSegment.
                //  We already knows that the last M-N path segments (when M=pathSegmentVariableNames.Count and
                //  N=firstNullablePathVariable) are optional (see the previos comment). We will start there and
                //  move to the left, stopping at the first segment, which is not a variable or is a variable
                //  and doesn't have a default value.
                int numNullablePathVariables = (this.pathSegmentVariableNames.Count - this.firstNullablePathVariable);
                firstOptionalSegment = this.owner.segments.Count - numNullablePathVariables;
                if (!this.owner.HasWildcard)
                {
                    while (firstOptionalSegment > 0)
                    {
                        UriTemplatePathSegment ps = this.owner.segments[firstOptionalSegment - 1];
                        if (ps.Nature != UriTemplatePartType.Variable)
                            break;

                        UriTemplateVariablePathSegment vps = (ps as UriTemplateVariablePathSegment);
                        if (!this.defaultValues.ContainsKey(vps.VarName))
                            break;

                        firstOptionalSegment--;
                    }
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="extraParameters"></param>
            private void AddAdditionalDefaults(ref IDictionary<string, string> extraParameters)
            {
                if (extraParameters == null)
                    extraParameters = this.owner.additionalDefaults;
                else
                {
                    foreach (KeyValuePair<string, string> kvp in this.owner.additionalDefaults)
                        if (!extraParameters.ContainsKey(kvp.Key))
                            extraParameters.Add(kvp.Key, kvp.Value);
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="normalizedParameters"></param>
            /// <param name="lastNonDefaultPathParameter"></param>
            /// <param name="lastNonNullablePathParameter"></param>
            private void LoadDefaultsAndValidate(string[] normalizedParameters, out int lastNonDefaultPathParameter,
                out int lastNonNullablePathParameter)
            {
                // First step - loading defaults
                for (int i = 0; i < this.pathSegmentVariableNames.Count; i++)
                    if (string.IsNullOrEmpty(normalizedParameters[i]) && (this.defaultValues != null))
                        this.defaultValues.TryGetValue(this.pathSegmentVariableNames[i], out normalizedParameters[i]);

                // Second step - calculating bind constrains
                lastNonDefaultPathParameter = this.pathSegmentVariableNames.Count - 1;
                if ((this.defaultValues != null) &&
                    (this.owner.segments[this.owner.segments.Count - 1].Nature != UriTemplatePartType.Literal))
                {
                    bool foundNonDefaultPathParameter = false;
                    while (!foundNonDefaultPathParameter && (lastNonDefaultPathParameter >= 0))
                    {
                        string defaultValue;
                        if (this.defaultValues.TryGetValue(this.pathSegmentVariableNames[lastNonDefaultPathParameter],
                            out defaultValue))
                        {
                            if (string.Compare(normalizedParameters[lastNonDefaultPathParameter],
                                defaultValue, StringComparison.Ordinal) != 0)
                                foundNonDefaultPathParameter = true;
                            else
                                lastNonDefaultPathParameter--;
                        }
                        else
                            foundNonDefaultPathParameter = true;
                    }
                }

                if (this.firstNullablePathVariable > lastNonDefaultPathParameter)
                    lastNonNullablePathParameter = this.firstNullablePathVariable - 1;
                else
                    lastNonNullablePathParameter = lastNonDefaultPathParameter;

                // Third step - validate
                for (int i = 0; i <= lastNonNullablePathParameter; i++)
                {
                    // Skip validation for terminating star variable segment :
                    if (this.owner.HasWildcard && this.owner.wildcard.HasVariable &&
                        (i == this.pathSegmentVariableNames.Count - 1))
                        continue;

                    // Validate
                    if (string.IsNullOrEmpty(normalizedParameters[i]))
                        throw new ArgumentException(string.Format("Bind UriTemplate to null or empty path parameter; {0}", pathSegmentVariableNames[i]), "parameters");
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="varDeclaration"></param>
            /// <param name="varName"></param>
            /// <param name="defaultValue"></param>
            private void ParseVariableDeclaration(string varDeclaration, out string varName, out string defaultValue)
            {
                if ((varDeclaration.IndexOf('{') != -1) || (varDeclaration.IndexOf('}') != -1))
                    throw new FormatException(string.Format("Invalid variable declaration; {0} {1}", owner.originalTemplate, varDeclaration));

                int equalSignIndex = varDeclaration.IndexOf('=');
                switch (equalSignIndex)
                {
                    case -1:
                        varName = varDeclaration;
                        defaultValue = null;
                        break;

                    case 0:
                        throw new FormatException(string.Format("Invalid variable declaration; {0} {1}", owner.originalTemplate, varDeclaration));

                    default:
                        varName = varDeclaration.Substring(0, equalSignIndex);
                        defaultValue = varDeclaration.Substring(equalSignIndex + 1);
                        if (defaultValue.IndexOf('=') != -1)
                            throw new FormatException(string.Format("Invalid variable declaration; {0} {1}", owner.originalTemplate, varDeclaration));
                        break;
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            private string[] PrepareNormalizedParameters()
            {
                string[] normalizedParameters = new string[this.pathSegmentVariableNames.Count + this.queryValueVariableNames.Count];
                for (int i = 0; i < normalizedParameters.Length; i++)
                    normalizedParameters[i] = null;

                return normalizedParameters;
            }

            private void ProcessBindParameter(string name, string value, string[] normalizedParameters,
                ref IDictionary<string, string> extraParameters)
            {
                if (string.IsNullOrEmpty(name))
                    throw new ArgumentException("Bind by name called with empty key", "parameters");

                string uppercaseVarName = name.ToUpperInvariant();
                int pathVarIndex = this.pathSegmentVariableNames.IndexOf(uppercaseVarName);
                if (pathVarIndex != -1)
                {
                    normalizedParameters[pathVarIndex] = (string.IsNullOrEmpty(value) ? string.Empty : value);
                    return;
                }

                int queryVarIndex = this.queryValueVariableNames.IndexOf(uppercaseVarName);
                if (queryVarIndex != -1)
                {
                    normalizedParameters[this.pathSegmentVariableNames.Count + queryVarIndex] = (string.IsNullOrEmpty(value) ? string.Empty : value);
                    return;
                }

                if (extraParameters == null)
                    extraParameters = new Dictionary<string, string>(UriTemplateHelpers.GetQueryKeyComparer());

                extraParameters.Add(name, value);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="omitDefaults"></param>
            /// <param name="normalizedParameters"></param>
            /// <param name="extraParameters"></param>
            /// <param name="bindInfo"></param>
            private void ProcessDefaultsAndCreateBindInfo(bool omitDefaults, string[] normalizedParameters,
                IDictionary<string, string> extraParameters, out BindInformation bindInfo)
            {
                int lastNonDefaultPathParameter;
                int lastNonNullablePathParameter;
                LoadDefaultsAndValidate(normalizedParameters, out lastNonDefaultPathParameter,
                    out lastNonNullablePathParameter);
                if (this.owner.additionalDefaults != null)
                {
                    if (omitDefaults)
                        RemoveAdditionalDefaults(ref extraParameters);
                    else
                        AddAdditionalDefaults(ref extraParameters);
                }

                bindInfo = new BindInformation(normalizedParameters, lastNonDefaultPathParameter,
                    lastNonNullablePathParameter, extraParameters);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="extraParameters"></param>
            private void RemoveAdditionalDefaults(ref IDictionary<string, string> extraParameters)
            {
                if (extraParameters == null)
                    return;

                foreach (KeyValuePair<string, string> kvp in this.owner.additionalDefaults)
                {
                    string extraParameter;
                    if (extraParameters.TryGetValue(kvp.Key, out extraParameter))
                        if (string.Compare(extraParameter, kvp.Value, StringComparison.Ordinal) == 0)
                            extraParameters.Remove(kvp.Key);
                }

                if (extraParameters.Count == 0)
                    extraParameters = null;
            }
        } // private class VariablesCollection

        /// <summary>
        /// 
        /// </summary>
        private class WildcardInfo
        {
            private readonly UriTemplate owner;
            private readonly string varName;

            /*
            ** Properties
            */

            /// <summary>
            /// 
            /// </summary>
            internal bool HasVariable
            {
                get => (!string.IsNullOrEmpty(this.varName));
            }

            /*
            ** Methods
            */

            /// <summary>
            /// Initializes a new instance of the <see cref="WildcardInfo"/> class.
            /// </summary>
            /// <param name="owner"></param>
            public WildcardInfo(UriTemplate owner)
            {
                this.varName = null;
                this.owner = owner;
            }
            /// <summary>
            /// Initializes a new instance of the <see cref="WildcardInfo"/> class.
            /// </summary>
            /// <param name="owner"></param>
            /// <param name="segment"></param>
            public WildcardInfo(UriTemplate owner, string segment)
            {
                bool hasDefault;
                this.varName = owner.AddPathVariable(UriTemplatePartType.Variable,
                    segment.Substring(1 + WildcardPath.Length, segment.Length - 2 - WildcardPath.Length),
                    out hasDefault);

                // Since this is a terminating star segment there shouldn't be a default
                if (hasDefault)
                    throw new InvalidOperationException(string.Format("Star variable with defaults; {0} {1} {2}", owner.originalTemplate, segment, varName));

                this.owner = owner;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="values"></param>
            /// <param name="valueIndex"></param>
            /// <param name="path"></param>
            public void Bind(string[] values, ref int valueIndex, StringBuilder path)
            {
                if (HasVariable)
                {
                    if (string.IsNullOrEmpty(values[valueIndex]))
                        valueIndex++;
                    else
                        path.Append(values[valueIndex++]);
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="numMatchedSegments"></param>
            /// <param name="relativePathSegments"></param>
            /// <param name="boundParameters"></param>
            public void Lookup(int numMatchedSegments, Collection<string> relativePathSegments,
                NameValueCollection boundParameters)
            {
                if (HasVariable)
                {
                    StringBuilder remainingPath = new StringBuilder();
                    for (int i = numMatchedSegments; i < relativePathSegments.Count; i++)
                    {
                        if (i < relativePathSegments.Count - 1)
                            remainingPath.AppendFormat("{0}/", relativePathSegments[i]);
                        else
                            remainingPath.Append(relativePathSegments[i]);
                    }

                    boundParameters.Add(this.varName, remainingPath.ToString());
                }
            }
        } // private class WildcardInfo

        /*
        ** Properties
        */

        /// <summary>
        /// Gets a collection of name/value pairs for any default parameter values.
        /// </summary>
        public IDictionary<string, string> Defaults
        {
            get
            {
                if (this.defaults == null)
                    Interlocked.CompareExchange<IDictionary<string, string>>(ref this.defaults, new UriTemplateDefaults(this), null);
                return this.defaults;
            }
        }

        /// <summary>
        /// Specifies whether trailing slashes "/" in the template should be ignored when matching candidate URIs.
        /// </summary>
        public bool IgnoreTrailingSlash
        {
            get { return this.ignoreTrailingSlash; }
        }

        /// <summary>
        /// Gets a collection of variable names used within path segments in the template.
        /// </summary>
        public ReadOnlyCollection<string> PathSegmentVariableNames
        {
            get
            {
                if (this.variables == null)
                    return VariablesCollection.EmptyCollection;
                else
                    return this.variables.PathSegmentVariableNames;
            }
        }

        /// <summary>
        /// Gets a collection of variable names used within the query string in the template.
        /// </summary>
        public ReadOnlyCollection<string> QueryValueVariableNames
        {
            get
            {
                if (this.variables == null)
                    return VariablesCollection.EmptyCollection;
                else
                    return this.variables.QueryValueVariableNames;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        internal bool HasNoVariables
        {
            get { return (this.variables == null); }
        }

        /// <summary>
        /// 
        /// </summary>
        internal bool HasWildcard
        {
            get { return (this.wildcard != null); }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="UriTemplate"/> class.
        /// </summary>
        /// <param name="template">The template string.</param>
        public UriTemplate(string template)
            : this(template, false)
        {
            /* stub */
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="UriTemplate"/> class.
        /// </summary>
        /// <param name="template">The template string.</param>
        /// <param name="ignoreTrailingSlash">A value that specifies whether trailing slash "/" characters should be ignored.</param>
        public UriTemplate(string template, bool ignoreTrailingSlash)
            : this(template, ignoreTrailingSlash, null)
        {
            /* stub */
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="UriTemplate"/> class.
        /// </summary>
        /// <param name="template">The template string.</param>
        /// <param name="additionalDefaults">A dictionary that contains a list of default values for the template parameters.</param>
        public UriTemplate(string template, IDictionary<string, string> additionalDefaults)
            : this(template, false, additionalDefaults)
        {
            /* stub */
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="UriTemplate"/> class.
        /// </summary>
        /// <param name="template">The template string.</param>
        /// <param name="ignoreTrailingSlash">A value that specifies whether trailing slash "/" characters should be ignored.</param>
        /// <param name="additionalDefaults">A dictionary that contains a list of default values for the template parameters.</param>
        public UriTemplate(string template, bool ignoreTrailingSlash, IDictionary<string, string> additionalDefaults)
        {
            if (template == null)
                throw new ArgumentNullException("template");

            this.originalTemplate = template;
            this.ignoreTrailingSlash = ignoreTrailingSlash;
            this.segments = new List<UriTemplatePathSegment>();
            this.queries = new Dictionary<string, UriTemplateQueryValue>(StringComparer.OrdinalIgnoreCase);

            // parse it
            string pathTemplate;
            string queryTemplate;

            // ignore a leading slash
            if (template.StartsWith("/", StringComparison.Ordinal))
                template = template.Substring(1);

            // pull out fragment
            int fragmentStart = template.IndexOf('#');
            if (fragmentStart == -1)
                this.fragment = "";
            else
            {
                this.fragment = template.Substring(fragmentStart + 1);
                template = template.Substring(0, fragmentStart);
            }

            // pull out path and query
            int queryStart = template.IndexOf('?');
            if (queryStart == -1)
            {
                queryTemplate = string.Empty;
                pathTemplate = template;
            }
            else
            {
                queryTemplate = template.Substring(queryStart + 1);
                pathTemplate = template.Substring(0, queryStart);
            }
            
            template = null; // to ensure we don't accidentally reference this variable any more

            // setup path template and validate
            if (!string.IsNullOrEmpty(pathTemplate))
            {
                int startIndex = 0;
                while (startIndex < pathTemplate.Length)
                {
                    // Identify the next segment
                    int endIndex = pathTemplate.IndexOf('/', startIndex);
                    string segment;
                    if (endIndex != -1)
                    {
                        segment = pathTemplate.Substring(startIndex, endIndex + 1 - startIndex);
                        startIndex = endIndex + 1;
                    }
                    else
                    {
                        segment = pathTemplate.Substring(startIndex);
                        startIndex = pathTemplate.Length;
                    }
                    
                    // Checking for wildcard segment ("*") or ("{*<var name>}")
                    UriTemplatePartType wildcardType;
                    if ((startIndex == pathTemplate.Length) &&
                        UriTemplateHelpers.IsWildcardSegment(segment, out wildcardType))
                    {
                        switch (wildcardType)
                        {
                            case UriTemplatePartType.Literal:
                                this.wildcard = new WildcardInfo(this);
                                break;

                            case UriTemplatePartType.Variable:
                                this.wildcard = new WildcardInfo(this, segment);
                                break;

                            default:
                                break;
                        }
                    }
                    else
                        this.segments.Add(UriTemplatePathSegment.CreateFromUriTemplate(segment, this));
                }
            }

            // setup query template and validate
            if (!string.IsNullOrEmpty(queryTemplate))
            {
                int startIndex = 0;
                while (startIndex < queryTemplate.Length)
                {
                    // Identify the next query part
                    int endIndex = queryTemplate.IndexOf('&', startIndex);
                    int queryPartStart = startIndex;
                    int queryPartEnd;
                    if (endIndex != -1)
                    {
                        queryPartEnd = endIndex;
                        startIndex = endIndex + 1;
                        if (startIndex >= queryTemplate.Length)
                            throw new InvalidOperationException(string.Format("Query cannot end in an ampersand; {0}", originalTemplate));
                    }
                    else
                    {
                        queryPartEnd = queryTemplate.Length;
                        startIndex = queryTemplate.Length;
                    }

                    // Checking query part type; identifying key and value
                    int equalSignIndex = queryTemplate.IndexOf('=', queryPartStart, queryPartEnd - queryPartStart);
                    string key;
                    string value;
                    if (equalSignIndex >= 0)
                    {
                        key = queryTemplate.Substring(queryPartStart, equalSignIndex - queryPartStart);
                        value = queryTemplate.Substring(equalSignIndex + 1, queryPartEnd - equalSignIndex - 1);
                    }
                    else
                    {
                        key = queryTemplate.Substring(queryPartStart, queryPartEnd - queryPartStart);
                        value = null;
                    }

                    if (string.IsNullOrEmpty(key))
                        throw new InvalidOperationException(string.Format("Query cannot have empty names; {0}", originalTemplate));

                    if (UriTemplateHelpers.IdentifyPartType(key) != UriTemplatePartType.Literal)
                        throw new ArgumentException(string.Format("Query must have literal names; {0}", originalTemplate), "template");

                    // Adding a new entry to the queries dictionary
                    key = HttpUtility.UrlDecode(key, Encoding.UTF8);
                    if (this.queries.ContainsKey(key))
                        throw new InvalidOperationException(string.Format("Query names must be unique; {0}", originalTemplate));

                    this.queries.Add(key, UriTemplateQueryValue.CreateFromUriTemplate(value, this));
                }
            }

            // Process additional defaults (if has some) :
            if (additionalDefaults != null)
            {
                if (this.variables == null)
                {
                    if (additionalDefaults.Count > 0)
                        this.additionalDefaults = new Dictionary<string, string>(additionalDefaults, StringComparer.OrdinalIgnoreCase);
                }
                else
                {
                    foreach (KeyValuePair<string, string> kvp in additionalDefaults)
                    {
                        string uppercaseKey = kvp.Key.ToUpperInvariant();
                        if ((this.variables.DefaultValues != null) && this.variables.DefaultValues.ContainsKey(uppercaseKey))
                            throw new ArgumentException(string.Format("Addtional default is invalid; {0} {1}", kvp.Key, originalTemplate), "additionalDefaults");

                        if (this.variables.PathSegmentVariableNames.Contains(uppercaseKey))
                            this.variables.AddDefaultValue(uppercaseKey, kvp.Value);
                        else if (this.variables.QueryValueVariableNames.Contains(uppercaseKey))
                            throw new InvalidOperationException(string.Format("Default value to query variable from addtional defaults; {0} {1}", originalTemplate, uppercaseKey));
                        else if (string.Compare(kvp.Value, UriTemplate.NullableDefault, StringComparison.OrdinalIgnoreCase) == 0)
                            throw new InvalidOperationException(string.Format("Nullable default at additional defaults; {0} {1}", originalTemplate, uppercaseKey));
                        else
                        {
                            if (this.additionalDefaults == null)
                                this.additionalDefaults = new Dictionary<string, string>(additionalDefaults.Count, StringComparer.OrdinalIgnoreCase);

                            this.additionalDefaults.Add(kvp.Key, kvp.Value);
                        }
                    }
                }
            }

            // Validate defaults (if should)
            if ((this.variables != null) && (this.variables.DefaultValues != null))
                this.variables.ValidateDefaults(out this.firstOptionalSegment);
            else
                this.firstOptionalSegment = this.segments.Count;
        }

        /// <summary>
        /// Creates a new URI from the template and the collection of parameters.
        /// </summary>
        /// <param name="baseAddress">The base address.</param>
        /// <param name="parameters">A dictionary that contains a collection of parameter name/value pairs.</param>
        /// <returns>A URI.</returns>
        public Uri BindByName(Uri baseAddress, IDictionary<string, string> parameters)
        {
            return BindByName(baseAddress, parameters, false);
        }

        /// <summary>
        /// Creates a new URI from the template and the collection of parameters.
        /// </summary>
        /// <param name="baseAddress">The base address.</param>
        /// <param name="parameters">A dictionary that contains a collection of parameter name/value pairs.</param>
        /// <param name="omitDefaults">true is the default values are ignored; otherwise false.</param>
        /// <returns>A URI.</returns>
        public Uri BindByName(Uri baseAddress, IDictionary<string, string> parameters, bool omitDefaults)
        {
            if (baseAddress == null)
                throw new ArgumentNullException("baseAddress");
            if (!baseAddress.IsAbsoluteUri)
                throw new ArgumentException(string.Format("Bad base address"), "baseAddress");

            BindInformation bindInfo;
            if (this.variables == null)
                bindInfo = PrepareBindInformation(parameters, omitDefaults);
            else
                bindInfo = this.variables.PrepareBindInformation(parameters, omitDefaults);

            return Bind(baseAddress, bindInfo, omitDefaults);
        }

        /// <summary>
        /// Creates a new URI from the template and the collection of parameters.
        /// </summary>
        /// <param name="baseAddress">The base address.</param>
        /// <param name="parameters">The parameter values.</param>
        /// <returns>A URI.</returns>
        public Uri BindByName(Uri baseAddress, NameValueCollection parameters)
        {
            return BindByName(baseAddress, parameters, false);
        }
        public Uri BindByName(Uri baseAddress, NameValueCollection parameters, bool omitDefaults)
        {
            if (baseAddress == null)
                throw new ArgumentNullException("baseAddress");
            if (!baseAddress.IsAbsoluteUri)
                throw new ArgumentException(string.Format("Bad base address"), "baseAddress");

            BindInformation bindInfo;
            if (this.variables == null)
                bindInfo = PrepareBindInformation(parameters, omitDefaults);
            else
                bindInfo = this.variables.PrepareBindInformation(parameters, omitDefaults);

            return Bind(baseAddress, bindInfo, omitDefaults);
        }

        /// <summary>
        /// Creates a new URI from the template and an array of parameter values.
        /// </summary>
        /// <param name="baseAddress">The base address.</param>
        /// <param name="values">The parameter values.</param>
        /// <returns>A URI.</returns>
        public Uri BindByPosition(Uri baseAddress, params string[] values)
        {
            if (baseAddress == null)
                throw new ArgumentNullException("baseAddress");
            if (!baseAddress.IsAbsoluteUri)
                throw new ArgumentException(string.Format("Bad base address"), "baseAddress");

            BindInformation bindInfo;
            if (this.variables == null)
            {
                if (values.Length > 0)
                    throw new FormatException(string.Format("Bind by position with no variables; {0} {1}", originalTemplate, values.Length));
                bindInfo = new BindInformation(this.additionalDefaults);
            }
            else
                bindInfo = this.variables.PrepareBindInformation(values);

            return Bind(baseAddress, bindInfo, false);
        }

        // A note about UriTemplate equivalency:
        //  The introduction of defaults and, more over, terminal defaults, broke the simple
        //  intuative notion of equivalency between templates. We will define equivalent
        //  templates as such based on the structure of them and not based on the set of uri
        //  that are matched by them. The result is that, even though they do not match the
        //  same set of uri's, the following templates are equivalent:
        //      - "/foo/{bar}"
        //      - "/foo/{bar=xyz}"
        //  A direct result from the support for 'terminal defaults' is that the IsPathEquivalentTo
        //  method, which was used both to determine the equivalence between templates, as 
        //  well as verify that all the templates, combined together in the same PathEquivalentSet, 
        //  are equivalent in thier path is no longer valid for both purposes. We will break 
        //  it to two distinct methods, each will be called in a different case.
        /// <summary>
        /// Indicates whether a <see cref="UriTemplate"/> is structurally equivalent to another.
        /// </summary>
        /// <param name="other">The UriTemplate to compare.</param>
        /// <returns>true if the UriTemplate is structurally equivalent to another; otherwise false.</returns>
        public bool IsEquivalentTo(UriTemplate other)
        {
            if (other == null)
                return false;

            if (other.segments == null || other.queries == null)
            {
                // they never are null, but PreSharp is complaining, 
                // and warning suppression isn't working
                return false;
            }

            if (!IsPathFullyEquivalent(other))
                return false;

            if (!IsQueryEquivalent(other))
                return false;

            return true;
        }

        /// <summary>
        /// Attempts to match a Uri to a UriTemplate.
        /// </summary>
        /// <param name="baseAddress">The base address.</param>
        /// <param name="candidate">The Uri to match against the template.</param>
        /// <returns>An instance.</returns>
        public UriTemplateMatch Match(Uri baseAddress, Uri candidate)
        {
            if (baseAddress == null)
                throw new ArgumentNullException("baseAddress");
            if (!baseAddress.IsAbsoluteUri)
                throw new ArgumentException(string.Format("Bad base address"), "baseAddress");
            if (candidate == null)
                throw new ArgumentNullException("candidate");

            // ensure that the candidate is 'under' the base address
            if (!candidate.IsAbsoluteUri)
                return null;

            string basePath = UriTemplateHelpers.GetUriPath(baseAddress);
            string candidatePath = UriTemplateHelpers.GetUriPath(candidate);
            if (candidatePath.Length < basePath.Length)
                return null;

            if (!candidatePath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
                return null;

            // Identifying the relative segments \ checking matching to the path :
            int numSegmentsInBaseAddress = baseAddress.Segments.Length;
            string[] candidateSegments = candidate.Segments;
            int numMatchedSegments;
            Collection<string> relativeCandidateSegments;
            if (!IsCandidatePathMatch(numSegmentsInBaseAddress, candidateSegments,
                out numMatchedSegments, out relativeCandidateSegments))
                return null;

            // Checking matching to the query (if should) :
            NameValueCollection candidateQuery = null;
            if (!UriTemplateHelpers.CanMatchQueryTrivially(this))
            {
                candidateQuery = UriTemplateHelpers.ParseQueryString(candidate.Query);
                if (!UriTemplateHelpers.CanMatchQueryInterestingly(this, candidateQuery, false))
                    return null;
            }

            // We matched; lets build the UriTemplateMatch
            return CreateUriTemplateMatch(baseAddress, candidate, null, numMatchedSegments,
                relativeCandidateSegments, candidateQuery);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return this.originalTemplate;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourceNature"></param>
        /// <param name="varDeclaration"></param>
        /// <returns></returns>
        internal string AddPathVariable(UriTemplatePartType sourceNature, string varDeclaration)
        {
            bool hasDefaultValue;
            return AddPathVariable(sourceNature, varDeclaration, out hasDefaultValue);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourceNature"></param>
        /// <param name="varDeclaration"></param>
        /// <param name="hasDefaultValue"></param>
        /// <returns></returns>
        internal string AddPathVariable(UriTemplatePartType sourceNature, string varDeclaration,
            out bool hasDefaultValue)
        {
            if (this.variables == null)
            {
                this.variables = new VariablesCollection(this);
            }
            return this.variables.AddPathVariable(sourceNature, varDeclaration, out hasDefaultValue);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="varDeclaration"></param>
        /// <returns></returns>
        internal string AddQueryVariable(string varDeclaration)
        {
            if (this.variables == null)
            {
                this.variables = new VariablesCollection(this);
            }
            return this.variables.AddQueryVariable(varDeclaration);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="baseUri"></param>
        /// <param name="uri"></param>
        /// <param name="data"></param>
        /// <param name="numMatchedSegments"></param>
        /// <param name="relativePathSegments"></param>
        /// <param name="uriQuery"></param>
        /// <returns></returns>
        internal UriTemplateMatch CreateUriTemplateMatch(Uri baseUri, Uri uri, object data,
            int numMatchedSegments, Collection<string> relativePathSegments, NameValueCollection uriQuery)
        {
            UriTemplateMatch result = new UriTemplateMatch();
            result.RequestUri = uri;
            result.BaseUri = baseUri;
            if (uriQuery != null)
                result.SetQueryParameters(uriQuery);

            result.SetRelativePathSegments(relativePathSegments);
            result.Data = data;
            result.Template = this;
            for (int i = 0; i < numMatchedSegments; i++)
                this.segments[i].Lookup(result.RelativePathSegments[i], result.BoundVariables);

            if (this.wildcard != null)
                this.wildcard.Lookup(numMatchedSegments, result.RelativePathSegments,
                    result.BoundVariables);
            else if (numMatchedSegments < this.segments.Count)
                BindTerminalDefaults(numMatchedSegments, result.BoundVariables);

            if (this.queries.Count > 0)
            {
                foreach (KeyValuePair<string, UriTemplateQueryValue> kvp in this.queries)
                    kvp.Value.Lookup(result.QueryParameters[kvp.Key], result.BoundVariables);
            }

            if (this.additionalDefaults != null)
            {
                foreach (KeyValuePair<string, string> kvp in this.additionalDefaults)
                    result.BoundVariables.Add(kvp.Key, UnescapeDefaultValue(kvp.Value));
            }

            result.SetWildcardPathSegmentsStart(numMatchedSegments);
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <param name="segmentsCount"></param>
        /// <returns></returns>
        internal bool IsPathPartiallyEquivalentAt(UriTemplate other, int segmentsCount)
        {
            // Refer to the note on template equivalency at IsEquivalentTo
            // This method checks if any uri with given number of segments, which can be matched
            //  by this template, can be also matched by the other template.
            for (int i = 0; i < segmentsCount; ++i)
                if (!this.segments[i].IsEquivalentTo(other.segments[i], ((i == segmentsCount - 1) && (this.ignoreTrailingSlash || other.ignoreTrailingSlash))))
                    return false;

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        internal bool IsQueryEquivalent(UriTemplate other)
        {
            if (this.queries.Count != other.queries.Count)
                return false;

            foreach (string key in this.queries.Keys)
            {
                UriTemplateQueryValue utqv = this.queries[key];
                UriTemplateQueryValue otherUtqv;
                if (!other.queries.TryGetValue(key, out otherUtqv))
                    return false;

                if (!utqv.IsEquivalentTo(otherUtqv))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="host"></param>
        /// <returns></returns>
        internal static Uri RewriteUri(Uri uri, string host)
        {
            if (!string.IsNullOrEmpty(host))
            {
                string originalHostHeader = uri.Host + ((!uri.IsDefaultPort) ? ":" + uri.Port.ToString(CultureInfo.InvariantCulture) : string.Empty);
                if (!string.Equals(originalHostHeader, host, StringComparison.OrdinalIgnoreCase))
                {
                    Uri sourceUri = new Uri(string.Format(CultureInfo.InvariantCulture, "{0}://{1}", uri.Scheme, host));
                    return (new UriBuilder(uri) { Host = sourceUri.Host, Port = sourceUri.Port }).Uri;
                }
            }

            return uri;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="baseAddress"></param>
        /// <param name="bindInfo"></param>
        /// <param name="omitDefaults"></param>
        /// <returns></returns>
        private Uri Bind(Uri baseAddress, BindInformation bindInfo, bool omitDefaults)
        {
            UriBuilder result = new UriBuilder(baseAddress);
            int parameterIndex = 0;
            int lastPathParameter = ((this.variables == null) ? -1 : this.variables.PathSegmentVariableNames.Count - 1);
            int lastPathParameterToBind;
            if (lastPathParameter == -1)
                lastPathParameterToBind = -1;
            else if (omitDefaults)
                lastPathParameterToBind = bindInfo.LastNonDefaultPathParameter;
            else
                lastPathParameterToBind = bindInfo.LastNonNullablePathParameter;

            string[] parameters = bindInfo.NormalizedParameters;
            IDictionary<string, string> extraQueryParameters = bindInfo.AdditionalParameters;
            // Binding the path :
            StringBuilder pathString = new StringBuilder(result.Path);
            if (pathString[pathString.Length - 1] != '/')
                pathString.Append('/');

            if (lastPathParameterToBind < lastPathParameter)
            {
                // Binding all the parameters we need
                int segmentIndex = 0;
                while (parameterIndex <= lastPathParameterToBind)
                    this.segments[segmentIndex++].Bind(parameters, ref parameterIndex, pathString);

                // Maybe we have some literals yet to bind
                while (this.segments[segmentIndex].Nature == UriTemplatePartType.Literal)
                    this.segments[segmentIndex++].Bind(parameters, ref parameterIndex, pathString);

                // We're done; skip to the beggining of the query parameters
                parameterIndex = lastPathParameter + 1;
            }
            else if (this.segments.Count > 0 || this.wildcard != null)
            {
                for (int i = 0; i < this.segments.Count; i++)
                    this.segments[i].Bind(parameters, ref parameterIndex, pathString);

                if (this.wildcard != null)
                    this.wildcard.Bind(parameters, ref parameterIndex, pathString);
            }

            if (this.ignoreTrailingSlash && (pathString[pathString.Length - 1] == '/'))
                pathString.Remove(pathString.Length - 1, 1);

            result.Path = pathString.ToString();

            // Binding the query :
            if ((this.queries.Count != 0) || (extraQueryParameters != null))
            {
                StringBuilder query = new StringBuilder("");
                foreach (string key in this.queries.Keys)
                    this.queries[key].Bind(key, parameters, ref parameterIndex, query);

                if (extraQueryParameters != null)
                {
                    foreach (string key in extraQueryParameters.Keys)
                    {
                        if (this.queries.ContainsKey(key.ToUpperInvariant()))
                            throw new ArgumentException(string.Format("Both literal and NameValueCollection key; {0}", key), "parameters");

                        string value = extraQueryParameters[key];
                        string escapedValue = (string.IsNullOrEmpty(value) ? string.Empty : HttpUtility.UrlEncode(value, Encoding.UTF8));
                        query.AppendFormat("&{0}={1}", HttpUtility.UrlEncode(key, Encoding.UTF8), escapedValue);
                    }
                }

                if (query.Length != 0)
                    query.Remove(0, 1); // remove extra leading '&'

                result.Query = query.ToString();
            }

            // Adding the fragment (if needed)
            if (this.fragment != null)
                result.Fragment = this.fragment;

            return result.Uri;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="numMatchedSegments"></param>
        /// <param name="boundParameters"></param>
        private void BindTerminalDefaults(int numMatchedSegments, NameValueCollection boundParameters)
        {
            for (int i = numMatchedSegments; i < this.segments.Count; i++)
            {
                switch (this.segments[i].Nature)
                {
                    case UriTemplatePartType.Variable:
                        {
                            UriTemplateVariablePathSegment vps = this.segments[i] as UriTemplateVariablePathSegment;
                            this.variables.LookupDefault(vps.VarName, boundParameters);
                        }
                        break;

                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="numSegmentsInBaseAddress"></param>
        /// <param name="candidateSegments"></param>
        /// <param name="numMatchedSegments"></param>
        /// <param name="relativeSegments"></param>
        /// <returns></returns>
        private bool IsCandidatePathMatch(int numSegmentsInBaseAddress, string[] candidateSegments,
            out int numMatchedSegments, out Collection<string> relativeSegments)
        {
            int numRelativeSegments = candidateSegments.Length - numSegmentsInBaseAddress;
            relativeSegments = new Collection<string>();
            bool isStillMatch = true;
            int relativeSegmentsIndex = 0;
            while (isStillMatch && (relativeSegmentsIndex < numRelativeSegments))
            {
                string segment = candidateSegments[relativeSegmentsIndex + numSegmentsInBaseAddress];

                // Mathcing to next regular segment in the template (if there is one); building the wire segment representation
                if (relativeSegmentsIndex < this.segments.Count)
                {
                    bool ignoreSlash = (this.ignoreTrailingSlash && (relativeSegmentsIndex == numRelativeSegments - 1));
                    UriTemplateLiteralPathSegment lps = UriTemplateLiteralPathSegment.CreateFromWireData(segment);
                    if (!this.segments[relativeSegmentsIndex].IsMatch(lps, ignoreSlash))
                    {
                        isStillMatch = false;
                        break;
                    }

                    string relPathSeg = Uri.UnescapeDataString(segment);
                    if (lps.EndsWithSlash)
                        relPathSeg = relPathSeg.Substring(0, relPathSeg.Length - 1); // trim slash

                    relativeSegments.Add(relPathSeg);
                }
                // Checking if the template has a wild card ('*') or a final star var segment ("{*<var name>}"
                else if (this.HasWildcard)
                    break;
                else
                {
                    isStillMatch = false;
                    break;
                }
                relativeSegmentsIndex++;
            }

            if (isStillMatch)
            {
                numMatchedSegments = relativeSegmentsIndex;
                // building the wire representation to segments that were matched to a wild card
                if (relativeSegmentsIndex < numRelativeSegments)
                {
                    while (relativeSegmentsIndex < numRelativeSegments)
                    {
                        string relPathSeg = Uri.UnescapeDataString(candidateSegments[relativeSegmentsIndex + numSegmentsInBaseAddress]);
                        if (relPathSeg.EndsWith("/", StringComparison.Ordinal))
                            relPathSeg = relPathSeg.Substring(0, relPathSeg.Length - 1); // trim slash

                        relativeSegments.Add(relPathSeg);
                        relativeSegmentsIndex++;
                    }
                }
                // Checking if we matched all required segments already
                else if (numMatchedSegments < this.firstOptionalSegment)
                    isStillMatch = false;
            }
            else
                numMatchedSegments = 0;

            return isStillMatch;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        private bool IsPathFullyEquivalent(UriTemplate other)
        {
            // Refer to the note on template equivalency at IsEquivalentTo
            // This method checks if both templates has a fully equivalent path.
            if (this.HasWildcard != other.HasWildcard)
                return false;

            if (this.segments.Count != other.segments.Count)
                return false;

            for (int i = 0; i < this.segments.Count; ++i)
            {
                if (!this.segments[i].IsEquivalentTo(other.segments[i],
                    (i == this.segments.Count - 1) && !this.HasWildcard && (this.ignoreTrailingSlash || other.ignoreTrailingSlash)))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="omitDefaults"></param>
        /// <returns></returns>
        private BindInformation PrepareBindInformation(IDictionary<string, string> parameters, bool omitDefaults)
        {
            if (parameters == null)
                throw new ArgumentNullException("parameters");

            IDictionary<string, string> extraParameters = new Dictionary<string, string>(UriTemplateHelpers.GetQueryKeyComparer());
            foreach (KeyValuePair<string, string> kvp in parameters)
            {
                if (string.IsNullOrEmpty(kvp.Key))
                    throw new ArgumentException(string.Format("Bind by name called with empty key"), "parameters");

                extraParameters.Add(kvp);
            }

            BindInformation bindInfo;
            ProcessDefaultsAndCreateBindInfo(omitDefaults, extraParameters, out bindInfo);
            return bindInfo;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="omitDefaults"></param>
        /// <returns></returns>
        private BindInformation PrepareBindInformation(NameValueCollection parameters, bool omitDefaults)
        {
            if (parameters == null)
                throw new ArgumentNullException("parameters");

            IDictionary<string, string> extraParameters = new Dictionary<string, string>(UriTemplateHelpers.GetQueryKeyComparer());
            foreach (string key in parameters.AllKeys)
            {
                if (string.IsNullOrEmpty(key))
                    throw new ArgumentException(string.Format("Bind by name called with empty key"), "parameters");

                extraParameters.Add(key, parameters[key]);
            }

            BindInformation bindInfo;
            ProcessDefaultsAndCreateBindInfo(omitDefaults, extraParameters, out bindInfo);
            return bindInfo;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="omitDefaults"></param>
        /// <param name="extraParameters"></param>
        /// <param name="bindInfo"></param>
        private void ProcessDefaultsAndCreateBindInfo(bool omitDefaults, IDictionary<string, string> extraParameters,
            out BindInformation bindInfo)
        {
            if (this.additionalDefaults != null)
            {
                if (omitDefaults)
                {
                    foreach (KeyValuePair<string, string> kvp in this.additionalDefaults)
                    {
                        string extraParameter;
                        if (extraParameters.TryGetValue(kvp.Key, out extraParameter))
                            if (string.Compare(extraParameter, kvp.Value, StringComparison.Ordinal) == 0)
                                extraParameters.Remove(kvp.Key);
                    }
                }
                else
                {
                    foreach (KeyValuePair<string, string> kvp in this.additionalDefaults)
                    {
                        if (!extraParameters.ContainsKey(kvp.Key))
                            extraParameters.Add(kvp.Key, kvp.Value);
                    }
                }
            }

            if (extraParameters.Count == 0)
                extraParameters = null;

            bindInfo = new BindInformation(extraParameters);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="escapedValue"></param>
        /// <returns></returns>
        private string UnescapeDefaultValue(string escapedValue)
        {
            if (string.IsNullOrEmpty(escapedValue))
                return escapedValue;
            if (this.unescapedDefaults == null)
                this.unescapedDefaults = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);

            return this.unescapedDefaults.GetOrAdd(escapedValue, Uri.UnescapeDataString);
        }
    } // public class UriTemplate
} // namespace TridentFramework.RPC
