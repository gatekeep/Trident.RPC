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

using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace TridentFramework.RPC.Http
{
    /// <summary>
    /// Secure version of the HTTP listener.
    /// </summary>
    public class SecureHttpListener : HttpListener
    {
        private readonly X509Certificate certificate;

        /*
        ** Properties
        */

        /// <inheritdoc />
        public override bool IsSecure
        {
            get { return true; }
        }

        /// <summary>
        /// Gets or sets SSL protocol.
        /// </summary>
        public SslProtocols Protocol { get; set; }

        /// <summary>
        /// Gets or sets if client certificate should be used.
        /// </summary>
        public bool UseClientCertificate { get; set; }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="SecureHttpListener"/> class.
        /// </summary>
        /// <param name="address">Address to accept new connections on.</param>
        /// <param name="port">Port to accept connections on.</param>
        /// <param name="certificate">Certificate securing the connection.</param>
        public SecureHttpListener(IPAddress address, int port, X509Certificate certificate) : base(address, port)
        {
            Protocol = SslProtocols.Tls12;
            this.certificate = certificate;
        }

        /// <inheritdoc />
        protected override HttpContext CreateContext(Socket socket)
        {
            return Factory.Get<SecureHttpContext>(certificate, Protocol, socket);
        }
    } // public class SecureHttpListener : HttpListener
} // namespace TridentFramework.RPC.Http
