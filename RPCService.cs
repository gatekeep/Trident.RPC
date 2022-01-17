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

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

using TridentFramework.RPC.Net;
using TridentFramework.RPC.Net.Encryption;
using TridentFramework.RPC.Net.Message;
using TridentFramework.RPC.Net.PeerConnection;
using TridentFramework.RPC.Remoting;

using TridentFramework.RPC.Utility;

namespace TridentFramework.RPC
{
    /// <summary>
    /// Defines an RPC "service" or server that services RPC requests.
    /// </summary>
    public class RPCService : INetServer
    {
        public const string PACKET_HASH = "a61a5b66-9f34-4438-8773-60f9f16b594d";
        public const ushort RPC_DATA_LEADER = 0x0F01;

        private Uri serviceUri;
        private RPCProxyHelper proxyHelper;

        [ThreadStatic] private static RPCService ctxService;
        private RPCContext context = new RPCContext();

        private Dictionary<string, Type> serviceEndpoints = new Dictionary<string, Type>(16);
        private Dictionary<Type, Type> serviceTypes = new Dictionary<Type, Type>(16);

        internal static long requestCounter = 0;
        internal Dictionary<long, ManualResetEvent> requestMREQueue = new Dictionary<long, ManualResetEvent>(16);
        internal Dictionary<long, RPCMessage> requestResponseQueue = new Dictionary<long, RPCMessage>(16);

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
        /// Gets or sets the instance of the <see cref="IAuthenticationGuard"/> for this RPC service.
        /// </summary>
        public IAuthenticationGuard AuthenticationGuard
        {
            get { return proxyHelper.AuthenticationGuard; }
            set { proxyHelper.AuthenticationGuard = value; }
        }

        /// <summary>
        /// Gets the list of service endpoints for this <see cref="RPCService"/>.
        /// </summary>
        public Dictionary<string, Type> ServiceEndpoints
        {
            get { return serviceEndpoints; }
        }

        /// <summary>
        /// Gets the list of service types for this <see cref="RPCService"/>.
        /// </summary>
        public Dictionary<Type, Type> ServiceTypes
        {
            get { return serviceTypes; }
        }

