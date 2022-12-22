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

using TridentFramework.RPC.Utility;

namespace TridentFramework.RPC
{
    /// <summary>
    /// Provides access to the execution context of a service method.
    /// </summary>
    public class RPCContext
    {
        [ThreadStatic] internal static RPCContext ctxCurrent;
        [ThreadStatic] internal static RPCMessage ctxMessage = null;

        [ThreadStatic] internal static MessageHeaders ctxOutgoingHeaders = null;
        [ThreadStatic] internal static MessageProperties ctxOutgoingProperties = null;

        [ThreadStatic] internal static Type ctxIntfType = null;
        [ThreadStatic] internal static Type ctxSvcType = null;

        [ThreadStatic] internal static bool ctxUseMessageResponse = false;

        internal List<IServiceMessageInspector> serviceMessageInspectors = new List<IServiceMessageInspector>();
        internal List<IChannelMessageInspector> channelMessageInspectors = new List<IChannelMessageInspector>();
        internal List<IRPCExceptionHandler> exceptionHandlers = new List<IRPCExceptionHandler>();

        /*
        ** Properties
        */

        /// <summary>
        /// Gets or sets the execution context for the current thread.
        /// </summary>
        public static RPCContext Current
        {
            get { return ctxCurrent; }
            set { ctxCurrent = value; }
        }

        /// <summary>
        /// Flag indicating the context message should be used as the response message.
        /// </summary>
        public bool UseMessageAsResponse
        {
            get { return ctxUseMessageResponse; }
            set { ctxUseMessageResponse = value; }
        }

        /// <summary>
        /// Gets the incoming/outgoing message.
        /// </summary>
        public RPCMessage Message
        {
            get { return ctxMessage; }
        }

        /// <summary>
        /// Gets the incoming message headers.
        /// </summary>
        public MessageHeaders IncomingMessageHeaders
        {
            get { return ctxMessage != null ? ctxMessage.IncomingMessageHeaders : null; }
        }

        /// <summary>
        /// Gets the incoming message properties.
        /// </summary>
        public MessageProperties IncomingMessageProperties
        {
            get { return ctxMessage != null ? ctxMessage.IncomingMessageProperties : null; }
        }

        /// <summary>
        /// Gets the message base URI.
        /// </summary>
        public Uri BaseUri
        {
            get { return ctxMessage != null ? ctxMessage.BaseUri : null; }
        }

        /// <summary>
        /// Gets the message RPC request URI.
        /// </summary>
        public Uri RequestUri
        {
            get { return ctxMessage != null ? ctxMessage.RequestUri : null; }
        }

        /// <summary>
        /// Gets the outgoing message headers.
        /// </summary>
        public MessageHeaders OutgoingMessageHeaders
        {
            get
            {
                if (ctxOutgoingHeaders == null)
                {
                    RPCLogger.WriteWarning("BUGBUG: Outgoing headers were not available! This should not happen.");
                    return new MessageHeaders();
                }
                return ctxOutgoingHeaders;
            }
        }

        /// <summary>
        /// Gets the incoming message properties.
        /// </summary>
        public MessageProperties OutgoingMessageProperties
        {
            get
            {
                if (ctxOutgoingProperties == null)
                {
                    RPCLogger.WriteWarning("BUGBUG: Incoming message properties were not available! This should not happen.");
                    return new MessageProperties();
                }
                return ctxOutgoingProperties;
            }
        }

        /// <summary>
        /// Gets the list of message inspectors for this <see cref="RPCContext"/>.
        /// </summary>
        public List<IServiceMessageInspector> MessageInspectors
        {
            get { return serviceMessageInspectors; }
        }

        /// <summary>
        /// Gets the list of channel inspectors for this <see cref="RPCContext"/>.
        /// </summary>
        public List<IChannelMessageInspector> ChannelInspectors
        {
            get { return channelMessageInspectors; }
        }

        /// <summary>
        /// Gets the list of exception handlers for this <see cref="RPCContext"/>.
        /// </summary>
        public List<IRPCExceptionHandler> ExceptionHandlers
        {
            get { return exceptionHandlers; }
        }

        /// <summary>
        /// Gets the type of the service.
        /// </summary>
        public Type ServiceType
        {
            get { return ctxSvcType; }
        }

        /// <summary>
        /// Gets the type of the service interface.
        /// </summary>
        public Type InterfaceType
        {
            get { return ctxIntfType; }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Internal helper to reset the state of the RPC context.
        /// </summary>
        internal void Reset()
        {
            ctxMessage = null;

            ctxOutgoingHeaders = null;
            ctxOutgoingProperties = null;

            ctxIntfType = null;
            ctxSvcType = null;

            ctxUseMessageResponse = false;

            ctxCurrent = this;
        }
    } // public class RPCContext
} // namespace TridentFramework.RPC
