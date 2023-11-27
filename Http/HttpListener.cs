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
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

using TridentFramework.RPC.Http.Headers;
using TridentFramework.RPC.Http.HttpMessages;

using TridentFramework.RPC.Utility;

namespace TridentFramework.RPC.Http
{
    /// <summary>
    /// Http listener.
    /// </summary>
    public class HttpListener : IHttpListener
    {
        private readonly HttpFactory factory;
        private readonly ManualResetEvent shutdownEvent = new ManualResetEvent(false);
        private TcpListener listener;
        private int pendingAccepts;
        private bool shuttingDown;

        /*
        ** Properties
        */

        /// <summary>
        /// Gets HTTP factory used to create types used by this HTTP library.
        /// </summary>
        protected IHttpFactory Factory
        {
            get { return factory; }
        }

        /// <summary>
        /// Gets or sets the maximum number of bytes that the request body can contain.
        /// </summary>
        /// <value>The content length limit.</value>
        /// <remarks>
        /// <para>
        /// Used when responding to 100-continue.
        /// </para>
        /// <para>
        /// 0 = turned off.
        /// </para>
        /// </remarks>
        public int ContentLengthLimit { get; set; }

        /// <summary>
        /// Gets listener address.
        /// </summary>
        public IPAddress Address { get; private set; }

        /// <summary>
        /// Gets if listener is secure.
        /// </summary>
        public virtual bool IsSecure
        {
            get { return true; }
        }

        /// <summary>
        /// Gets if listener have been started.
        /// </summary>
        public bool IsStarted { get; private set; }

        /// <summary>
        /// Gets listening port.
        /// </summary>
        public int Port { get; private set; }

        /*
        ** Events
        */

        /// <summary>
        /// A new request have been received.
        /// </summary>
        public event EventHandler<RequestEventArgs> RequestReceived = delegate { };

        /// <summary>
        /// Can be used to reject certain clients.
        /// </summary>
        public event EventHandler<SocketFilterEventArgs> SocketAccepted = delegate { };

        /// <summary>
        /// A HTTP exception have been thrown.
        /// </summary>
        /// <remarks>
        /// Fill the body with a user friendly error page, or redirect to somewhere else.
        /// </remarks>
        public event EventHandler<ErrorPageEventArgs> ErrorPageRequested = delegate { };

