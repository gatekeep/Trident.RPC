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
using System.Threading;

using TridentFramework.RPC.Net.Message;
using TridentFramework.RPC.Net.PeerConnection;

namespace TridentFramework.RPC.Net.Channel
{
    /// <summary>
    /// Sender part of Selective repeat ARQ for a particular NetChannel
    /// </summary>
    internal sealed class UnreliableSenderChannel : ISenderChannel
    {
        private Connection connection;
        private int windowStart;
        private int windowSize;
        private int sendStart;
        private bool doFlowControl;

        private BitVector receivedAcks;

        /*
        ** Properties
        */

        /// <inheritdoc />
        public override int WindowSize
        {
            get { return windowSize; }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="UnreliableSenderChannel"/> class.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="windowSize"></param>
        /// <param name="method"></param>
        internal UnreliableSenderChannel(Connection connection, int windowSize, DeliveryMethod method)
        {
            this.connection = connection;
            this.windowSize = windowSize;
            windowStart = 0;
            sendStart = 0;
            receivedAcks = new BitVector(NetUtility.NumSequenceNumbers);
            queuedSends = new ThreadSafeQueue<OutgoingMessage>(8);

            doFlowControl = true;
            if (method == DeliveryMethod.Unreliable && connection.Peer.Configuration.SuppressUnreliableUnorderedAcks == true)
                doFlowControl = false;
        }

        /// <inheritdoc />
        public override int GetAllowedSends()
        {
            if (!doFlowControl)
                return 2; // always allowed to send without flow control!
            int retval = windowSize - ((sendStart + NetUtility.NumSequenceNumbers) - windowStart) % windowSize;
            NetworkException.Assert(retval >= 0 && retval <= windowSize);
            return retval;
        }

        /// <inheritdoc />
        public override void Reset()
        {
            receivedAcks.Clear();
            queuedSends.Clear();
            windowStart = 0;
            sendStart = 0;
        }

        /// <inheritdoc />
        public override SendResult Enqueue(OutgoingMessage message)
        {
            int queueLen = queuedSends.Count + 1;
            int left = GetAllowedSends();
            if (queueLen > left || (message.LengthBytes > connection.currentMTU && connection.Peer.Configuration.UnreliableSizeBehavior == UnreliableSizeBehavior.DropAboveMTU))
                return SendResult.Dropped;

            queuedSends.Enqueue(message);
            connection.Peer.NeedFlushSendQueue = true; // a race condition to this variable will simply result in a single superflous call to FlushSendQueue()
            return SendResult.Sent;
        }

        /// <inheritdoc />
        public override void SendQueuedMessages(float now)
        {
            int num = GetAllowedSends();
            if (num < 1)
                return;

            // queued sends
            while (queuedSends.Count > 0 && num > 0)
            {
                OutgoingMessage om;
                if (queuedSends.TryDequeue(out om))
                    ExecuteSend(now, om);
                num--;
            }
        }

        /// <summary>
        /// Execute a send
        /// </summary>
        /// <param name="now"></param>
        /// <param name="message">Message to send</param>
        private void ExecuteSend(float now, OutgoingMessage message)
        {
            connection.Peer.VerifyNetworkThread();

            int seqNr = sendStart;
            sendStart = (sendStart + 1) % NetUtility.NumSequenceNumbers;

            connection.QueueSendMessage(message, seqNr);

            if (message.recyclingCount <= 0)
                connection.Peer.Recycle(message);

            return;
        }

        /// <inheritdoc />
        public override void ReceiveAcknowledge(float now, int seqNr)
        {
            if (doFlowControl == false)
            {
                // we have no use for acks on this channel since we don't respect the window anyway
                return;
            }

            // late (dupe), on time or early ack?
            int relate = NetUtility.RelativeSequenceNumber(seqNr, windowStart);

            if (relate < 0)
                return; // late/duplicate ack

            if (relate == 0)
            {
                // ack arrived right on time
                NetworkException.Assert(seqNr == windowStart);

                receivedAcks[windowStart] = false;
                windowStart = (windowStart + 1) % NetUtility.NumSequenceNumbers;

                return;
            }

            // Advance window to this position
            receivedAcks[seqNr] = true;

            while (windowStart != seqNr)
            {
                receivedAcks[windowStart] = false;
                windowStart = (windowStart + 1) % NetUtility.NumSequenceNumbers;
            }
        }
    } // internal sealed class UnreliableSenderChannel : ISenderChannel
} // namespace TridentFramework.RPC.Net.Channel
