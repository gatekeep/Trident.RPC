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
using System.Net;

namespace TridentFramework.RPC.Http
{
    /// <summary>
    /// Result of processing.
    /// </summary>
    public enum ProcessingResult
    {
        /// <summary>
        /// Continue with the next handler
        /// </summary>
        Continue,

        /// <summary>
        /// No more handlers can process the request.
        /// </summary>
        /// <remarks>
        /// The server will process the response object and
        /// generate a HTTP response from it.
        /// </remarks>
        SendResponse,

        /// <summary>
        /// Response have been sent back by the handler.
        /// </summary>
        /// <remarks>
        /// This option should only be used if you are streaming
        /// something or sending back a custom result. The server will
        /// not process the response object or send anything back
        /// to the client.
        /// </remarks>
        Abort
    } // public enum ProcessingResult

    /// <summary>
    /// Http listener
    /// </summary>
    public interface IHttpListener
    {
        /*
        ** Properties
        */

        /// <summary>
        /// Gets listener address.
        /// </summary>
        IPAddress Address { get; }

        /// <summary>
        /// Gets if listener is secure.
        /// </summary>
        bool IsSecure { get; }

        /// <summary>
        /// Gets if listener have been started.
        /// </summary>
        bool IsStarted { get; }

        /// <summary>
        /// Gets listening port.
        /// </summary>
        int Port { get; }

        /// <summary>
        /// Gets the maximum content size.
        /// </summary>
        /// <value>The content length limit.</value>
        /// <remarks>
        /// Used when responding to 100-continue.
        /// </remarks>
        int ContentLengthLimit { get; set; }

        /*
        ** Events
        */

        /// <summary>
        /// A new request have been received.
        /// </summary>
        event EventHandler<RequestEventArgs> RequestReceived;

        /// <summary>
        /// Can be used to reject certain clients.
        /// </summary>
        event EventHandler<SocketFilterEventArgs> SocketAccepted;

        /// <summary>
        /// A HTTP exception have been thrown.
        /// </summary>
        /// <remarks>
        /// Fill the body with a user friendly error page, or redirect to somewhere else.
        /// </remarks>
        event EventHandler<ErrorPageEventArgs> ErrorPageRequested;

        /*
        ** Methods
        */

        /// <summary>
        /// Start listener.
        /// </summary>
        /// <param name="backLog">Number of pending accepts.</param>
        /// <remarks>
        /// Make sure that you are subscribing on <see cref="RequestReceived"/> first.
        /// </remarks>
        /// <exception cref="InvalidOperationException">Listener have already been started.</exception>
        /// <exception cref="SocketException">Failed to start socket.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Invalid port number.</exception>
        void Start(int backLog);

        /// <summary>
        /// Stop listener.
        /// </summary>
        void Stop();
    } // public interface IHttpListener
} // namespace TridentFramework.RPC.Http
