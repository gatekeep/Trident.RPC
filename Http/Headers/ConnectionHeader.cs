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
//
// Based on code from the C# WebServer project. (http://webserver.codeplex.com/)
// Copyright (c) 2012 Jonas Gauffin
// Licensed under the Apache 2.0 License (http://opensource.org/licenses/Apache-2.0)
//

using System;

namespace TridentFramework.RPC.Http.Headers
{
    /// <summary>
    /// Type of HTTP connection
    /// </summary>
    public enum ConnectionType
    {
        /// <summary>
        /// Connection is closed after each request-response
        /// </summary>
        Close,

        /// <summary>
        /// Connection is kept alive for X seconds (unless another request have been made)
        /// </summary>
        KeepAlive
    } // public enum ConnectionType

    /// <summary>
    /// The Connection general-header field allows the sender to specify options
    /// that are desired for that particular connection and MUST NOT be
    /// communicated by proxies over further connections.
    /// </summary>
    /// <remarks>
    /// <para>
    ///   HTTP/1.1 proxies MUST parse the Connection header field before a
    ///   message is forwarded and, for each connection-token in this field,
    ///   remove any header field(s) from the message with the same name as the
    ///   connection-token. Connection options are signaled by the presence of
    ///   a connection-token in the Connection header field, not by any
    ///   corresponding additional header field(s), since the additional header
    ///   field may not be sent if there are no parameters associated with that
    ///   connection option.
    ///</para><para>
    ///   Message headers listed in the Connection header MUST NOT include
    ///   end-to-end headers, such as Cache-Control.
    ///</para><para>
    ///   HTTP/1.1 defines the "close" connection option for the sender to
    ///   signal that the connection will be closed after completion of the
    ///   response. For example,
    ///<example>
    ///       Connection: close
    ///</example>
    ///   in either the request or the response header fields indicates that
    ///   the connection SHOULD NOT be considered `persistent' (section 8.1)
    ///   after the current request/response is complete.
    ///</para><para>
    ///   HTTP/1.1 applications that do not support persistent connections MUST
    ///   include the "close" connection option in every message.
    ///</para><para>
    ///   A system receiving an HTTP/1.0 (or lower-version) message that
    ///   includes a Connection header MUST, for each connection-token in this
    ///   field, remove and ignore any header field(s) from the message with
    ///   the same name as the connection-token. This protects against mistaken
    ///   forwarding of such header fields by pre-HTTP/1.1 proxies. See section
    ///   19.6.2 in RFC2616.
    /// </para>
    /// </remarks>
    public class ConnectionHeader : IHeader
    {
        /// <summary>
        /// Header name
        /// </summary>
        public const string NAME = "Connection";

        /// <summary>
        /// Default connection header for HTTP/1.0
        /// </summary>
        public static readonly ConnectionHeader Default10 = new ConnectionHeader(ConnectionType.Close);

        /// <summary>
        /// Default connection header for HTTP/1.1
        /// </summary>
        public static readonly ConnectionHeader Default11 = new ConnectionHeader(ConnectionType.KeepAlive);

        /*
        ** Properties
        */

        /// <summary>
        /// Gets connection parameters.
        /// </summary>
        public HeaderParameterCollection Parameters { get; private set; }

        /// <summary>
        /// Gets or sets connection type
        /// </summary>
        public ConnectionType Type { get; set; }

        /// <summary>
        /// Gets header name
        /// </summary>
        public string Name
        {
            get { return NAME; }
        }

        /// <summary>
        /// Gets Connection header as a string
        /// </summary>
        public string HeaderValue
        {
            get { return Type == ConnectionType.KeepAlive ? "Keep-Alive" : Type.ToString(); }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionHeader"/> class.
        /// </summary>
        /// <param name="type">Connection type.</param>
        /// <param name="parameters">The parameters.</param>
        public ConnectionHeader(ConnectionType type, HeaderParameterCollection parameters)
        {
            Parameters = parameters;
            Type = type;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionHeader"/> class.
        /// </summary>
        /// <param name="type">The type.</param>
        public ConnectionHeader(ConnectionType type)
        {
            Type = type;
            Parameters = new HeaderParameterCollection();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return HeaderValue;
        }
    } // public class ConnectionHeader : IHeader
} // namespace TridentFramework.RPC.Http.Headers
