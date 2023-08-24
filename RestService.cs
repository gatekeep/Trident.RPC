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

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using HttpStatusCode = System.Net.HttpStatusCode;
using IPAddress = System.Net.IPAddress;

using Newtonsoft.Json.Linq;

using TridentFramework.RPC.Http;
using TridentFramework.RPC.Http.Headers;
using TridentFramework.RPC.Http.Service;
using TridentFramework.RPC.Net;
using TridentFramework.RPC.RestDoc;
using TridentFramework.RPC.Utility;

namespace TridentFramework.RPC.Http
{
    /// <summary>
    /// Defines an REST API "service" or server that services REST API requests.
    /// </summary>
    public class RestService : IDisposable
    {
        public const string HeaderNS = "http://temp.uri/";
        public const string ContentTypeJson = "application/json";
        public const string DocumentationEndpoint = "/apiDoc";

        private bool disposed = false;
        private bool isOpened = false;

        private Uri serviceUri;
        private RPCProxyHelper proxyHelper;

        private RequestWorker httpRequestWorker;
        private HttpListener listener;

        private Thread listenerThread;
        private bool threadRunning;

        [ThreadStatic] private static RestService ctxService;
        private RPCContext context = new RPCContext();

        private Dictionary<string, Type> serviceEndpoints = new Dictionary<string, Type>(16);
        private Dictionary<Type, Type> serviceTypes = new Dictionary<Type, Type>(16);

        private bool enableDocumentation = false;
        private DocumentHandler documentHandler;

        /*
        ** Properties
        */

        /// <summary>
        /// Gets the list of message inspectors for this <see cref="RPCService"/>.
        /// </summary>
        public List<IServiceMessageInspector> MessageInspectors
        {
            get { return context.serviceMessageInspectors; }
        }

        /// <summary>
        /// Gets the list of exception handlers for this <see cref="RPCService"/>.
        /// </summary>
        public List<IRPCExceptionHandler> ExceptionHandlers
        {
            get { return context.exceptionHandlers; }
        }

        /// <summary>
        /// Gets or sets the instance of the <see cref="IAuthenticationGuard"/> for this REST service.
        /// </summary>
        public IAuthenticationGuard AuthenticationGuard
        {
            get { return proxyHelper.AuthenticationGuard; }
            set { proxyHelper.AuthenticationGuard = value; }
        }

        /// <summary>
        /// Gets the list of service endpoints for this <see cref="RestService"/>.
        /// </summary>
        public Dictionary<string, Type> ServiceEndpoints
        {
            get { return serviceEndpoints; }
        }

        /// <summary>
        /// Gets the list of service types for this <see cref="RestService"/>.
        /// </summary>
        public Dictionary<Type, Type> ServiceTypes
        {
            get { return serviceTypes; }
        }

        /// <summary>
        /// Gets the current context of this service.
        /// </summary>
        public static RestService Context
        {
            get { return ctxService; }
        }

