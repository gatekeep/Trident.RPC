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
/*
 * Based on code from Lidgren Network v3
 * Copyright (C) 2010 Michael Lidgren., All Rights Reserved.
 * Licensed under MIT License.
 */

using System;
using System.Collections.Generic;

using TridentFramework.RPC.Net.Message;

namespace TridentFramework.RPC.Net.Channel
{
    /// <summary>
    /// Result of a SendMessage call
    /// </summary>
    public enum SendResult
    {
        /// <summary>
        /// Message failed to enqueue
        /// </summary>
        Failed = 0,

        /// <summary>
        /// Message was sent immediately
        /// </summary>
        Sent = 1,

        /// <summary>
        /// Message was queued for delivery
        /// </summary>
        Queued = 2,

        /// <summary>
        /// Message was dropped immediately since too many messages were queued
        /// </summary>
        Dropped = 3
    }

    /// <summary>
    /// Network sender channel
    /// </summary>
    public abstract class ISenderChannel
    {
        internal ThreadSafeQueue<OutgoingMessage> queuedSends;

        /*
        ** Properties
        */

        /// <summary>
        /// Gets the network window size
        /// </summary>
        public abstract int WindowSize
        {
            get;
        }

        /// <summary>
        /// Gets the count of queued sends.
        /// </summary>
        internal int QueuedSendsCount
        {
            get { return queuedSends.Count; }
        }

        /*
        ** Methods
        */

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public abstract int GetAllowedSends();

        /// <summary>
        ///
        /// </summary>
        public abstract void Reset();

        /// <summary>
        /// Enqueue an outgoing message
        /// </summary>
        /// <param name="message">Message to enqueue</param>
        /// <returns>Result of message enqueuing</returns>
        public abstract SendResult Enqueue(OutgoingMessage message);

        /// <summary>
        /// Send queued messages
        /// </summary>
        /// <param name="now"></param>
        public abstract void SendQueuedMessages(float now);

        /// <summary>
        /// Receive acknowledge when everything has arrived
        /// </summary>
        /// <param name="now"></param>
        /// <param name="sequenceNumber">Sequence Number</param>
        public abstract void ReceiveAcknowledge(float now, int sequenceNumber);

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        internal virtual bool NeedToSendMessages()
        {
            return queuedSends.Count > 0;
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public int GetFreeWindowSlots()
        {
            return GetAllowedSends() - queuedSends.Count;
        }
    } // internal abstract class NetSenderChannelBase
} // namespace TridentFramework.RPC.Net.Channel
