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

namespace TridentFramework.RPC
{
    /// <summary>
    /// Defines a message inspector object that can be added to a channel to view or modify messages.
    /// </summary>
    public interface IChannelMessageInspector
    {
        /*
        ** Methods
        */

        /// <summary>
        /// Enables inspection or modification of a RPC context after a reply message is recieved but
        /// prior to passing it back to the client application.
        /// </summary>
        /// <param name="message">RPC message</param>
        void AfterRecieveReply(RPCMessage message);

        /// <summary>
        /// Enables inspection or modification of a RPC context before a request message is sent to
        /// a service.
        /// </summary>
        /// <param name="context">RPC context</param>
        /// <param name="message">RPC message</param>
        /// <param name="channel">RPC channel making the request</param>
        /// <returns></returns>
        object BeforeSendRequest(RPCContext context, ref RPCMessage message, RPCChannel channel);
    } // public interface IChannelMessageInspector
} // namespace TridentFramework.RPC
