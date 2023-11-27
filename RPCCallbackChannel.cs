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
using System.Reflection;
using System.Text;
using System.Threading;

using Newtonsoft.Json.Linq;

using TridentFramework.RPC.Net;
using TridentFramework.RPC.Net.Message;
using TridentFramework.RPC.Remoting;

using TridentFramework.RPC.Utility;

namespace TridentFramework.RPC
{
    /// <summary>
    /// Defines an RPC callback "channel" or connection to a RPC client.
    /// </summary>
    public class RPCCallbackChannel : IRPCProxySend
    {
        [ThreadStatic] private static int requestRetryCount = 0;

        private RPCContext context;

        private RPCProxyHelper proxyHelper;

        private long connectionId;
        private RPCService service;

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="RPCCallbackChannel"/> class.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="context"></param>
        public RPCCallbackChannel(RPCService service, RPCContext context)
        {
            this.context = context;

            this.connectionId = context.Message.ConnectionId;
            this.service = service;

            this.proxyHelper = new RPCProxyHelper();
        }

        /// <summary>
        /// Internal helper to send a JSON message body to a particular connection.
        /// </summary>
        /// <param name="message"></param>
        private ManualResetEvent SendMessageBody(RPCMessage message)
        {
            // generate network message
            UTF8Encoding utf8 = new UTF8Encoding();
            byte[] messageBytes = utf8.GetBytes(message.Serialize(context.OutgoingMessageHeaders));
            uint crc = CRC.CalculateDigest(messageBytes, 0, messageBytes.Length);

            ManualResetEvent mre = new ManualResetEvent(false);
            service.requestMREQueue.Add(message.RequestId, mre);

            OutgoingMessage outMsg = service.PrepareMessage();
            outMsg.Write(RPCService.RPC_DATA_LEADER);
            outMsg.Write(message.RequestId);
            outMsg.Write(context.Message.RequestUri.AbsoluteUri);
            outMsg.Write((byte)message.Direction);
            outMsg.Write(crc);
            outMsg.Write(messageBytes, true);

            Interlocked.Increment(ref RPCService.requestCounter);
            service.SendMessageTo(connectionId, outMsg, false);
            return mre;
        }

        /// <summary>
        /// Internal helper to send an RPC result back to the service.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="methodInfo"></param>
        /// <param name="ex"></param>
        /// <returns></returns>
        private void SendFaultResult(RPCMessage message, MethodInfo methodInfo, Exception ex)
        {
            // generate and send RPC message
            RPCMessage response = new RPCMessage(RPCMessageType.Json_RPC, MessageDirection.Response, message.BaseUri, message.RequestUri,
                proxyHelper.PrepareRPCFaultResponse(message, methodInfo, ex))
            {
                RequestId = message.RequestId,
                ConnectionId = connectionId,
            };
            SendMessageBody(response);
        }

        /// <inheritdoc />
        public object Send(MethodInfo targetMethod, MethodMapper mapper, object[] ins, object[] outs)
        {
            if (ins.Length != mapper.InArgs.Length)
                throw new InvalidOperationException();
            if (outs.Length != mapper.OutArgs.Length)
                throw new InvalidOperationException();

            // generate JSON message body
            JObject json = proxyHelper.PrepareRPCRequest(targetMethod, mapper, ins, outs);

            // generate and send RPC message
            RPCMessage message = new RPCMessage(RPCMessageType.Json_RPC, MessageDirection.Request, context.Message.RequestUri, context.Message.RequestUri,
                json)
            {
                RequestId = RPCService.requestCounter,
                ConnectionId = connectionId,
            };
            ManualResetEvent mre = SendMessageBody(message);

            // block and wait for response
            bool success = mre.WaitOne(RPCChannel.REQUEST_TIMEOUT);
            if (!success && requestRetryCount <= RPCChannel.REQUEST_TRIES)
            {
                requestRetryCount++;
                return Send(targetMethod, mapper, ins, outs);
            }

            requestRetryCount = 0;
            service.requestMREQueue.Remove(message.RequestId);

            if (service.requestResponseQueue.ContainsKey(message.RequestId))
            {
                RPCMessage response;
                if (!service.requestResponseQueue.TryGetValue(message.RequestId, out response))
                    return null;
                service.requestResponseQueue.Remove(message.RequestId);

                return ProcessRPCResponse(response, targetMethod, outs);
            }
            else
                return null;
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

            // set context data
            {
                context.Reset();
                RPCContext.ctxMessage = message;
                RPCContext.ctxCurrent = context;
            }

            // process rpc response
            ret = proxyHelper.ProcessRPCResponse(context, targetMethod, outs);
            return ret;
        }
    } // public class RPCCallbackChannel : INetClient
} // namespace TridentFramework.RPC