        /// <summary>
        /// Flag indicating whether or not documentation endpoints are enabled.
        /// </summary>
        public bool IsDocumentationEnabled
        {
            get { return enableDocumentation; }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="RestService"/> class.
        /// </summary>
        /// <param name="serviceUri"></param>
        /// <param name="enableDocumentation"></param>
        public RestService(Uri serviceUri, bool enableDocumentation = false)
        {
            this.serviceUri = serviceUri;

            this.proxyHelper = new RPCProxyHelper();
            if (enableDocumentation)
            {
                this.enableDocumentation = enableDocumentation;
                this.documentHandler = new DocumentHandler(proxyHelper, this, DocumentationEndpoint);
            }

            this.context.channelMessageInspectors = null;
        }

        /// <summary>
        /// Add a service endpoint to the hosted service, with the specified interface, contract and URI.
        /// </summary>
        /// <param name="interfaceType"></param>
        /// <param name="serviceType"></param>
        /// <param name="uri"></param>
        public void AddServiceEndpoint(Type interfaceType, Type serviceType, Uri uri)
        {
            if (!interfaceType.IsDefined(typeof(RPCContractAttribute), false))
                throw new ArgumentException("Interface doesn't define the RPCContract attribute");

            if (uri.Host != serviceUri.Host)
                throw new ArgumentException("uri");
            if (uri.Port != serviceUri.Port)
                throw new ArgumentException("uri");
            if (!uri.IsAbsoluteUri)
                uri = new Uri(serviceUri, uri);

            if (serviceEndpoints.ContainsKey(uri.AbsolutePath))
                throw new InvalidOperationException("REST Uri endpoint " + uri + " is already defined!");
            if (serviceTypes.ContainsKey(interfaceType))
                throw new InvalidOperationException("REST interface type " + interfaceType.ToString() + " is already defined as a REST endpoint!");

            serviceEndpoints.Add(uri.AbsolutePath, interfaceType);
            serviceTypes.Add(interfaceType, serviceType);
        }

        /// <summary>
        /// Add a service endpoint to the hosted service, with the specified interface, contract and URI.
        /// </summary>
        /// <param name="interfaceType"></param>
        /// <param name="serviceType"></param>
        /// <param name="uri"></param>
        public void AddServiceEndpoint(Type interfaceType, Type serviceType, string uri)
        {
            AddServiceEndpoint(interfaceType, serviceType, new Uri(serviceUri, uri));
        }

        /// <summary>
        /// Causes the communication object to transition from its current state
        /// into the opened state.
        /// </summary>
        /// <param name="cert"></param>
        public void Open(X509Certificate cert = null)
        {
            if (isOpened)
                throw new InvalidOperationException("REST server is already opened");

            // initialize the request handler thread
            listenerThread = new Thread(new ThreadStart(HttpListenerThread));

            // initialize the request worker
            httpRequestWorker = new RequestWorker();
            httpRequestWorker.OnProcessRequest += HandleHttpRequest;

            // initialize the http listener
            IPAddress addr = NetUtility.Resolve(serviceUri.Host);
            if (cert != null)
                listener = new SecureHttpListener(addr, serviceUri.Port, cert);
            else
                listener = HttpListener.Create(addr, serviceUri.Port);
            listener.RequestReceived += (object sender, RequestEventArgs e) => { httpRequestWorker.ProcessRequest(listener, e.Context); };

            // start the request handler thread
            threadRunning = true;
            listenerThread.Start();

            isOpened = true;
        }

        /// <summary>
        /// Causes the communication object to transition from its current state
        /// to the closed state.
        /// </summary>
        public void Close()
        {
            if (!isOpened)
                throw new InvalidOperationException("REST server is already closed");

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

            isOpened = false;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose(bool disposing) executes in two distinct scenarios.
        /// If disposing equals true, the method has been called directly
        /// or indirectly by a user's code. Managed and unmanaged resources
        /// can be disposed.
        /// If disposing equals false, the method has been called by the
        /// runtime from inside the finalizer and you should not reference
        /// other objects. Only unmanaged resources can be disposed.
        /// </summary>
        private void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this.disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    if (isOpened)
                        Close();
                }
            }
            disposed = true;
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

        /// <summary>
        /// Internal helper to send an REST result back to the caller.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="methodInfo"></param>
        /// <param name="ex"></param>
        /// <returns></returns>
        private bool SendFaultResult(RPCMessage message, MethodInfo methodInfo, Exception ex)
        {
            if (message.MessageType != RPCMessageType.Json_REST)
                throw new ArgumentException("message");

            bool handled = false;
            JObject fault = null;

            IHttpContext httpContext = message.IncomingMessageProperties[RPCProxyHelper.RPC_MSG_PROP_HTTP_CONTEXT] as IHttpContext;
            if (httpContext == null)
                throw new ArgumentNullException("httpContext");

            // iterate through the exception handlers
            foreach (IRPCExceptionHandler handler in context.exceptionHandlers)
            {
                if (!handler.HandleError(ex))
                {
                    handler.ProvideFault(ex, ref fault);
                    if (fault == null)
                    {
                        fault = proxyHelper.PrepareRESTFaultResponse(methodInfo, ex);
                        break;
                    }
                }
                else
                    handled = true;
            }

            // if we have no custom handlers...
            if (context.exceptionHandlers.Count == 0)
                fault = proxyHelper.PrepareRESTFaultResponse(methodInfo, ex);

            if (!handled)
            {
                if (fault == null)
                    fault = new JObject();

                // convert context headers into HTTP-type headers
                Dictionary<string, string> httpOutgoingHeaders = RPCMessage.BuildOutgoingHeaders(RPCContext.ctxOutgoingHeaders);

                string result = fault.ToString();
                httpRequestWorker.RespondWithString(httpContext, result.ToString(), httpOutgoingHeaders, HttpStatusCode.OK, ContentTypeJson);
                return false;
            }
            else
                return true;
        }

