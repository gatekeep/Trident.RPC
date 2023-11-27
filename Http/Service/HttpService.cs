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

using System;
using System.Threading;
using IPAddress = System.Net.IPAddress;
using System.Reflection;
using System.ServiceProcess;
using System.Security.Cryptography.X509Certificates;

using TridentFramework.RPC.Utility;

namespace TridentFramework.RPC.Http.Service
{
    /// <summary>
    /// This class serves as the entry point for the application.
    /// </summary>
    public class HttpService : ServiceBase
    {
        private RequestWorker httpWorker;
        private HttpListener listener;

        private Thread listenerThread;
        private bool threadRunning;

        protected X509Certificate sslCert;
        private int port = 8080;

        private bool embeddedResources = false;
        private bool missingWithIndex = false;
        private string webrootPath = string.Empty;

        /*
        ** Properties
        */

        /// <summary>
        /// Gets/sets the webroot path/namespace.
        /// </summary>
        public string WebrootPath
        {
            get;
            set;
        }

        /// <summary>
        /// Flag indicating the servlet engine is running in embedded mode.
        /// </summary>
        public bool Embedded
        {
            get { return embeddedResources; }
        }

        /// <summary>
        /// Flag indicating missing files/directories will be redirected to index.html.
        /// </summary>
        public bool MissingWithIndex
        {
            get { return missingWithIndex; }
        }

        /// <summary>
        /// Gets/sets the executing assembly (this is used for embedded resource locating).
        /// </summary>
        public Assembly ExecutingAssembly
        {
            get;
            set;
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpService"/> class.
        /// </summary>
        /// <param name="port"></param>
        /// <param name="webrootPath"></param>
        /// <param name="embedded"></param>
        /// <param name="missingWithIndex"></param>
        public HttpService(int port, string webrootPath, bool embedded, bool missingWithIndex)
        {
            this.httpWorker = new RequestWorker();
            this.port = port;

            this.webrootPath = webrootPath;
            this.embeddedResources = embedded;
            this.missingWithIndex = missingWithIndex;

            // initialize the request handler thread
            listenerThread = new Thread(new ThreadStart(HttpListenerThread));
            threadRunning = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpService"/> class.
        /// </summary>
        /// <param name="port"></param>
        /// <param name="cert"></param>
        /// <param name="webrootPath"></param>
        /// <param name="embedded"></param>
        /// <param name="missingWithIndex"></param>
        public HttpService(int port, X509Certificate cert, string webrootPath, bool embedded, bool missingWithIndex) :
            this(port, webrootPath, embedded, missingWithIndex)
        {
            this.sslCert = cert;
        }

        /// <summary>
        /// Request handler thread entry point.
        /// </summary>
        private void HttpListenerThread()
        {
            try
            {
                while (threadRunning)
                {
                    if (!listener.IsStarted)
                        listener.Start(5);
                    Thread.Sleep(1);
                }
            }
            catch (ThreadAbortException)
            {
                /* stub */
            }
        }

        /// <inheritdoc />
        protected override void OnStart(string[] args)
        {
            try
            {
                StartService();
            }
            catch (Exception e)
            {
                RPCLogger.StackTrace(e, false);
                OnStop();
            }
        }

        /// <summary>
        /// Starts the web console server.
        /// </summary>
        public virtual void StartService()
        {
            httpWorker = new RequestWorker(webrootPath, ExecutingAssembly, embeddedResources, missingWithIndex);

            // initialize the request handler thread
            listenerThread = new Thread(new ThreadStart(HttpListenerThread));
            threadRunning = true;

            // initialize the http listener
            listener = HttpListener.Create(IPAddress.Any, port);
            listener.RequestReceived += (object sender, RequestEventArgs e) => { httpWorker.ProcessRequest(listener, e, e.Context); };

            // start the request handler thread
            threadRunning = true;
            listenerThread.Start();

            RPCLogger.Trace("started HTTP service (listening on port " + port + ")");
        }

        /// <inheritdoc />
        protected override void OnStop()
        {
            try
            {
                StopService();
            }
            catch (Exception e)
            {
                StopService();
                RPCLogger.StackTrace(e, false);
            }
        }

        /// <summary>
        /// Stops the web console server.
        /// </summary>
        public virtual void StopService()
        {
            // start the request handler thread
            if (threadRunning)
            {
                threadRunning = false;

                try
                {
                    listenerThread.Abort();
                    listenerThread.Join();
                }
                catch (Exception e)
                {
                    RPCLogger.StackTrace(e, false);
                }

                if (listenerThread != null)
                    listenerThread = null;
            }

            // attempt to start the HTTP listener
            if (listener != null)
            {
                listener.Stop();
                listener = null;
            }

            RPCLogger.Trace("stopped HTTP service");
        }
    } // public class HttpService : ServiceBase
} // namespace TridentFramework.RPC.Http.Service
