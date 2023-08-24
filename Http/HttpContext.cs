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
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using TridentFramework.RPC.Http.Headers;
using TridentFramework.RPC.Http.HttpMessages;
using TridentFramework.RPC.Http.HttpMessages.Parser;
using TridentFramework.RPC.Http.Tools;
using TridentFramework.RPC.Http.Transports;

using TridentFramework.RPC.Utility;

namespace TridentFramework.RPC.Http
{
    /// <summary>
    /// A HTTP context
    /// </summary>
    /// <remarks>
    ///
    /// </remarks>
    [Component]
    public class HttpContext : IHttpContext, IDisposable
    {
        private static readonly ObjectPool<byte[]> Buffers = new ObjectPool<byte[]>(() => new byte[65535]);
        [ThreadStatic] private static IHttpContext context;
        private readonly byte[] buffer;
        private Timer keepAlive;
        private int keepAliveTimeout = 100000; // 100 seconds.

        /*
        ** Properties
        */

        /// <summary>
        /// Gets currently executing HTTP context.
        /// </summary>
        public static IHttpContext Current
        {
            get { return context; }
            internal set { context = value; }
        }

        /// <summary>
        /// Gets or sets description
        /// </summary>
        internal HttpFactory HttpFactory { get; set; }

        /// <summary>
        /// gets factory used to build request objects
        /// </summary>
        internal MessageFactoryContext MessageFactoryContext { get; private set; }

        /// <summary>
        /// Gets socket
        /// </summary>
        internal Socket Socket { get; private set; }

        /// <summary>
        /// Gets remove end point
        /// </summary>
        public IPEndPoint RemoteEndPoint
        {
            get { return (IPEndPoint)Socket.RemoteEndPoint; }
        }

        /// <summary>
        /// Gets network stream.
        /// </summary>
        public Stream Stream { get; private set; }

        /// <summary>
        /// Gets the currently handled request
        /// </summary>
        /// <value>The request.</value>
        public IRequest Request { get; internal set; }

        /// <summary>
        /// Gets the response that is going to be sent back
        /// </summary>
        /// <value>The response.</value>
        public IResponse Response { get; internal set; }

        /// <summary>
        /// Gets if current context is using a secure connection.
        /// </summary>
        public virtual bool IsSecure
        {
            get { return false; }
        }

        /*
        ** Events
        */

        /// <summary>
        /// Triggered for all requests in the server (after the response have been sent)
        /// </summary>
        public static event EventHandler<RequestEventArgs> CurrentRequestCompleted = delegate { };

        /// <summary>
        /// Triggered for current request (after the response have been sent)
        /// </summary>
        public event EventHandler<RequestEventArgs> RequestCompleted = delegate { };

        /// <summary>
        /// A new request have been received.
        /// </summary>
        public event EventHandler<RequestEventArgs> RequestReceived = delegate { };

        /// <summary>
        /// A new request have been received (invoked for ALL requests)
        /// </summary>
        public static event EventHandler<RequestEventArgs> CurrentRequestReceived = delegate { };

        /// <summary>
        /// Client have been disconnected.
        /// </summary>
        public event EventHandler Disconnected = delegate { };

        /// <summary>
        /// Client asks if he may continue.
        /// </summary>
        /// <remarks>
        /// If the body is too large or anything like that you should respond <see cref="HttpStatusCode.ExpectationFailed"/>.
        /// </remarks>
        public event EventHandler<ContinueEventArgs> ContinueResponseRequested = delegate { };

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpContext"/> class.
        /// </summary>
        /// <param name="socket">Socket received from HTTP listener.</param>
        /// <param name="context">Context used to parse incoming messages.</param>
        public HttpContext(Socket socket, MessageFactoryContext context)
        {
            Socket = socket;
            MessageFactoryContext = context;
            MessageFactoryContext.RequestCompleted += OnRequest;
            MessageFactoryContext.ContinueResponseRequested += On100Continue;
            buffer = Buffers.Dequeue();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            Buffers.Enqueue(buffer);
            Close();
        }

