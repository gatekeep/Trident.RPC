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
//
// Based on code from the C# WebServer project. (http://webserver.codeplex.com/)
// Copyright (c) 2012 Jonas Gauffin
// Licensed under the Apache 2.0 License (http://opensource.org/licenses/Apache-2.0)
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

using TridentFramework.RPC.Http.Headers;
using TridentFramework.RPC.Http.Tools;

namespace TridentFramework.RPC.Http.HttpMessages
{
    /// <summary>
    /// Request implementation.
    /// </summary>
    internal class Request : IRequest, IDisposable
    {
        private const int MEMORY_MAX_SIZE = 67108864;
        private readonly HeaderCollection headers;
        private NumericHeader contentLength = new NumericHeader("Content-Length", 0);
        private RequestCookieCollection cookies;
        private IParameterCollection form;
        private string bodyFileName;

        /*
        ** Properties
        */

        /// <summary>
        /// Gets a header.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public IHeader this[string name]
        {
            get { return headers[name]; }
        }

        /// <summary>
        /// Gets request URI.
        /// </summary>
        public Uri Uri { get; private set; }

        /// <summary>
        /// Gets cookies.
        /// </summary>
        public RequestCookieCollection Cookies
        {
            get { return cookies ?? (cookies = new RequestCookieCollection()); }

            private set { cookies = value; }
        }

        /// <summary>
        /// Gets all uploaded files.
        /// </summary>
        public HttpFileCollection Files { get; internal set; }

        /// <summary>
        /// Gets query string and form parameters
        /// </summary>
        public IParameterCollection Parameters { get; internal set; }

        /// <summary>
        /// Gets form parameters.
        /// </summary>
        public IParameterCollection Form
        {
            get { return form; }
            internal set
            {
                form = value;
                Parameters = new ParameterCollection(QueryString, form);
            }
        }

        /// <summary>
        /// Gets query string.
        /// </summary>
        public IParameterCollection QueryString { get; internal set; }

        /// <summary>
        /// Gets if request is an Ajax request.
        /// </summary>
        public bool IsAjax
        {
            get
            {
                var header = headers["X-Requested-With"] as StringHeader;
                return header != null && header.Value == "XMLHttpRequest";
            }
        }

        /// <summary>
        /// Gets or sets connection header.
        /// </summary>
        public ConnectionHeader Connection
        {
            get { return (ConnectionHeader)Headers[ConnectionHeader.NAME]; }
            set { headers[ConnectionHeader.NAME] = value; }
        }

        /// <summary>
        /// Gets or sets HTTP version.
        /// </summary>
        public string HttpVersion { get; set; }

        /// <summary>
        /// Gets or sets HTTP method.
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// Gets requested URI.
        /// </summary>
        Uri IRequest.Uri
        {
            get { return Uri; }
            set { Uri = value; }
        }

        /// <summary>
        /// Kind of content in the body
        /// </summary>
        /// <remarks>Default is <c>text/html</c></remarks>
        public ContentTypeHeader ContentType { get; set; }

        /// <summary>
        /// Gets or sets encoding
        /// </summary>
        public Encoding Encoding
        {
            get;
            set;
        }

        /// <summary>
        /// Gets body stream.
        /// </summary>
        public Stream Body { get; private set; }

        /// <summary>
        /// Size of the body. MUST be specified before sending the header,
        /// unless property Chunked is set to <c>true</c>.
        /// </summary>
        /// <value>
        /// Any specifically assigned value or Body stream length.
        /// </value>
        public NumericHeader ContentLength
        {
            get
            {
                if (Body.Length > 0)
                    contentLength.Value = Body.Length;
                return contentLength;
            }
            set
            {
                contentLength = value;

                if (contentLength.Value <= MEMORY_MAX_SIZE)
                    return;

                if (bodyFileName == null)
                    bodyFileName = Path.GetTempFileName();
                Body = new FileStream(bodyFileName, FileMode.Create);
            }
        }

        /// <summary>
        /// Gets the User-Agent header.
        /// </summary>
        public string UserAgent
        {
            get
            {
                var header = headers["User-Agent"] as StringHeader;
                if (header != null)
                    return header.Value;
                else
                    return "Unknown/1.0";
            }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="Request"/> class.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <param name="path">The path.</param>
        /// <param name="version">The version.</param>
        public Request(string method, string path, string version)
        {
            Body = new MemoryStream();
            Method = method;
            HttpVersion = version;
            Encoding = Encoding.UTF8;

            // HttpFactory is not set during tests.
            HeaderFactory headerFactory = HttpFactory.Current == null
                                              ? new HeaderFactory()
                                              : HttpFactory.Current.Get<HeaderFactory>();

            headers = new HeaderCollection(headerFactory);

            // Parse query string.
            int pos = path.IndexOf("?");
            QueryString = pos != -1 ? UrlParser.Parse(path.Substring(pos + 1)) : new ParameterCollection();

            Parameters = QueryString;
            Uri = new Uri("http://not.specified.yet" + path);
        }

        /// <summary>
        /// Get a header
        /// </summary>
        /// <typeparam name="T">Type that it should be cast to</typeparam>
        /// <param name="headerName">Name of header</param>
        /// <returns>Header if found and casted properly; otherwise <c>null</c>.</returns>
        public T Get<T>(string headerName) where T : class, IHeader
        {
            return headers.Get<T>(headerName);
        }

        /// <summary>
        /// Add a new header.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Add(string name, IHeader value)
        {
            string lowerName = name.ToLower();
            if (lowerName == "host")
            {
                var header = (StringHeader)value;
                string method = HttpContext.Current.IsSecure ? "https://" : "http://";
                Uri = new Uri(method + header.Value + Uri.PathAndQuery);
                return;
            }
            if (lowerName == "content-length")
                ContentLength = (NumericHeader)value;
            if (lowerName == "content-type")
            {
                ContentType = (ContentTypeHeader)value;
                string charset = ContentType.Parameters["charset"];
                if (!string.IsNullOrEmpty(charset))
                    Encoding = Encoding.GetEncoding(charset);
            }
            if (lowerName == "cookie")
                Cookies = ((CookieHeader)value).Cookies;

            headers.Add(name, value);
        }

        /// <summary>
        /// Add a new header.
        /// </summary>
        /// <param name="header">Header to add.</param>
        public void Add(IHeader header)
        {
            Add(header.Name, header);
        }

        /// <summary>
        /// Gets headers.
        /// </summary>
        public IHeaderCollection Headers
        {
            get { return headers; }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<IHeader> GetEnumerator()
        {
            return headers.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        ///
        /// </summary>
        public void Dispose()
        {
            if (!string.IsNullOrEmpty(bodyFileName))
            {
                File.Delete(bodyFileName);
                bodyFileName = null;
            }
        }
    } // internal class Request : IRequest, IDisposable
} // namespace TridentFramework.RPC.Http.HttpMessages
