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
    /// Defines an RPC "channel" or client connection to a RPC service.
    /// </summary>
    public class RPCChannel : INetClient, IRPCProxySend
    {
        public const int REQUEST_TIMEOUT = 5000; //2500;
        public const int REQUEST_TRIES = 0;

        [ThreadStatic] private static int requestRetryCount = 0;
        private RPCContext context = new RPCContext();

        /// <summary>
        /// Flag used to disable the request retry logic.
        /// </summary>
        public static bool DisableRetry = false;

        private Uri remoteUri;

        private RPCProxyHelper proxyHelper;

        private Type callbackInterfaceType;
        private Type callbackType;
        private object callbackObject;

        internal static long requestCounter = 0;
        private Dictionary<long, ManualResetEvent> requestMREQueue = new Dictionary<long, ManualResetEvent>(16);
        private Dictionary<long, RPCMessage> requestResponseQueue = new Dictionary<long, RPCMessage>(16);

        /*
        ** Properties
        */

        /// <summary>
        /// Gets the list of message inspectors for this <see cref="RPCChannel"/>.
        /// </summary>
        public List<IChannelMessageInspector> MessageInspectors
        {
            get { return context.channelMessageInspectors; }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="RPCChannel"/> class.
        /// </summary>
        /// <param name="remoteUri"></param>
        public RPCChannel(Uri remoteUri)
            : this(remoteUri, new PeerConfiguration(RPCService.PACKET_HASH, 0)
            {
                AcceptIncomingConnections = false,
                EnableEncryption = false,
                EncryptionProvider = new AESEncryption(),
                EncryptionKey = RPCService.PACKET_HASH
            })
        {
            /* stub */
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RPCChannel"/> class.
        /// </summary>
        /// <param name="remoteUri"></param>
        /// <param name="encryptionKey"></param>
        public RPCChannel(Uri remoteUri, string encryptionKey)
            : this(remoteUri, new PeerConfiguration(RPCService.PACKET_HASH, 0)
            {
                AcceptIncomingConnections = false,
                EnableEncryption = true,
                EncryptionProvider = new AESEncryption(),
                EncryptionKey = encryptionKey
            })
        {
            /* stub */
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RPCChannel"/> class.
        /// </summary>
        /// <param name="remoteUri"></param>
        /// <param name="peerConfiguration"></param>
        protected RPCChannel(Uri remoteUri, PeerConfiguration peerConfiguration) : base(remoteUri, peerConfiguration)
        {
            this.remoteUri = remoteUri;

            this.proxyHelper = new RPCProxyHelper();

            // hook self events
            this.OnUserDefinedNetworkData += RPCChannel_OnUserDefinedNetworkData;
        }

        /// <summary>
        /// Creates a channel of a specified type to a specified endpoint address.
        /// </summary>
        /// <remarks><typeparamref name="TObject"/> must be an interface.</remarks>
        /// <returns>The channel proxy for the current proxy instance.</returns>
        public TObject CreateChannel<TObject>() where TObject : class
        {
            // make sure networking is open and connected
            if (!IsOpened && !IsConnected)
                Open(typeof(TObject).FullName);
            if (!IsOpened)
                return default(TObject);
            if (IsOpened && !IsConnected)
                Connect();
            if (!IsConnected)
                return default(TObject);

            ChannelProxy<TObject> proxy = new ChannelProxy<TObject>(typeof(TObject), this);
            return proxy.Create();
        }

        /// <summary>
        /// Creates a channel of a specified type to a specified endpoint address with the given
        /// callback object.
        /// </summary>
        /// <typeparam name="TObject"></typeparam>
        /// <param name="callbackObject"></param>
        /// <remarks><typeparamref name="TObject"/> must be an interface.</remarks>
        /// <returns>The channel proxy for the current proxy instance.</returns>
        public TObject CreateDuplexChannel<TObject>(object callbackObject) where TObject : class
        {
            bool foundInterface = false;

            // make sure networking is open and connected
            if (!IsOpened && !IsConnected)
                Open(typeof(TObject).FullName);
            if (IsOpened && !IsConnected)
                Connect();

            if (callbackObject == null)
                throw new ArgumentNullException("callbackObject");
            this.callbackObject = callbackObject;

            Type t = typeof(TObject);
            if (t.GetCustomAttribute(typeof(RPCContractAttribute)) == null)
                throw new ArgumentException("TObject must be an interface with the RPCContract attribute");
            RPCContractAttribute rpcContract = t.GetCustomAttribute<RPCContractAttribute>();
            callbackInterfaceType = rpcContract.CallbackContract;

            callbackType = callbackObject.GetType();
            Type[] ctInterfaces = callbackType.GetInterfaces();
            foreach (Type iface in ctInterfaces)
                if (iface == callbackInterfaceType)
                {
                    foundInterface = true;
                    break;
                }

            if (!foundInterface)
                throw new ArgumentException("Specified callback object does not implement " + callbackInterfaceType.Name);

            ChannelProxy<TObject> proxy = new ChannelProxy<TObject>(typeof(TObject), this);
            return proxy.Create();
        }

        /// <summary>
        /// Internal helper to send a JSON message body to a particular connection.
        /// </summary>
        /// <param name="message"></param>
        private ManualResetEvent SendMessageBody(RPCMessage message)
        {
            if (message.MessageType != RPCMessageType.Json_RPC)
                throw new ArgumentException("message");

            // perform message inspector behaviors before sending request
            foreach (IChannelMessageInspector inspector in context.channelMessageInspectors)
                inspector.BeforeSendRequest(context, ref message, this);

            // generate network message
            UTF8Encoding utf8 = new UTF8Encoding();
            byte[] messageBytes = utf8.GetBytes(message.Serialize(RPCContext.ctxOutgoingHeaders));
            uint crc = CRC.CalculateDigest(messageBytes, 0, messageBytes.Length);

            ManualResetEvent mre = new ManualResetEvent(false);
            requestMREQueue.Add(message.RequestId, mre);

            OutgoingMessage outMsg = PrepareMessage();
            outMsg.Write(RPCService.RPC_DATA_LEADER);
            outMsg.Write(message.RequestId);
            outMsg.Write(remoteUri.AbsoluteUri);
            outMsg.Write((byte)message.Direction);
            outMsg.Write(crc);
            outMsg.Write(messageBytes, true);

            Interlocked.Increment(ref requestCounter);
            SendMessage(outMsg);
            return mre;
        }

        /// <summary>
        /// Internal helper to send an RPC result back to the service.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="methodInfo"></param>
        /// <param name="ex"></param>
        /// <returns></returns>
        private bool SendFaultResult(RPCMessage message, MethodInfo methodInfo, Exception ex)
        {
            // generate and send RPC message
            RPCMessage response = new RPCMessage(RPCMessageType.Json_RPC, MessageDirection.Response, message.BaseUri, message.RequestUri,
                proxyHelper.PrepareRPCFaultResponse(message, methodInfo, ex))
            {
                RequestId = message.RequestId,
                ConnectionId = connectionId,
            };
            SendMessageBody(response);
            return false; // ?
        }

        /// <inheritdoc />
        public object Send(MethodInfo targetMethod, MethodMapper mapper, object[] ins, object[] outs)
        {
            if (IsOpened && !IsConnected)
                Connect();

            // set context data
            {
                context.Reset();
                RPCContext.ctxOutgoingHeaders = new MessageHeaders();
                RPCContext.ctxCurrent = context;
            }

            JObject messageBody = proxyHelper.PrepareRPCRequest(targetMethod, mapper, ins, outs);

            // generate and send RPC message
            RPCMessage message = new RPCMessage(RPCMessageType.Json_RPC, MessageDirection.Request, remoteUri, remoteUri, messageBody)
            {
                RequestId = requestCounter,
                ConnectionId = connectionId,
            };
            ManualResetEvent mre = SendMessageBody(message);

            // block and wait for response
            bool success = mre.WaitOne(REQUEST_TIMEOUT);
            if (!DisableRetry)
            {
                if (!success && requestRetryCount <= REQUEST_TRIES)
                {
                    requestRetryCount++;
                    RPCLogger.Trace("RPC request timeout, retries " + requestRetryCount);
                    return Send(targetMethod, mapper, ins, outs);
                }
            }

            requestRetryCount = 0;
            requestMREQueue.Remove(message.RequestId);

            if (requestResponseQueue.ContainsKey(message.RequestId))
            {
                RPCMessage response;
                if (!requestResponseQueue.TryGetValue(message.RequestId, out response))
                    return null;
                requestResponseQueue.Remove(message.RequestId);

                return ProcessRPCResponse(response, targetMethod, outs);
            }
            else
                return null;
        }

        /// <summary>
        /// Internal helper to handle a RPC request.
        /// </summary>
        /// <param name="message"></param>
        private void ProcessRPCRequest(RPCMessage message)
        {
            // do we have a callback object?
            if (callbackObject == null)
            {
                RPCException rpcEx = new RPCException("No callback object specified, callbackObject == null");
                SendFaultResult(message, null, rpcEx);
                throw rpcEx;
            }

            // do we have a callback type?
            if (callbackInterfaceType == null)
            {
                RPCException rpcEx = new RPCException("No callback interface specified, callbackInterfaceType == null");
                SendFaultResult(message, null, rpcEx);
                throw rpcEx;
            }

            // process any execution faults
            proxyHelper.ProcessFaultResponse(message.MessageBody, out _, true, out _);

            // process message headers
            message.ReadIncomingHeaders();

            // set context data
            {
                context.Reset();
                RPCContext.ctxOutgoingHeaders = new MessageHeaders();
                RPCContext.ctxMessage = message;
                RPCContext.ctxCurrent = context;
            }

            JObject messageBody = proxyHelper.ProcessRPCRequest(context, callbackInterfaceType, callbackType, callbackObject, SendFaultResult);
            if (messageBody != null)
            {
                // generate and send RPC message
                RPCMessage response = new RPCMessage(RPCMessageType.Json_RPC, MessageDirection.Response, context.BaseUri, context.RequestUri, messageBody)
                {
                    RequestId = message.RequestId,
                    ConnectionId = connectionId,
                };
                SendMessageBody(response);
            }
        }

        /// <summary>
        /// Internal helper to handle a RPC request response.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="targetMethod"></param>
        /// <param name="outs"></param>
        private object ProcessRPCResponse(RPCMessage message, MethodInfo targetMethod, object[] outs)
        {
            object ret = null;

            // process message headers
            message.ReadIncomingHeaders();

            // set context data
            {
                context.Reset();
                RPCContext.ctxMessage = message;
                RPCContext.ctxCurrent = context;
            }

            // perform message inspector behaviors after receiving response
            foreach (IChannelMessageInspector inspector in context.channelMessageInspectors)
                inspector.AfterRecieveReply(message);

            // process rpc response
            ret = proxyHelper.ProcessRPCResponse(context, targetMethod, outs);
            return ret;
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
        private void RPCChannel_OnUserDefinedNetworkData(object sender, long id, IncomingMessage msg)
        {
            ushort signature = msg.ReadUInt16();

            // RPC data
            if (signature == RPCService.RPC_DATA_LEADER)
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

                // is this request destined for us?
                if (uriPath != remoteUri)
                    return;

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

                if (message.Direction == MessageDirection.Response)
                {
                    if (requestResponseQueue.ContainsKey(requestId))
                    {
                        RPCLogger.WriteError(string.Format("{0} already exists in request response queue! dropping message!", requestId));
                        ++requestCounter;
                    }
                    else
                    {
                        // queue request response
                        requestResponseQueue.Add(requestId, message);

                        try
                        {
                            requestMREQueue[requestId].Set();
                        }
                        catch (KeyNotFoundException)
                        {
                            RPCLogger.WriteWarning("response timed out -- MRE key was removed");
                        }
                    }
                }
                else if (message.Direction == MessageDirection.Request)
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
                else
                    RPCLogger.WriteError("invalid message direction (" + (byte)message.Direction + ")");
            }
        }
    } // public class RPCChannel : INetClient
} // namespace TridentFramework.RPC
