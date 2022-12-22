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
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace TridentFramework.RPC
{
    /// <summary>
    /// A class that represents the results of a match operation on a <see cref="UriTemplate"/> instance.
    /// </summary>
    public class UriTemplateMatch
    {
        private Uri baseUri;
        private NameValueCollection boundVariables;
        private object data;
        private NameValueCollection queryParameters;
        private Collection<string> relativePathSegments;
        private Uri requestUri;
        private UriTemplate template;
        private Collection<string> wildcardPathSegments;
        private int wildcardSegmentsStartOffset = -1;

        /*
        ** Properties
        */

        /// <summary>
        /// Gets or sets the base URI for the template match.
        /// </summary>
        public Uri BaseUri   // the base address, untouched
        {
            get => this.baseUri;
            set => this.baseUri = value;
        }

        /// <summary>
        /// Gets the BoundVariables collection for the template match.
        /// </summary>
        public NameValueCollection BoundVariables // result of TryLookup, values are decoded
        {
            get
            {
                if (this.boundVariables == null)
                    this.boundVariables = new NameValueCollection();
                return this.boundVariables;
            }
        }

        /// <summary>
        /// Gets or sets the object associated with the <see cref="UriTemplateMatch"/> instance.
        /// </summary>
        public object Data
        {
            get => this.data;
            set => this.data = value;
        }

        /// <summary>
        /// Gets a collection of query string parameters and their values.
        /// </summary>
        public NameValueCollection QueryParameters  // the result of UrlUtility.ParseQueryString (keys and values are decoded)
        {
            get
            {
                if (this.queryParameters == null)
                    PopulateQueryParameters();
                return this.queryParameters;
            }
        }

        /// <summary>
        /// Gets a collection of relative path segments.
        /// </summary>
        public Collection<string> RelativePathSegments  // entire Path (after the base address), decoded
        {
            get
            {
                if (this.relativePathSegments == null)
                    this.relativePathSegments = new Collection<string>();
                return this.relativePathSegments;
            }
        }

        /// <summary>
        /// Gets or sets the matched URI.
        /// </summary>
        public Uri RequestUri  // uri on the wire, untouched
        {
            get => this.requestUri;
            set => this.requestUri = value;
        }

        /// <summary>
        /// Gets or sets the <see cref="UriTemplate"/> instance associated with this UriTemplateMatch instance.
        /// </summary>
        public UriTemplate Template // which one got matched
        {
            get => this.template;
            set => this.template = value;
        }

        /// <summary>
        /// Gets a collection of path segments that are matched by a wildcard in the URI template.
        /// </summary>
        public Collection<string> WildcardPathSegments  // just the Path part matched by "*", decoded
        {
            get
            {
                if (this.wildcardPathSegments == null)
                    PopulateWildcardSegments();
                return this.wildcardPathSegments;
            }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="UriTemplateMatch"/> class.
        /// </summary>
        public UriTemplateMatch()
        {
            /* stub */
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="queryParameters"></param>
        internal void SetQueryParameters(NameValueCollection queryParameters)
        {
            this.queryParameters = new NameValueCollection(queryParameters);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="segments"></param>
        internal void SetRelativePathSegments(Collection<string> segments)
        {
            this.relativePathSegments = segments;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="startOffset"></param>
        internal void SetWildcardPathSegmentsStart(int startOffset)
        {
            this.wildcardSegmentsStartOffset = startOffset;
        }

        /// <summary>
        /// 
        /// </summary>
        private void PopulateQueryParameters()
        {
            if (this.requestUri != null)
                this.queryParameters = UriTemplateHelpers.ParseQueryString(this.requestUri.Query);
            else
                this.queryParameters = new NameValueCollection();
        }
        
        /// <summary>
        /// 
        /// </summary>
        private void PopulateWildcardSegments()
        {
            if (wildcardSegmentsStartOffset != -1)
            {
                this.wildcardPathSegments = new Collection<string>();
                for (int i = this.wildcardSegmentsStartOffset; i < this.RelativePathSegments.Count; ++i)
                    this.wildcardPathSegments.Add(this.RelativePathSegments[i]);
            }
            else
                this.wildcardPathSegments = new Collection<string>();
        }
    } // public class UriTemplateMatch
} // namespace TridentFramework.RPC
