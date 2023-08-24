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

namespace TridentFramework.RPC.Http.HttpMessages
{
    /// <summary>
    /// Create a HTTP response object.
    /// </summary>
    public class Response : IResponse
    {
        private readonly MemoryStream body = new MemoryStream();
        private readonly IHttpContext context;
        private readonly ResponseCookieCollection cookies = new ResponseCookieCollection();
        private readonly HeaderCollection headers;
        private ConnectionHeader connection;
        private NumericHeader contentLength = new NumericHeader("Content-Length", 0);

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
        /// Gets connection type.
        /// </summary>
        public ConnectionHeader Connection
        {
            get
            {
                return connection ??
                       (HttpVersion == "HTTP/1.0" ? ConnectionHeader.Default10 : ConnectionHeader.Default11);
            }
            set { connection = value; }
        }

        /// <summary>
        /// Status code that is sent to the client.
        /// </summary>
        /// <remarks>Default is <see cref="HttpStatusCode.OK"/></remarks>
        public HttpStatusCode Status { get; set; }

        /// <summary>
        /// Gets HTTP version.
        /// </summary>
        /// <remarks>
        /// Default is HTTP/1.1
        /// </remarks>
        public string HttpVersion { get; private set; }

        /// <summary>
        /// Information about why a specific status code was used.
        /// </summary>
        public string Reason { get; set; }

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
            set { contentLength = value; }
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
            get; set;
        }

        /// <summary>
        /// Gets cookies.
        /// </summary>
        public ResponseCookieCollection Cookies
        {
            get { return cookies; }
        }

        /// <summary>
        /// Gets body stream.
        /// </summary>
        public Stream Body
        {
            get { return body; }
        }

        /// <summary>
        /// Gets headers.
        /// </summary>
        public IHeaderCollection Headers
        {
            get { return headers; }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="Response"/> class.
        /// </summary>
        /// <param name="version">HTTP Version.</param>
        /// <param name="code">HTTP status code.</param>
        /// <param name="reason">Why the status code was selected.</param>
        /// <exception cref="FormatException">Version must start with 'HTTP/'</exception>
        public Response(string version, HttpStatusCode code, string reason)
        {
            if (!version.StartsWith("HTTP/"))
                throw new FormatException("Version must start with 'HTTP/'");

            Status = code;
            Reason = reason;
            HttpVersion = version;
            ContentType = new ContentTypeHeader("text/html");
            Encoding = Encoding.UTF8;
            headers = CreateHeaderCollection();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Response"/> class.
        /// </summary>
        /// <param name="context">Context that the response will be sent through.</param>
        /// <param name="request">Request that the response is for.</param>
        /// <exception cref="FormatException">Version must start with 'HTTP/'</exception>
        public Response(IHttpContext context, IRequest request)
        {
            this.context = context;
            HttpVersion = request.HttpVersion;
            Reason = string.Empty;
            Status = HttpStatusCode.OK;
            ContentType = new ContentTypeHeader("text/html");
            Encoding = request.Encoding;
            headers = CreateHeaderCollection();
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        private HeaderCollection CreateHeaderCollection()
        {
            HeaderFactory headerFactory = HttpFactory.Current == null ? new HeaderFactory() : HttpFactory.Current.Get<HeaderFactory>();
            return new HeaderCollection(headerFactory);
        }

        /// <summary>
        /// Redirect user.
        /// </summary>
        /// <param name="uri">Where to redirect to.</param>
        /// <remarks>
        /// Any modifications after a redirect will be ignored.
        /// </remarks>
        public void Redirect(string uri)
        {
            Status = HttpStatusCode.Redirect;
            headers["Location"] = new StringHeader("Location", uri);
        }

        /// <summary>
        /// Add a new header.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Add(string name, IHeader value)
        {
            string lowerName = name.ToLower();
            if (lowerName == "Content-Length")
                ContentLength = (NumericHeader)value;
            if (lowerName == "Content-Type")
                ContentType = (ContentTypeHeader)value;
            if (lowerName == "Connection")
                Connection = (ConnectionHeader)value;

            headers.Add(name, value);
        }

        /// <summary>
        /// Add a new header.
        /// </summary>
        /// <param name="header">Header to add.</param>
        public void Add(IHeader header)
        {
            headers.Add(header.Name, header);
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
    } // internal class Response : IResponse
} // namespace TridentFramework.RPC.Http.HttpMessages