        /// <summary>
        /// Internal helper to handle the REST request.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="methodName"></param>
        /// <param name="serviceType"></param>
        /// <param name="interfaceType"></param>
        /// <returns></returns>
        private void ProcessRESTRequest(RPCMessage message, string methodName, Type serviceType, Type interfaceType)
        {
            if (message.MessageType != RPCMessageType.Json_REST)
                throw new ArgumentException("message");

            IHttpContext httpContext = message.IncomingMessageProperties[RPCProxyHelper.RPC_MSG_PROP_HTTP_CONTEXT] as IHttpContext;
            if (httpContext == null)
                throw new ArgumentNullException("httpContext");

            // process message headers
            message.ReadIncomingHeaders();

            // perform message inspector behaviors after receiving request
            foreach (IServiceMessageInspector inspector in context.serviceMessageInspectors)
                inspector.AfterRecieveRequest(message);

            // set context data
            {
                context.Reset();
                RPCContext.ctxOutgoingHeaders = new MessageHeaders();
                RPCContext.ctxMessage = message;
                RPCContext.ctxCurrent = context;
                ctxService = this;
            }

            MethodBase ifaceMethodBase = proxyHelper.GetInheritanceMethod(methodName, interfaceType);
            bool userEndpointHandler = false;
            if (ifaceMethodBase != null)
            {
                UserEndpointHandlerAttribute userHandlerAttr = ifaceMethodBase.GetCustomAttribute<UserEndpointHandlerAttribute>();
                if (userHandlerAttr != null)
                    userEndpointHandler = true;
            }

            // convert context headers into HTTP-type headers
            Dictionary<string, string> httpOutgoingHeaders = RPCMessage.BuildOutgoingHeaders(RPCContext.ctxOutgoingHeaders);

            UriTemplateMatch httpQueryTemplate = message.IncomingMessageProperties[RPCProxyHelper.RPC_MSG_PROP_HTTP_QUERY_TEMPLATE] as UriTemplateMatch;
            if (httpQueryTemplate == null && !userEndpointHandler)
            {
                httpRequestWorker.RespondWithString(httpContext, string.Empty, httpOutgoingHeaders, HttpStatusCode.MethodNotAllowed, ContentTypeJson);
                return; // ? - should we throw an exception?
            }

            MethodInfo methodInfo = null;
            object ret = null;
            List<Tuple<string, Type>> outTypes = null;
            object[] outArgs = null;

            if (proxyHelper.ProcessRESTRequest(context, methodName, serviceType, interfaceType, out methodInfo, out ret, out outTypes, out outArgs,
                SendFaultResult))
            {
                // perform message inspector behaviors before sending response
                foreach (IServiceMessageInspector inspector in context.serviceMessageInspectors)
                    inspector.BeforeSendReply(context, ref message);

                if (ifaceMethodBase != null)
                {
                    UserEndpointHandlerAttribute userHandlerAttr = ifaceMethodBase.GetCustomAttribute<UserEndpointHandlerAttribute>();
                    if (userHandlerAttr != null && context.Message.MessageBody == null)
                        return;
                }

                // rebuild outgoing headers
                httpOutgoingHeaders = RPCMessage.BuildOutgoingHeaders(context.OutgoingMessageHeaders);

                if ((httpContext.Request.Method == Method.Head) || (httpContext.Request.Method == Method.Options))
                    httpRequestWorker.RespondWithString(httpContext, string.Empty, httpOutgoingHeaders, HttpStatusCode.OK, ContentTypeJson);
                else
                {
                    string result = string.Empty;
                    if (context.UseMessageAsResponse)
                    {
                        if (message.MessageBody == null)
                            message = context.Message;
                        if (message.MessageBody != null)
                            result = message.MessageBody.ToString();
                        else
                            result = proxyHelper.PrepareRESTFaultResponse(methodInfo, new MethodAccessException("REST method returned no message body.")).ToString();
                    }
                    else
                        result = proxyHelper.PrepareRESTResult(methodInfo, ret, outTypes, outArgs);
                    httpRequestWorker.RespondWithString(httpContext, result.ToString(), httpOutgoingHeaders, HttpStatusCode.OK, ContentTypeJson);
                }
            }
        }

