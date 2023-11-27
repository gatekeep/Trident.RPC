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
using System.Collections.Generic;

using TridentFramework.RPC.Http;
using TridentFramework.RPC.Http.Headers;

using Newtonsoft.Json.Linq;

using TridentFramework.RPC.Utility;

namespace TridentFramework.RPC
{
    /// <summary>
    /// Enumeration of the different RPC message types.
    /// </summary>
    public enum RPCMessageType : byte
    {
        Json_RPC = 1,
        Json_REST = 2
    } // public enum RPCMessageType : byte

    /// <summary>
    /// Enumeration of different message directions.
    /// </summary>
    public enum MessageDirection : byte
    {
        Request = 1,
        Response = 2
    } // public enum MessageDirection : byte

    /// <summary>
    /// Represents a unit of communication between endpoints in a distributed environment.
    /// </summary>
    public sealed class RPCMessage
    {
        public const string HeaderNS = "http://temp.uri/";

        private const string RPC_MSG_HEADERS = "headers";

        private MessageHeaders ctxIncomingHeaders = null;
        private MessageProperties ctxIncomingProperties = null;

        /// <summary>
        /// Type of RPC message
        /// </summary>
        public readonly RPCMessageType MessageType;

        /// <summary>
        /// Direction of the RPC message
        /// </summary>
        public readonly MessageDirection Direction;

        /// <summary>
        /// Base Uri
        /// </summary>
        public readonly Uri BaseUri;

        /// <summary>
        /// RPC Request Uri
        /// </summary>
        public readonly Uri RequestUri;

        /// <summary>
        /// Unique Request Id
        /// </summary>
        public long RequestId;

        /// <summary>
        /// Unique Connection Id
        /// </summary>
        public long ConnectionId;

        /// <summary>
        /// Message body for RPC message
        /// </summary>
        public JObject MessageBody;

        /*
        ** Properties
        */

        /// <summary>
        /// Gets the incoming message headers.
        /// </summary>
        public MessageHeaders IncomingMessageHeaders
        {
            get
            {
                if (ctxIncomingHeaders == null)
                {
                    RPCLogger.WriteWarning("BUGBUG: Incoming headers were not available! This should not happen.");
                    return new MessageHeaders();
                }
                return ctxIncomingHeaders;
            }
        }

        /// <summary>
        /// Gets the incoming message properties.
        /// </summary>
        public MessageProperties IncomingMessageProperties
        {
            get
            {
                if (ctxIncomingProperties == null)
                {
                    RPCLogger.WriteWarning("BUGBUG: Incoming message properties were not available! This should not happen.");
                    return new MessageProperties();
                }
                return ctxIncomingProperties;
            }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="RPCMessage"/> class.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="direction"></param>
        /// <param name="baseUri"></param>
        /// <param name="requestUri"></param>
        public RPCMessage(RPCMessageType type, MessageDirection direction, Uri baseUri, Uri requestUri)
        {
            this.MessageType = type;
            this.Direction = direction;
            this.BaseUri = baseUri;
            this.RequestUri = requestUri;

            this.ctxIncomingProperties = new MessageProperties();

            // null all other parameters (if possible)
            this.RequestId = 0;
            this.ConnectionId = 0;
            this.MessageBody = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RPCMessage"/> class.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="direction"></param>
        /// <param name="baseUri"></param>
        /// <param name="requestUri"></param>
        /// <param name="body"></param>
        public RPCMessage(RPCMessageType type, MessageDirection direction, Uri baseUri, Uri requestUri, JObject body) :
            this(type, direction, baseUri, requestUri)
        {
            this.MessageBody = body;
        }

        /// <summary>
        /// Helper to create headers from the JSON message body.
        /// </summary>
        public void ReadIncomingHeaders()
        {
            IHttpContext httpContext = ctxIncomingProperties["HttpContext"] as IHttpContext;

            // handle processing HTTP incoming headers
            if (httpContext != null)
            {
                ctxIncomingHeaders = new MessageHeaders();

                // process HTTP headers
                foreach (IHeader httpHeader in httpContext.Request.Headers)
                    ctxIncomingHeaders.Add(new MessageHeader(httpHeader.Name, httpHeader.HeaderValue));
            }
            else
            {
                if (MessageBody == null)
                    throw new ArgumentNullException("MessageBody", "MessageBody was not set!");

                // process message headers embedded in message body
                JArray messageHeaders = MessageBody[RPC_MSG_HEADERS] as JArray;
                if (messageHeaders != null)
                {
                    ctxIncomingHeaders = new MessageHeaders();

                    int childCount = messageHeaders.Count;
                    for (int i = 0; i < childCount; i++)
                    {
                        JObject jsonHeader = messageHeaders[i] as JObject;
                        foreach (JProperty prop in jsonHeader.Properties())
                        {
                            string key = prop.Name;
                            string value = prop.Value.Value<string>();
                            ctxIncomingHeaders.Add(new MessageHeader(key, value));
                        }
                    }

                    // remove headers from message body after processing
                    MessageBody.Remove(RPC_MSG_HEADERS);
                }
            }
        }

        /// <summary>
        /// Helper to generate a string dictionary of the outgoing message headers.
        /// </summary>
        /// <param name="headers"></param>
        /// <returns></returns>
        public static Dictionary<string, string> BuildOutgoingHeaders(MessageHeaders headers)
        {
            Dictionary<string, string> ret = new Dictionary<string, string>();
            foreach (MessageHeader header in headers)
                ret.Add(header.Name, header.HeaderValue);

            return ret;
        }

        /// <summary>
        /// Helper to serialize the message body to the JSON text string representation of the <see cref="RPCMessage"/>.
        /// </summary>
        /// <param name="outgoingHeaders"></param>
        /// <returns></returns>
        public string Serialize(MessageHeaders outgoingHeaders)
        {
            if (MessageBody == null)
                throw new ArgumentNullException("MessageBody", "MessageBody was not set!");

            // append headers to the message body
            if (outgoingHeaders != null)
            {
                if (outgoingHeaders.Count > 0)
                {
                    Dictionary<string, string> headers = BuildOutgoingHeaders(outgoingHeaders);
                    JArray bodyHeaders = new JArray();

                    // iterate through headers and add
                    foreach (KeyValuePair<string, string> header in headers)
                    {
                        JObject jsonHeader = new JObject();
                        jsonHeader.Add(header.Key, header.Value);

                        bodyHeaders.Add(jsonHeader);
                    }

                    MessageBody.Add(new JProperty(RPC_MSG_HEADERS, bodyHeaders));
                }
            }

            return MessageBody.ToString();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            IHttpContext httpContext = ctxIncomingProperties["HttpContext"] as IHttpContext;
            return "{" + MessageType + ", " + Direction + ", " + RequestUri.ToString() + (((MessageType == RPCMessageType.Json_REST) && httpContext != null) ? ", " + httpContext.Request.Method.ToUpper() : "") + "}";
        }
    } // public sealed class RPCMessage
} // namespace TridentFramework.RPC
