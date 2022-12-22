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
using System.Net;

using TridentFramework.RPC.Http.Headers;
using TridentFramework.RPC.Http.HttpMessages.Parser;
using TridentFramework.RPC.Http.Tools;

namespace TridentFramework.RPC.Http.HttpMessages
{
    /// <summary>
    /// Parses and builds messages
    /// </summary>
    /// <remarks>
    /// <para>The message factory takes care of building messages
    /// from all end points.</para>
    /// <para>
    /// Since both message and packet protocols are used, the factory
    /// hands out contexts to all end points. The context keeps a state
    /// to be able to parse partial messages properly.
    /// </para>
    /// <para>
    /// Each end point need to hand the context back to the message factory
    /// when the client disconnects (or a message have been parsed).
    /// </para>
    /// </remarks>
    [Component]
    public class MessageFactory
    {
        private readonly ObjectPool<MessageFactoryContext> builders;
        private readonly HeaderFactory factory;

        /*
        ** Events
        */

        /// <summary>
        /// A request have been received from one of the end points.
        /// </summary>
        public event EventHandler<FactoryRequestEventArgs> RequestReceived = delegate { };

        /// <summary>
        /// A response have been received from one of the end points.
        /// </summary>
        public event EventHandler<FactoryResponseEventArgs> ResponseReceived = delegate { };

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageFactory"/> class.
        /// </summary>
        /// <param name="factory">Factory used to create headers.</param>
        public MessageFactory(HeaderFactory factory)
        {
            this.factory = factory;
            builders = new ObjectPool<MessageFactoryContext>(CreateBuilder);
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        private MessageFactoryContext CreateBuilder()
        {
            var mb = new MessageFactoryContext(this, factory, new HttpParser());
            mb.RequestCompleted += OnRequest;
            mb.ResponseCompleted += OnResponse;
            return mb;
        }

        /// <summary>
        /// Create a new message factory context.
        /// </summary>
        /// <returns>A new context.</returns>
        /// <remarks>
        /// A context is used to parse messages from a specific endpoint.
        /// </remarks>
        internal MessageFactoryContext CreateNewContext()
        {
            return builders.Dequeue();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="method"></param>
        /// <param name="uri"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        internal IRequest CreateRequest(string method, string uri, string version)
        {
            return new Request(method, uri, version);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="version"></param>
        /// <param name="statusCode"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        internal IResponse CreateResponse(string version, HttpStatusCode statusCode, string reason)
        {
            return new Response(version, statusCode, reason);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnRequest(object sender, FactoryRequestEventArgs e)
        {
            RequestReceived(this, e);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnResponse(object sender, FactoryResponseEventArgs e)
        {
            ResponseReceived(this, e);
        }

        /// <summary>
        /// Release a used factory context.
        /// </summary>
        /// <param name="factoryContext"></param>
        internal void Release(MessageFactoryContext factoryContext)
        {
            builders.Enqueue(factoryContext);
        }
    } // public class MessageFactory
} // namespace TridentFramework.RPC.Http.HttpMessages