        /// <summary>
        /// Internal helper to handle the raw HTTP request.
        /// </summary>
        /// <param name="listener"></param>
        /// <param name="context"></param>
        private bool HandleHttpRequest(IHttpListener listener, IHttpContext context)
        {
            // ignore requests for "favicon.ico" return 404
            if (context.Request.Uri.AbsoluteUri.Contains("favicon.ico"))
                return false;

            Type interfaceType = null, serviceType = null;
            Uri basePath = null;
            proxyHelper.GetUriAndTypes(context.Request.Uri, serviceEndpoints, serviceTypes, out basePath, out interfaceType, out serviceType, DocumentationEndpoint);

            if (interfaceType == null)
                return false;
            if (serviceType == null)
                return false;

            // are documentation endpoints enabled?
            if (enableDocumentation)
            {
                if (documentHandler.Generate(httpRequestWorker, context, basePath, interfaceType))
                    return true;
            }

            // attempt to validate content type
            if (context.Request.ContentType != null)
            {
                if ((context.Request.ContentType.Value != "application/json") && (context.Request.ContentType.Value != "multipart/form-data"))
                    throw new InvalidOperationException("Request must be a JSON or form data request");
            }

            UriTemplateMatch utm = null;

            string methodName = string.Empty;
            MethodInfo[] interfaceMethods = proxyHelper.GetInheritanceMethods(interfaceType);
            foreach (MethodInfo info in interfaceMethods)
            {
                RestMethodAttribute restInvoke = info.GetCustomAttribute<RestMethodAttribute>();
                if (restInvoke != null)
                {
                    if (restInvoke.Method == context.Request.Method)
                    {
                        UriTemplate template = new UriTemplate(restInvoke.UriTemplate);
                        utm = template.Match(basePath, context.Request.Uri);
                        if (utm != null)
                        {
                            string requestParameters = context.Request.Uri.GetComponents(UriComponents.Query, UriFormat.SafeUnescaped);
                            if (requestParameters != string.Empty && utm.Template.ToString() == string.Empty)
                                continue;

                            methodName = info.Name;
                            break;
                        }
                    }
                }
            }

            // do we still have no method name?
            if (methodName == string.Empty)
            {
                foreach (MethodInfo info in interfaceMethods)
                {
                    RestMethodAttribute restInvoke = info.GetCustomAttribute<RestMethodAttribute>();
                    if (restInvoke != null)
                    {
                        if (restInvoke.Method == context.Request.Method)
                        {
                            // is this a user handler?
                            UserEndpointHandlerAttribute userHandlerAttr = info.GetCustomAttribute<UserEndpointHandlerAttribute>();
                            if (userHandlerAttr != null)
                            {
                                methodName = info.Name;
                                break;
                            }
                        }
                    }
                }
            }

            RPCMessage message = new RPCMessage(RPCMessageType.Json_REST, MessageDirection.Request, basePath, context.Request.Uri);

            // set message incoming properties
            {
                message.IncomingMessageProperties.Add("Via", context.Request.Uri);
                message.IncomingMessageProperties.Add(RPCProxyHelper.RPC_MSG_PROP_HTTP_REQUEST_WORKER, httpRequestWorker);
                message.IncomingMessageProperties.Add(RPCProxyHelper.RPC_MSG_PROP_HTTP_REQUEST_METHOD, context.Request.Method);
                message.IncomingMessageProperties.Add(RPCProxyHelper.RPC_MSG_PROP_HTTP_QUERY_TEMPLATE, utm);
                message.IncomingMessageProperties.Add(RPCProxyHelper.RPC_MSG_PROP_HTTP_CONTEXT, context);
                message.IncomingMessageProperties.Add(RPCProxyHelper.RPC_MSG_PROP_HTTP_REQUEST, context.Request);
            }

            ProcessRESTRequest(message, methodName, serviceType, interfaceType);
            return true; // always return true, otherwise the HTTP server engine will return a 404
        }
    } // public class RestService : IDisposable
} // namespace TridentFramework.RPC.Http
