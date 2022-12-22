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
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

using TridentFramework.RPC.Http.HttpMessages;
using TridentFramework.RPC.Http.Transports;

using TridentFramework.RPC.Utility;

namespace TridentFramework.RPC.Http
{
    /// <summary>
    ///
    /// </summary>
    internal class SecureHttpContext : HttpContext
    {
        private readonly X509Certificate _certificate;

        /*
        ** Properties
        */

        /// <summary>
        /// Gets or sets client certificate.
        /// </summary>
        public ClientCertificate ClientCertificate { get; private set; }

        /// <inheritdoc />
        public override bool IsSecure
        {
            get { return true; }
        }

        /// <summary>
        /// Gets used protocol.
        /// </summary>
        protected SslProtocols Protocol { get; private set; }

        /// <summary>
        /// Gets or sets if client certificate should be used instead of server certificate.
        /// </summary>
        public bool UseClientCertificate { get; set; }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="SecureHttpContext"/> class.
        /// </summary>
        /// <param name="protocols">SSL protocol to use.</param>
        /// <param name="socket">The socket.</param>
        /// <param name="context">The context.</param>
        /// <param name="certificate">Server certificate to use.</param>
        public SecureHttpContext(X509Certificate certificate, SslProtocols protocols, Socket socket,
                                 MessageFactoryContext context) : base(socket, context)
        {
            _certificate = certificate;
            Protocol = protocols;
        }

        /// <inheritdoc />
        protected override Stream CreateStream(Socket socket)
        {
            Stream stream = base.CreateStream(socket);

            var sslStream = new SslStream(stream, false, OnValidation);
            try
            {
                sslStream.AuthenticateAsServer(_certificate, UseClientCertificate, Protocol, false);
            }
            catch (IOException err)
            {
                RPCLogger.Trace(err.Message);
                throw new InvalidOperationException("Failed to authenticate", err);
            }
            catch (ObjectDisposedException err)
            {
                RPCLogger.Trace(err.Message);
                throw new InvalidOperationException("Failed to create stream.", err);
            }
            catch (AuthenticationException err)
            {
                RPCLogger.Trace((err.InnerException != null) ? err.InnerException.Message : err.Message);
                throw new InvalidOperationException("Failed to authenticate.", err);
            }

            return sslStream;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="receivedCertificate"></param>
        /// <param name="chain"></param>
        /// <param name="sslPolicyErrors"></param>
        /// <returns></returns>
        private bool OnValidation(object sender, X509Certificate receivedCertificate, X509Chain chain,
                                  SslPolicyErrors sslPolicyErrors)
        {
            ClientCertificate = new ClientCertificate(receivedCertificate, chain, sslPolicyErrors);
            return !(UseClientCertificate && receivedCertificate == null);
        }
    } // internal class SecureHttpContext : HttpContext
} // namespace TridentFramework.RPC.Http