        /// <summary>
        /// Gets the current context of this service.
        /// </summary>
        public static RPCService Context
        {
            get { return ctxService; }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="RPCService"/> class.
        /// </summary>
        /// <param name="serviceUri"></param>
        public RPCService(Uri serviceUri)
            : this(serviceUri, new PeerConfiguration(PACKET_HASH, serviceUri.Port)
            {
                AcceptIncomingConnections = true,
                LocalAddress = NetUtility.Resolve(serviceUri.Host),
                EnableEncryption = false,
                EncryptionProvider = new AESEncryption(),
                EncryptionKey = PACKET_HASH
            })
        {
            /* stub */
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RPCService"/> class.
        /// </summary>
        /// <param name="serviceUri"></param>
        /// <param name="encryptionKey"></param>
        public RPCService(Uri serviceUri, string encryptionKey)
            : this(serviceUri, new PeerConfiguration(PACKET_HASH, serviceUri.Port)
            {
                AcceptIncomingConnections = true,
                LocalAddress = NetUtility.Resolve(serviceUri.Host),
                EnableEncryption = true,
                EncryptionProvider = new AESEncryption(),
                EncryptionKey = encryptionKey
            })
        {
            /* stub */
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RPCService"/> class.
        /// </summary>
        /// <param name="serviceUri"></param>
        /// <param name="peerConfiguration"></param>
        protected RPCService(Uri serviceUri, PeerConfiguration peerConfiguration) : base(peerConfiguration)
        {
            this.serviceUri = serviceUri;

            this.context.channelMessageInspectors = null;

            this.proxyHelper = new RPCProxyHelper();

            // hook self events
            this.OnUserDefinedNetworkData += RPCService_OnUserDefinedNetworkData;
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
                throw new InvalidOperationException("RPC Uri endpoint " + uri + " is already defined!");
            if (serviceTypes.ContainsKey(interfaceType))
                throw new InvalidOperationException("RPC interface type " + interfaceType.ToString() + " is already defined as a RPC!");

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
        /// Creates a channel of a specified type to a specified endpoint address.
        /// </summary>
        /// <remarks><typeparamref name="TObject"/> must be an interface.</remarks>
        /// <returns>The channel proxy for the current proxy instance.</returns>
        public static TObject CreateCallbackChannel<TObject>() where TObject : class
        {
            RPCCallbackChannel callbackChannel = new RPCCallbackChannel(ctxService, RPCContext.Current);
            ChannelProxy<TObject> proxy = new ChannelProxy<TObject>(typeof(TObject), callbackChannel);
            return proxy.Create();
        }

        /// <summary>
        /// Internal helper to send a JSON message body to a particular connection.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="soapEnvelope"></param>
        private void SendMessageBodyTo(RPCMessage message)
        {
            if (message.MessageType != RPCMessageType.Json_RPC)
                throw new ArgumentException("message");

            // perform message inspector behaviors before sending response
            foreach (IServiceMessageInspector inspector in context.serviceMessageInspectors)
                inspector.BeforeSendReply(context, ref message);

            // generate network message
            UTF8Encoding utf8 = new UTF8Encoding();
            byte[] messageBytes = utf8.GetBytes(message.Serialize(RPCContext.ctxOutgoingHeaders));
            uint crc = CRC.CalculateDigest(messageBytes, 0, messageBytes.Length);

            OutgoingMessage outMsg = PrepareMessageTo(message.ConnectionId);
            outMsg.Write(RPCService.RPC_DATA_LEADER);
            outMsg.Write(message.RequestId);
            outMsg.Write(message.RequestUri.AbsoluteUri);
            outMsg.Write((byte)message.Direction);
            outMsg.Write(crc);
            outMsg.Write(messageBytes, true);

            SendMessageTo(message.ConnectionId, outMsg);
        }

        /// <summary>
        /// Internal helper to send an RPC result back to the caller.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="methodInfo"></param>
        /// <param name="ex"></param>
        /// <returns></returns>
        private bool SendFaultResult(RPCMessage message, MethodInfo methodInfo, Exception ex)
        {
            bool handled = false;
            JObject fault = null;

            if (message.MessageType != RPCMessageType.Json_RPC)
                throw new ArgumentException("message");

            // iterate through the exception handlers
            foreach (IRPCExceptionHandler handler in context.exceptionHandlers)
            {
                if (!handler.HandleError(ex))
                {
                    handler.ProvideFault(ex, ref fault);
                    if (fault == null)
                    {
                        fault = proxyHelper.PrepareRPCFaultResponse(message, methodInfo, ex);
                        break;
                    }
                }
                else
                    handled = true;
            }

            // if we have no custom handlers...
            if (context.exceptionHandlers.Count == 0)
                fault = proxyHelper.PrepareRPCFaultResponse(message, methodInfo, ex);

            if (!handled)
            {
                if (fault == null)
                    fault = new JObject();

                // generate and send RPC message
                RPCMessage response = new RPCMessage(RPCMessageType.Json_RPC, MessageDirection.Response, message.BaseUri, message.RequestUri, fault)
                {
                    RequestId = message.RequestId,
                    ConnectionId = connectionId,
                };
                SendMessageBodyTo(response);
                return false;
            }
            else
                return true;
        }

        /// <summary>
        /// Internal helper to handle a RPC request.
        /// </summary>
        /// <param name="message"></param>
        private void ProcessRPCRequest(RPCMessage message)
        {
            if (message.MessageType != RPCMessageType.Json_RPC)
                throw new ArgumentException("message");

            // process any execution faults
            JProperty faultProperty = null;
            Exception ex = null;
            if (proxyHelper.ProcessFaultResponse(message.MessageBody, out faultProperty, true, out ex))
            {
                RPCLogger.WriteWarning("unexpected RPC fault recieved from the client channel! bug?");

                // just send this fault back to the client (we don't process them)
                bool handled = SendFaultResult(message, null, new RPCException(string.Format("RPC exception has occurred. {0} {1}",
                    faultProperty["message"].Value<string>(), faultProperty["actor"].Value<string>()), ex));
                if (!handled)
                    return;
            }

            Type interfaceType = null, serviceType = null;
            if (serviceEndpoints.ContainsKey(message.RequestUri.AbsolutePath))
                if (!serviceEndpoints.TryGetValue(message.RequestUri.AbsolutePath, out interfaceType))
                    throw new InvalidOperationException("Service Uri endpoint invalid. No service endpoint defined for specified Uri: " + message.RequestUri);
            if (interfaceType == null)
                throw new InvalidOperationException("Service Uri endpoint invalid. No service interface for specified Uri: " + message.RequestUri);

            if (serviceTypes.ContainsKey(interfaceType))
                if (!serviceTypes.TryGetValue(interfaceType, out serviceType))
                    throw new InvalidOperationException("serviceTypes");
            if (serviceType == null)
                throw new InvalidOperationException("serviceType");

            requestResponseQueue.Clear();
            requestMREQueue.Clear();

            // process message headers
            message.ReadIncomingHeaders();

            // set context data
            {
                context.Reset();
                RPCContext.ctxOutgoingHeaders = new MessageHeaders();
                RPCContext.ctxMessage = message;
                RPCContext.ctxCurrent = context;
                ctxService = this;
            }

            // perform message inspector behaviors after receiving request
            foreach (IServiceMessageInspector inspector in context.serviceMessageInspectors)
                inspector.AfterRecieveRequest(message);

            JObject messageBody = proxyHelper.ProcessRPCRequest(context, interfaceType, serviceType, null, SendFaultResult);
            if (messageBody != null)
            {
                // generate and send RPC message
                RPCMessage response = new RPCMessage(RPCMessageType.Json_RPC, MessageDirection.Response, context.BaseUri, context.RequestUri, messageBody)
                {
                    RequestId = message.RequestId,
                    ConnectionId = connectionId,
                };
                SendMessageBodyTo(response);
            }
        }

        /**
         * Event Handlers
         */

        /// <summary>
        /// Occurs when user defined network data is recieved.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="id"></param>
        /// <param name="msg"></param>
        private void RPCService_OnUserDefinedNetworkData(object sender, long id, IncomingMessage msg)
        {
            ushort signature = msg.ReadUInt16();

            // RPC data
            if (signature == RPC_DATA_LEADER)
            {
                long requestId = msg.ReadInt64();
                Uri uriPath = new Uri(msg.ReadString());
                MessageDirection messageDirection = (MessageDirection)msg.ReadByte();
                UTF8Encoding utf8 = new UTF8Encoding();
                uint crc = msg.ReadUInt32();
                byte[] messageBytes = msg.ReadBytes();
                if (messageBytes == null)
                    return;

                if (!CRC.VerifyDigest(crc, messageBytes, 0, messageBytes.Length))
                {
                    RPCLogger.WriteWarning(string.Format("crc {0} != {1}, CRC mismatch! data may be corrupt", crc, CRC.CalculateDigest(messageBytes, 0, messageBytes.Length)));
                    return;
                }

                JObject messageBody = JObject.Parse(utf8.GetString(messageBytes));

                // check returned message body
                if (messageBody == null)
                {
                    RPCLogger.WriteError("malformed RPC response received");
                    return;
                }

                // generate RPC message
                RPCMessage message = new RPCMessage(RPCMessageType.Json_RPC, messageDirection, uriPath, uriPath, messageBody)
                {
                    RequestId = requestId,
                    ConnectionId = connectionId,
                };

                // set message incoming properties
                {
                    message.IncomingMessageProperties.Add("Via", uriPath);
                    message.IncomingMessageProperties.Add(RPCProxyHelper.RPC_MSG_PROP_REQ_ID, requestId);
                    message.IncomingMessageProperties.Add(RPCProxyHelper.RPC_MSG_PROP_CONN_ID, connectionId);
                }

                // is this a callback?
                if (message.Direction == MessageDirection.Request)
                {
                    // process the RPC request as a task
                    Task.Run(() =>
                    {
                        // handle actual RPC request
                        try
                        {
                            ProcessRPCRequest(message);
                        }
                        catch (Exception ex)
                        {
                            SendFaultResult(message, null, ex);
                        }
                    });
                }
                else if (message.Direction == MessageDirection.Response)
                {
                    // queue RPC message request response
                    requestResponseQueue.Add(requestId, new RPCMessage(RPCMessageType.Json_RPC, messageDirection, uriPath, uriPath, messageBody)
                    {
                        RequestId = requestId,
                        ConnectionId = connectionId,
                    });

                    try
                    {
                        requestMREQueue[requestId].Set();
                    }
                    catch (KeyNotFoundException)
                    {
                        RPCLogger.WriteWarning("response timed out -- MRE key was removed");
                    }
                }
                else
                    RPCLogger.WriteError("invalid message direction (" + (byte)message.Direction + ")");
            }
        }
    } // public class RPCService
} // namespace TridentFramework.RPC