        /// <summary>
        /// Disconnect context.
        /// </summary>
        public void Disconnect()
        {
            Close();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private void On100Continue(object sender, ContinueEventArgs e)
        {
            ContinueResponseRequested(this, e);
        }

        /// <summary>
        /// Close and release socket.
        /// </summary>
        private void Close()
        {
            lock (this)
            {
                if (Socket == null)
                    return;

                try
                {
                    if (keepAlive != null)
                    {
                        keepAlive.Dispose();
                        keepAlive = null;
                    }

                    Socket.Disconnect(true);
                    Socket.Close();
                    Socket = null;
                    Stream.Dispose();
                    Stream = null;
                    MessageFactoryContext.RequestCompleted -= OnRequest;
                    MessageFactoryContext.ContinueResponseRequested -= On100Continue;
                    MessageFactoryContext.Reset();
                }
                catch (Exception e)
                {
                    RPCLogger.StackTrace("Failed to close context properly", e, false);
                }
            }
            Disconnected(this, EventArgs.Empty);
        }

        /// <summary>
        /// Create stream used to send and receive bytes from the socket.
        /// </summary>
        /// <param name="socket">Socket to wrap</param>
        /// <returns>Stream</returns>
        /// <exception cref="InvalidOperationException">Stream could not be created.</exception>
        protected virtual Stream CreateStream(Socket socket)
        {
            return new ReusableSocketNetworkStream(socket, true);
        }

        /// <summary>
        /// Interpret incoming data.
        /// </summary>
        /// <param name="ar"></param>
        private void OnReceive(IAsyncResult ar)
        {
            // been closed by our side.
            if (Stream == null)
                return;

            context = this;
            HttpFactory.Current = HttpFactory;

            try
            {
                int bytesLeft = Stream.EndRead(ar);
                if (bytesLeft == 0)
                {
                    RPCLogger.Trace("Client disconnected");
                    Close();
                    return;
                }

                RPCLogger.Trace(Socket.RemoteEndPoint + " received " + bytesLeft + " bytes");

                if (bytesLeft < 5000)
                {
                    string temp = Encoding.Default.GetString(buffer, 0, bytesLeft);
                    RPCLogger.Trace(temp);
                }

                int offset = ParseBuffer(bytesLeft);
                bytesLeft -= offset;

                if (bytesLeft > 0)
                {
                    RPCLogger.WriteWarning("Moving " + bytesLeft + " from " + offset + " to beginning of array");
                    Buffer.BlockCopy(buffer, offset, buffer, 0, bytesLeft);
                    offset += bytesLeft;
                }
                else
                    offset = 0;

                Stream.BeginRead(buffer, offset, buffer.Length - offset, OnReceive, null);
            }
            catch (ParserException err)
            {
                RPCLogger.WriteWarning(err.ToString());
                var response = new Response("HTTP/1.0", HttpStatusCode.BadRequest, err.Message);
                var generator = HttpFactory.Current.Get<ResponseWriter>();
                generator.SendErrorPage(this, response, err);
                Close();
            }
            catch (Exception err)
            {
                if (!(err is IOException))
                {
                    RPCLogger.StackTrace("Failed to read from stream: ", err, false);
                    ResponseWriter responseWriter = HttpFactory.Current.Get<ResponseWriter>();
                    Response response = new Response("HTTP/1.0", HttpStatusCode.InternalServerError, err.Message);
                    responseWriter.SendErrorPage(this, response, err);
                }

                // is this a socket exception of some sort?
                if (err is IOException && err.InnerException != null)
                {
                    if (err.InnerException is SocketException)
                    {
                        SocketException se = (SocketException)err.InnerException;
                        switch (se.SocketErrorCode)
                        {
                            case SocketError.ConnectionAborted:
                                RPCLogger.WriteWarning($"HTTP connection was aborted by the remote application.");
                                break;
                            default:
                                RPCLogger.StackTrace("Failed to read from stream: ", err, false);
                                break;
                        }
                    }
                    else
                        RPCLogger.StackTrace("Failed to read from stream: ", err, false);
                }

                Close();
            }
        }

        /// <summary>
        /// A request was received from the parser.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnRequest(object sender, FactoryRequestEventArgs e)
        {
            context = this;
            Response = HttpFactory.Current.Get<IResponse>(this, e.Request);
            RPCLogger.Trace("Received '" + e.Request.Method + " " + e.Request.Uri.PathAndQuery + "' from " +
                          Socket.RemoteEndPoint);

            // keep alive.
            if (e.Request.Connection != null && e.Request.Connection.Type == ConnectionType.KeepAlive)
            {
                Response.Add(new StringHeader("Keep-Alive", "timeout=5, max=100"));

                // refresh timer
                if (keepAlive != null)
                    keepAlive.Change(keepAliveTimeout, keepAliveTimeout);
            }

            Request = e.Request;
            CurrentRequestReceived(this, new RequestEventArgs(this, e.Request, Response));
            RequestReceived(this, new RequestEventArgs(this, e.Request, Response));

            //
            if (Response.Connection.Type == ConnectionType.KeepAlive)
            {
                if (keepAlive == null)
                    keepAlive = new Timer(OnConnectionTimeout, null, keepAliveTimeout, keepAliveTimeout);
            }

            RequestCompleted(this, new RequestEventArgs(this, e.Request, Response));
            CurrentRequestCompleted(this, new RequestEventArgs(this, e.Request, Response));
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="state"></param>
		private void OnConnectionTimeout(object state)
        {
            if (keepAlive != null)
                keepAlive.Dispose();
            RPCLogger.Trace("Keep-Alive timeout");
            Disconnect();
        }

        /// <summary>
        /// Parse all complete requests in buffer.
        /// </summary>
        /// <param name="bytesLeft"></param>
        /// <returns>offset in buffer where parsing stopped.</returns>
        /// <exception cref="InvalidOperationException">Parsing failed.</exception>
        private int ParseBuffer(int bytesLeft)
        {
            int offset = MessageFactoryContext.Parse(buffer, 0, bytesLeft);
            bytesLeft -= offset;

            // try another pass if we got bytes left.
            if (bytesLeft <= 0)
                return offset;

            // Continue until offset is not changed.
            int oldOffset = 0;
            while (offset != oldOffset)
            {
                oldOffset = offset;
                RPCLogger.Trace("Parsing from index " + offset + ", " + bytesLeft + " bytes");
                offset = MessageFactoryContext.Parse(buffer, offset, bytesLeft);
                bytesLeft -= offset;
            }
            return offset;
        }

        /// <summary>
        /// Start content.
        /// </summary>
        /// <exception cref="SocketException">A socket operation failed.</exception>
        /// <exception cref="IOException">Reading from stream failed.</exception>
        internal void Start()
        {
            Stream = CreateStream(Socket);
            Stream.BeginRead(buffer, 0, buffer.Length, OnReceive, null);
        }
    } // public class HttpContext : IHttpContext, IDisposable
} // namespace TridentFramework.RPC.Http