        /// <summary>
        /// Client asks if he may continue.
        /// </summary>
        /// <remarks>
        /// If the body is too large or anything like that you should respond <see cref="HttpStatusCode.ExpectationFailed"/>.
        /// </remarks>
        public event EventHandler<RequestEventArgs> ContinueResponseRequested = delegate { };

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpListener"/> class.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <param name="port">The port.</param>
        protected HttpListener(IPAddress address, int port)
        {
            Address = address;
            Port = port;
            factory = new HttpFactory();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpListener"/> class.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <param name="port">The port.</param>
        /// <param name="httpFactory">The HTTP factory.</param>
        protected HttpListener(IPAddress address, int port, HttpFactory httpFactory)
        {
            Address = address;
            Port = port;
            factory = httpFactory;
        }

        /// <summary>
        ///
        /// </summary>
        private void BeginAccept()
        {
            if (shuttingDown)
                return;

            Interlocked.Increment(ref pendingAccepts);
            try
            {
                listener.BeginAcceptSocket(OnSocketAccepted, null);
            }
            catch (Exception e)
            {
                RPCLogger.StackTrace("Unhandled exception in BeginAccept.", e);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="socket"></param>
        /// <returns></returns>
        private bool CanAcceptSocket(Socket socket)
        {
            try
            {
                SocketFilterEventArgs args = new SocketFilterEventArgs(socket);
                SocketAccepted(this, args);
                return args.IsSocketOk;
            }
            catch (Exception e)
            {
                RPCLogger.StackTrace("SocketAccepted trigger exception", e);
                return true;
            }
        }

        /// <summary>
        /// Creates a new <see cref="HttpListener"/> instance with default factories.
        /// </summary>
        /// <param name="address">Address that the listener should accept connections on.</param>
        /// <param name="port">Port that listener should accept connections on.</param>
        /// <returns>Created HTTP listener.</returns>
        public static HttpListener Create(IPAddress address, int port)
        {
            return new HttpListener(address, port);
        }

        /// <summary>
        /// Creates a new <see cref="HttpListener"/> instance with default factories.
        /// </summary>
        /// <param name="address">Address that the listener should accept connections on.</param>
        /// <param name="port">Port that listener should accept connections on.</param>
        /// <param name="factory">Factory used to create different types in the framework.</param>
        /// <returns>Created HTTP listener.</returns>
        public static HttpListener Create(IPAddress address, int port, HttpFactory factory)
        {
            return new HttpListener(address, port);
        }

        /// <summary>
        /// Creates a new <see cref="HttpListener"/> instance with default factories.
        /// </summary>
        /// <param name="address">Address that the listener should accept connections on.</param>
        /// <param name="port">Port that listener should accept connections on.</param>
        /// <param name="certificate">Certificate to use</param>
        /// <returns>Created HTTP listener.</returns>
        public static HttpListener Create(IPAddress address, int port, X509Certificate certificate)
        {
            //RequestParserFactory requestFactory = new RequestParserFactory();
            //HttpContextFactory factory = new HttpContextFactory(NullLogWriter.Instance, 16384, requestFactory);
            return new SecureHttpListener(address, port, certificate);
        }

        /// <summary>
        /// Create a new context
        /// </summary>
        /// <param name="socket">Accepted socket</param>
        /// <returns>A new context.</returns>
        protected virtual HttpContext CreateContext(Socket socket)
        {
            return Factory.Get<HttpContext>(socket);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="exception"></param>
        private void SendErrorPage(Exception exception)
        {
            HttpException httpException = exception as HttpException;
            IResponse response = HttpContext.Current.Response;
            response.Status = httpException != null ? httpException.Code : HttpStatusCode.InternalServerError;
            response.Reason = exception.Message;

            if (response.Body.CanWrite)
                response.Body.SetLength(0);

            ErrorPageEventArgs args = new ErrorPageEventArgs(HttpContext.Current) { Exception = exception };
            ErrorPageRequested(this, args);

            try
            {
                ResponseWriter generator = new ResponseWriter();
                if (args.IsHandled)
                    generator.Send(HttpContext.Current, response);
                else
                    generator.SendErrorPage(HttpContext.Current, response, exception);
            }
            catch (Exception e)
            {
                RPCLogger.StackTrace("Failed to display error page", e);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void On100Continue(object sender, ContinueEventArgs e)
        {
            Response response = new Response(e.Request.HttpVersion, HttpStatusCode.Continue, "Please continue mate.");
            if (ContentLengthLimit != 0 && e.Request.ContentLength.Value > ContentLengthLimit)
            {
                RPCLogger.WriteWarning("Requested to send " + e.Request.ContentLength.Value + " bytes, but we only allow " + ContentLengthLimit);
                response.Status = HttpStatusCode.ExpectationFailed;
                response.Reason = "Too large content length";
            }

            string responseString = string.Format("{0} {1} {2}\r\n\r\n", e.Request.HttpVersion, (int)response.Status, response.Reason);
            byte[] buffer = e.Request.Encoding.GetBytes(responseString);
            HttpContext.Current.Stream.Write(buffer, 0, buffer.Length);
            HttpContext.Current.Stream.Flush();
            Console.WriteLine(responseString);
            RPCLogger.Trace(responseString);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDisconnect(object sender, EventArgs e)
        {
            HttpFactory.Current = Factory;
            HttpContext context = (HttpContext)sender;
            context.Disconnected -= OnDisconnect;
            context.RequestReceived -= OnRequest;
            context.ContinueResponseRequested -= On100Continue;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="Exception">Throwing exception if in debug mode and not exception handler have been specified.</exception>
        private void OnRequest(object sender, RequestEventArgs e)
        {
            HttpContext context = (HttpContext)sender;
            HttpFactory.Current = Factory;
            HttpContext.Current = context;

            try
            {
                var args = new RequestEventArgs(context, e.Request, e.Response);
                RequestReceived(this, args);
                if (!args.IsHandled)
                {
                    // need to respond to the context.
                    var generator = new ResponseWriter();
                    generator.Send(context, args.Response);
                }

                // Disconnect when done.
                if (e.Response.HttpVersion == "HTTP/1.0" || e.Response.Connection.Type == ConnectionType.Close)
                    context.Disconnect();
            }
            catch (Exception err)
            {
                if (err is HttpException)
                {
                    HttpException exception = (HttpException)err;
                    SendErrorPage(exception);
                }
                else
                {
                    RPCLogger.StackTrace("Request failed.", err, false);
                    SendErrorPage(err);
                }
                e.IsHandled = true;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="ar"></param>
        private void OnSocketAccepted(IAsyncResult ar)
        {
            HttpFactory.Current = Factory;
            Socket socket = null;
            try
            {
                socket = listener.EndAcceptSocket(ar);
                Interlocked.Decrement(ref pendingAccepts);
                if (shuttingDown && pendingAccepts == 0)
                    shutdownEvent.Set();

                if (!CanAcceptSocket(socket))
                {
                    RPCLogger.Trace("Socket was rejected: " + socket.RemoteEndPoint);
                    socket.Disconnect(true);
                    BeginAccept();
                    return;
                }
            }
            catch (Exception err)
            {
                RPCLogger.WriteWarning("Failed to end accept: " + err.Message);
                BeginAccept();
                if (socket != null)
                    socket.Disconnect(true);
                return;
            }

            if (!shuttingDown)
                BeginAccept();

            RPCLogger.Trace("Accepted connection from: " + socket.RemoteEndPoint);

            // Got a new context.
            try
            {
                HttpContext context = CreateContext(socket);
                HttpContext.Current = context;
                context.HttpFactory = factory;
                context.RequestReceived += OnRequest;
                context.Disconnected += OnDisconnect;
                context.ContinueResponseRequested += On100Continue;
                context.Start();
            }
            catch (Exception err)
            {
                RPCLogger.WriteError("ContextReceived raised an exception: " + err.Message);
                socket.Disconnect(true);
            }
        }

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
        public void Start(int backLog)
        {
            if (listener != null)
                throw new InvalidOperationException("Listener have already been started.");

            IsStarted = true;
            listener = new TcpListener(Address, Port);
            listener.Start(backLog);

            if (Port == 0 && listener.LocalEndpoint is IPEndPoint)
                Port = ((IPEndPoint)listener.LocalEndpoint).Port;

            // do not use beginaccept. Let exceptions be thrown.
            Interlocked.Increment(ref pendingAccepts);
            listener.BeginAcceptSocket(OnSocketAccepted, null);
        }

        /// <summary>
        /// Stop listener.
        /// </summary>
        public void Stop()
        {
            shuttingDown = true;
            listener.Stop();
        }
    } // public class HttpListener : IHttpListener
} // namespace TridentFramework.RPC.Http
