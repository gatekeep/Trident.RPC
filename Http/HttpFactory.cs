/**
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
using System.Collections.Generic;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

using TridentFramework.RPC.Http.Headers;
using TridentFramework.RPC.Http.HttpMessages;

using TridentFramework.RPC.Utility;

namespace TridentFramework.RPC.Http
{
    /// <summary>
    /// Delegate used to create a certain type
    /// </summary>
    /// <returns>Created type.</returns>
    /// <remarks>
    /// Method must never fail.
    /// </remarks>
    public delegate object FactoryMethod(Type type, object[] arguments);

    /// <summary>
    /// Used to create all key types in the HTTP server.
    /// </summary>
    /// <remarks>
    /// <para>Should have factory methods at least for the following types:
    /// <see cref="IRequest"/>, <see cref="IResponse"/>,
    /// <see cref="HeaderFactory"/>, <see cref="MessageFactory"/>,
    /// <see cref="HttpContext"/>, <see cref="SecureHttpContext"/>,
    /// <see cref="IResponse"/>, <see cref="IRequest"/>,
    /// <see cref="ResponseWriter"/>.
    /// </para>
    /// <para>Check the default implementations to see which constructor
    /// parameters you will get.</para>
    /// </remarks>
    /// <example>
    /// HttpFactory.Add(typeof(IRequest), (type, args) => new MyRequest((string)args[0]));
    /// </example>
    ///
    public class HttpFactory : IHttpFactory
    {
        [ThreadStatic] private static IHttpFactory current;
        private readonly Dictionary<Type, FactoryMethod> methods = new Dictionary<Type, FactoryMethod>();
        private HeaderFactory headerFactory;
        private MessageFactory messageFactory;

        /*
        ** Properties
        */

        /// <summary>
        /// Gets http factory for the current listener.
        /// </summary>
        internal static IHttpFactory Current
        {
            get { return current; }
            set { current = value; }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpFactory"/> class.
        /// </summary>
        public HttpFactory()
        {
            current = this;
            AddDefaultCreators();
        }

        /// <summary>
        /// Add a factory method for a type.
        /// </summary>
        /// <param name="type">Type to create</param>
        /// <param name="handler">Method creating the type.</param>
        public void Add(Type type, FactoryMethod handler)
        {
            methods[type] = handler;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="type"></param>
        /// <param name="handler"></param>
        private void AddDefault(Type type, FactoryMethod handler)
        {
            if (methods.ContainsKey(type))
                return;

            methods[type] = handler;
        }

        /// <summary>
        ///
        /// </summary>
        private void AddDefaultCreators()
        {
            AddDefault(typeof(HeaderFactory), OnSetupHeaderFactory);
            AddDefault(typeof(MessageFactory), OnSetupMessageFactory);
            AddDefault(typeof(HttpContext), CreateHttpContext);
            AddDefault(typeof(SecureHttpContext), CreateSecureHttpContext);
            AddDefault(typeof(IResponse), CreateResponse);
            AddDefault(typeof(IRequest), CreateRequest);
            AddDefault(typeof(ResponseWriter), CreateResponseGenerator);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="type"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        private object CreateHttpContext(Type type, object[] arguments)
        {
            MessageFactoryContext context = Get<MessageFactory>().CreateNewContext();
            var httpContext = new HttpContext((Socket)arguments[0], context);
            httpContext.Disconnected += OnContextDisconnected;
            return httpContext;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="type"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        private object CreateRequest(Type type, object[] arguments)
        {
            return new Request((string)arguments[0], (string)arguments[1], (string)arguments[1]);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="type"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        private object CreateResponse(Type type, object[] arguments)
        {
            return new Response((IHttpContext)arguments[0], (IRequest)arguments[1]);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="type"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        private object CreateResponseGenerator(Type type, object[] arguments)
        {
            return new ResponseWriter();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="type"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        private object CreateSecureHttpContext(Type type, object[] arguments)
        {
            MessageFactoryContext context = Get<MessageFactory>().CreateNewContext();
            var certificate = (X509Certificate)arguments[0];
            var protocols = (SslProtocols)arguments[1];
            var httpContext = new SecureHttpContext(certificate, protocols, (Socket)arguments[2], context);
            httpContext.Disconnected += OnContextDisconnected;
            return httpContext;
        }

        /// <summary>
        /// Used to
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        protected virtual FactoryMethod FindFactoryMethod(Type type)
        {
            FactoryMethod method;
            if (!methods.TryGetValue(type, out method))
            {
                RPCLogger.WriteWarning("Failed to find factory method for " + type.FullName);
                return null;
            }

            return method;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnContextDisconnected(object sender, EventArgs e)
        {
            var context = (HttpContext)sender;
            context.Disconnected -= OnContextDisconnected;
            messageFactory.Release(context.MessageFactoryContext);
        }

        /// <summary>
        /// Setup our singleton.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        /// <remarks>
        /// We want to use a singleton, but we also want to be able
        /// to let the developer to setup his own header factory.
        /// Therefore we use this method to create our own factory only if the user
        /// have not specified one.
        /// </remarks>
        private object OnSetupHeaderFactory(Type type, object[] arguments)
        {
            headerFactory = new HeaderFactory();
            headerFactory.AddDefaultParsers();
            methods[typeof(HeaderFactory)] = (type2, args) => headerFactory;
            return headerFactory;
        }

        /// <summary>
        /// Small method to create a message factory singleton and replace then default delegate method.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        private object OnSetupMessageFactory(Type type, object[] arguments)
        {
            messageFactory = new MessageFactory(Get<HeaderFactory>());
            methods[type] = (type2, args) => messageFactory;
            return messageFactory;
        }

        /// <summary>
        /// Create a type.
        /// </summary>
        /// <typeparam name="T">Type to create</typeparam>
        /// <returns>Created type.</returns>
        public T Get<T>(params object[] constructorArguments) where T : class
        {
            Type type = typeof(T);
            FactoryMethod method = FindFactoryMethod(type);
            if (method == null)
            {
                RPCLogger.WriteError("No factory method is associated with '" + type.FullName + "'");
                return null;
            }

            object createdType = method(type, constructorArguments);
            if (createdType == null)
            {
                RPCLogger.WriteError("Factory method failed to create type '" + type.FullName + "'");
                return null;
            }

            var instance = createdType as T;
            if (instance == null)
            {
                RPCLogger.WriteError("Factory method assigned to '" + type.FullName + "' created a incompatible type '" + createdType.GetType().FullName);
                return null;
            }

            return instance;
        }
    } // public class HttpFactory : IHttpFactory
} // namespace TridentFramework.RPC.Http
