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

using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace TridentFramework.RPC.Http.Transports
{
    /// <summary>
    /// Client X.509 certificate, X.509 chain, and any SSL policy errors encountered
    /// during the SSL stream creation
    /// </summary>
    public class ClientCertificate
    {
        /*
        ** Properties
        */

        /// <summary>
        /// Client security certificate
        /// </summary>
        public X509Certificate Certificate { get; private set; }

        /// <summary>
        /// Client security certificate chain
        /// </summary>
        public X509Chain Chain { get; private set; }

        /// <summary>
        /// Any SSL policy errors encountered during the SSL stream creation
        /// </summary>
        public SslPolicyErrors SslPolicyErrors { get; private set; }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientCertificate"/> class.
        /// </summary>
        /// <param name="certificate">The certificate.</param>
        /// <param name="chain">Client security certificate chain.</param>
        /// <param name="sslPolicyErrors">Any SSL policy errors encountered during the SSL stream creation.</param>
        public ClientCertificate(X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            Certificate = certificate;
            Chain = chain;
            SslPolicyErrors = sslPolicyErrors;
        }
    } // public class ClientCertificate
} // namespace TridentFramework.RPC.Http.Transports
