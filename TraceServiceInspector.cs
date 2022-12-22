/*
 * Copyright (c) 2008-2020 Bryan Biedenkapp., All Rights Reserved.
 * MIT Open Source. Use is subject to license terms.
 * DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
 */

using System;

using TridentFramework.RPC.Utility;

namespace TridentFramework.RPC
{
    /// <summary>
    ///
    /// </summary>
    public class TraceMessageInspector : IServiceMessageInspector
    {
        /*
        ** Methods
        */

        /// <inheritdoc />
        public object AfterRecieveRequest(RPCMessage message)
        {
            if (message != null)
            {
                string msgHeaders = string.Empty;
                foreach (MessageHeader header in message.IncomingMessageHeaders)
                    msgHeaders += string.Format("[{0}: {1}]", header.Name, header.HeaderValue) + ",";
                msgHeaders = msgHeaders.TrimEnd(new char[] { ',' });

                RPCLogger.Trace(message.ToString() + ((msgHeaders != string.Empty) ? " headers: {" + msgHeaders + "}" : string.Empty));
                if (message.MessageBody != null)
                    RPCLogger.Trace(string.Format("[TRACE] RPC Incoming:\n{0}", message.MessageBody.ToString()));
            }
            return null;
        }

        /// <inheritdoc />
        public void BeforeSendReply(RPCContext context, ref RPCMessage message)
        {
            if (message != null)
            {
                string msgHeaders = string.Empty;
                foreach (MessageHeader header in context.OutgoingMessageHeaders)
                    msgHeaders += string.Format("[{0}: {1}]", header.Name, header.HeaderValue) + ",";
                msgHeaders = msgHeaders.TrimEnd(new char[] { ',' });

                RPCLogger.Trace(message.ToString() + ((msgHeaders != string.Empty) ? " headers: {" + msgHeaders + "}" : string.Empty));
                if (message.MessageBody != null)
                    RPCLogger.Trace(string.Format("[TRACE] RPC Outgoing:\n{0}", message.MessageBody.ToString()));
            }
        }
    } // public class TraceMessageInspector : IServiceMessageInspector
} // namespace TridentFramework.RPC
