/**
 * Copyright (c) 2008-2020 Bryan Biedenkapp., All Rights Reserved.
 * MIT Open Source. Use is subject to license terms.
 * DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
 */

using System;

namespace TridentFramework.RPC
{
    /// <summary>
    /// This class implements our custom message inspector that is used for handling CORS.
    /// </summary>
    public class EnableCrossOriginResourceMessageInspector : IServiceMessageInspector
    {
        /*
        ** Methods
        */

        /// <inheritdoc />
        public object AfterRecieveRequest(RPCMessage message)
        {
            return null;
        }

        /// <inheritdoc />
        public void BeforeSendReply(RPCContext context, ref RPCMessage message)
        {
            // build ACL origin header
            string origin = "*";
            if (message.IncomingMessageHeaders["Origin"] != null)
                origin = message.IncomingMessageHeaders["Origin"].HeaderValue;
            if (origin == null)
                origin = "*";
            if (origin == string.Empty)
                origin = "*";

            if (RPCContext.Current.OutgoingMessageHeaders["Access-Control-Allow-Origin"] == null)
                RPCContext.Current.OutgoingMessageHeaders.Add("Access-Control-Allow-Origin", origin);

            // build remaining CORS headers
            if (RPCContext.Current.OutgoingMessageHeaders["Access-Control-Allow-Methods"] == null)
                RPCContext.Current.OutgoingMessageHeaders.Add("Access-Control-Allow-Methods", "GET,PUT,POST,DELETE,HEAD,OPTIONS");

            if (RPCContext.Current.OutgoingMessageHeaders["Access-Control-Allow-Headers"] == null)
                RPCContext.Current.OutgoingMessageHeaders.Add("Access-Control-Allow-Headers", "*");
        }
    } // public class EnableCrossOriginResourceMessageInspector : IServiceMessageInspector
} // namespace TridentFramework.RPC
