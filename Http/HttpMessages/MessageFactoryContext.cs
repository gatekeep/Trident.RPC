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
using System.IO;
using System.Net;

using TridentFramework.RPC.Http.Headers;
using TridentFramework.RPC.Http.HttpMessages.Parser;

using TridentFramework.RPC.Utility;

namespace TridentFramework.RPC.Http.HttpMessages
{
    /// <summary>
    /// Creates a single message for one of the end points.
    /// </summary>
    /// <remarks>
    /// The factory is
    /// </remarks>
    public class MessageFactoryContext : IDisposable
    {
        private readonly HeaderFactory factory;
        private readonly MessageFactory msgFactory;
        private readonly HttpParser parser;
        private IMessage message;

        /*
        ** Events
        */

        /// <summary>
        /// A request have been successfully parsed.
        /// </summary>
        public event EventHandler<FactoryRequestEventArgs> RequestCompleted = delegate { };

        /// <summary>
        /// A response have been successfully parsed.
        /// </summary>
        public event EventHandler<FactoryResponseEventArgs> ResponseCompleted = delegate { };

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
        /// Initializes a new instance of the <see cref="MessageFactoryContext"/> class.
        /// </summary>
        /// <param name="msgFactory">The MSG factory.</param>
        /// <param name="factory">The factory.</param>
        /// <param name="parser">The parser.</param>
        public MessageFactoryContext(MessageFactory msgFactory, HeaderFactory factory, HttpParser parser)
        {
            this.msgFactory = msgFactory;
            this.factory = factory;
            this.parser = parser;
            parser.HeaderParsed += OnHeader;
            parser.MessageComplete += OnMessageComplete;
            parser.RequestLineParsed += OnRequestLine;
            parser.ResponseLineParsed += OnResponseLine;
            parser.BodyBytesReceived += OnBody;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            /* stub */
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private void OnBody(object sender, BodyEventArgs e)
        {
            message.Body.Write(e.Buffer, e.Offset, e.Count);
        }

        /// <summary>
        /// Received a header from parser
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnHeader(object sender, HeaderEventArgs e)
        {
            RPCLogger.Trace(e.Name + ": " + e.Value);
            IHeader header = factory.Parse(e.Name, e.Value);
            message.Add(header.Name.ToLower(), header);
            if (header.Name.ToLower() == "expect" && e.Value.ToLower().Contains("100-continue"))
            {
                Console.WriteLine("Got 100 continue request.");
                ContinueResponseRequested(this, new ContinueEventArgs((IRequest)message));
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private void OnMessageComplete(object sender, EventArgs e)
        {
            message.Body.Seek(0, SeekOrigin.Begin);
            if (message is IRequest)
                RequestCompleted(this, new FactoryRequestEventArgs((IRequest)message));
            else
                ResponseCompleted(this, new FactoryResponseEventArgs((IResponse)message));
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private void OnRequestLine(object sender, RequestLineEventArgs e)
        {
            RPCLogger.Trace(e.Method + ": " + e.UriPath);
            message = msgFactory.CreateRequest(e.Method, e.UriPath, e.Version);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private void OnResponseLine(object sender, ResponseLineEventArgs e)
        {
            RPCLogger.Trace(e.StatusCode + ": " + e.ReasonPhrase);
            message = msgFactory.CreateResponse(e.Version, e.StatusCode, e.ReasonPhrase);
        }

        /// <summary>
        /// Will continue the parsing until nothing more can be parsed.
        /// </summary>
        /// <param name="buffer">buffer to parse</param>
        /// <param name="offset">where to start in the buffer</param>
        /// <param name="length">number of bytes to process.</param>
        /// <returns>Position where parser stopped parsing.</returns>
        /// <exception cref="ParserException">Parsing failed.</exception>
        public int Parse(byte[] buffer, int offset, int length)
        {
            return parser.Parse(buffer, offset, length);
        }

        /// <summary>
        /// Reset parser.
        /// </summary>
        /// <remarks>
        /// Something failed, reset parser so it can start on a new request.
        /// </remarks>
        public void Reset()
        {
            parser.Reset();
        }
    } // public class MessageFactoryContext : IDisposable

    /// <summary>
    /// Used to notify about 100-continue header.
    /// </summary>
    public class ContinueEventArgs : EventArgs
    {
        /*
        ** Properties
        */

        /// <summary>
        /// Gets request that want to continue
        /// </summary>
        public IRequest Request { get; private set; }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="ContinueEventArgs"/> class.
        /// </summary>
        /// <param name="request">request that want to continue.</param>
        public ContinueEventArgs(IRequest request)
        {
            Request = request;
        }
    } // public class ContinueEventArgs : EventArgs
} // namespace TridentFramework.RPC.Http.HttpMessages
