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
/*
 * Based on code from Lidgren Network v3
 * Copyright (C) 2010 Michael Lidgren., All Rights Reserved.
 * Licensed under MIT License.
 */

using System;
using System.Collections.Generic;
using System.Threading;

using TridentFramework.RPC.Net.Message;
using TridentFramework.RPC.Net.PeerConnection;

using TridentFramework.RPC.Utility;

namespace TridentFramework.RPC.Net.Channel
{
    /// <summary>
    /// Sender part of Selective repeat ARQ for a particular NetChannel
    /// </summary>
    internal sealed class ReliableSenderChannel : ISenderChannel
    {
        private Connection connection;
        private int windowStart;
        private int windowSize;
        private int sendStart;

        private bool anyStoredResends;

        private BitVector receivedAcks;
        internal StoredReliableMessage[] storedMessages;

        internal float resendDelay;

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
        /// Initializes a new instance of the <see cref="ReliableSenderChannel"/> class.
        /// </summary>
        /// <param name="connection">Connection channel belongs to</param>
        /// <param name="windowSize"></param>
        internal ReliableSenderChannel(Connection connection, int windowSize)
        {
            this.connection = connection;
            this.windowSize = windowSize;
            this.windowStart = 0;
            this.sendStart = 0;
            anyStoredResends = false;
            receivedAcks = new BitVector(NetUtility.NumSequenceNumbers);

            storedMessages = new StoredReliableMessage[windowSize];
            queuedSends = new ThreadSafeQueue<OutgoingMessage>(8);
            resendDelay = connection.GetResendDelay();
        }

        /// <inheritdoc />
        internal override bool NeedToSendMessages()
        {
            return base.NeedToSendMessages() || anyStoredResends;
        }

        /// <inheritdoc />
        public override int GetAllowedSends()
        {
            int retval = windowSize - ((sendStart + NetUtility.NumSequenceNumbers) - windowStart) % NetUtility.NumSequenceNumbers;
            NetworkException.Assert(retval >= 0 && retval <= windowSize);
            return retval;
        }

        /// <inheritdoc />
        public override void Reset()
        {
            receivedAcks.Clear();
            for (int i = 0; i < storedMessages.Length; i++)
                storedMessages[i].Reset();
            anyStoredResends = false;
            queuedSends.Clear();
            windowStart = 0;
            sendStart = 0;
        }

        /// <inheritdoc />
        public override SendResult Enqueue(OutgoingMessage message)
        {
            queuedSends.Enqueue(message);
            connection.Peer.NeedFlushSendQueue = true;
            if (queuedSends.Count <= GetAllowedSends())
                return SendResult.Sent;
            return SendResult.Queued;
        }

        /// <inheritdoc />
        public override void SendQueuedMessages(float now)
        {
            // resends
            anyStoredResends = false;
            for (int i = 0; i < storedMessages.Length; i++)
            {
                var storedMessage = storedMessages[i];
                OutgoingMessage om = storedMessage.Message;
                if (om == null)
                    continue;

                float t = storedMessage.LastSent;
                if (t > 0 && (now - t) > resendDelay)
                {
                    connection.Statistics.MessageResent(MessageResendReason.Delay);

                    connection.QueueSendMessage(om, storedMessage.SequenceNumber);

                    storedMessages[i].LastSent = now;
                    storedMessages[i].NumSent++;
                }
            }

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
                NetworkException.Assert(num == GetAllowedSends());
            }
        }

        /// <summary>
        /// Execute a send
        /// </summary>
        /// <param name="now"></param>
        /// <param name="message">Message to send</param>
        private void ExecuteSend(float now, OutgoingMessage message)
        {
            int seqNr = sendStart;
            sendStart = (sendStart + 1) % NetUtility.NumSequenceNumbers;

            // must increment recycle count here, since it's decremented in QueueSendMessage and we want to keep it for the future in case or resends
            // we will decrement once more in DestoreMessage for final recycling
            Interlocked.Increment(ref message.recyclingCount);

            connection.QueueSendMessage(message, seqNr);

            int storeIndex = seqNr % windowSize;
            NetworkException.Assert(storedMessages[storeIndex].Message == null);

            storedMessages[storeIndex].NumSent++;
            storedMessages[storeIndex].Message = message;
            storedMessages[storeIndex].LastSent = now;
            storedMessages[storeIndex].SequenceNumber = seqNr;
            anyStoredResends = true;

            return;
        }

        /// <summary>
        /// Destore a reliable message from the stored messages
        /// </summary>
        /// <param name="now"></param>
        /// <param name="storeIndex"></param>
        /// <param name="resetTimeout"></param>
        private void DestoreMessage(double now, int storeIndex, out bool resetTimeout)
        {
            // reset timeout if we receive ack within kThreshold of sending it
            const double kThreshold = 2.0;
            var srm = storedMessages[storeIndex];
            resetTimeout = (srm.NumSent == 1) && (now - srm.LastSent < kThreshold);

            var storedMessage = srm.Message;

#if DEBUG
            if (storedMessage == null)
                throw new NetworkException("m_storedMessages[" + storeIndex + "].Message is null; sent " + storedMessages[storeIndex].NumSent + " times, last time " + (NetTime.Now - storedMessages[storeIndex].LastSent) + " seconds ago");
#else
			if (storedMessage != null)
			{
#endif
            // on each destore; reduce recyclingcount so that when all instances are destored, the outgoing message can be recycled
            Interlocked.Decrement(ref storedMessage.recyclingCount);
            if (storedMessage.recyclingCount <= 0)
                connection.Peer.Recycle(storedMessage);
#if !DEBUG
			}
#endif
            storedMessages[storeIndex] = new StoredReliableMessage();
        }

        /// <inheritdoc />
        public override void ReceiveAcknowledge(float now, int seqNr)
        {
            // late (dupe), on time or early ack?
            int relate = NetUtility.RelativeSequenceNumber(seqNr, windowStart);

            if (relate < 0)
                return; // late/duplicate ack

            if (relate == 0)
            {
                // ack arrived right on time
                NetworkException.Assert(seqNr == windowStart);

                bool resetTimeout;
                receivedAcks[windowStart] = false;
                DestoreMessage(now, windowStart % windowSize, out resetTimeout);
                windowStart = (windowStart + 1) % NetUtility.NumSequenceNumbers;

                // advance window if we already have early acks
                while (receivedAcks.Get(windowStart))
                {
                    receivedAcks[windowStart] = false;
                    bool rt;
                    DestoreMessage(now, windowStart % windowSize, out rt);
                    resetTimeout |= rt;

                    NetworkException.Assert(storedMessages[windowStart % windowSize].Message == null); // should already be destored
                    windowStart = (windowStart + 1) % NetUtility.NumSequenceNumbers;
                }

                if (resetTimeout)
                    connection.ResetTimeout(now);
                return;
            }

            //
            // early ack... (if it has been sent!)
            //
            // If it has been sent either the windowStart message was lost
            // ... or the ack for that message was lost
            //
            int sendRelate = NetUtility.RelativeSequenceNumber(seqNr, sendStart);
            if (sendRelate <= 0)
            {
                // yes, we've sent this message - it's an early (but valid) ack
                if (receivedAcks[seqNr])
                {
                    // we've already destored/been acked for this message
                }
                else
                    receivedAcks[seqNr] = true;
            }
            else if (sendRelate > 0)
            {
                // uh... we haven't sent this message yet? Weird, dupe or error...
                NetworkException.Assert(false, "Got ack for message not yet sent?");
                return;
            }

            // Ok, lets resend all missing acks
            int rnr = seqNr;
            do
            {
                rnr--;
                if (rnr < 0)
                    rnr = NetUtility.NumSequenceNumbers - 1;

                if (receivedAcks[rnr])
                {
                    RPCLogger.Trace("Not resending #" + rnr + " (since we got ack)");
                }
                else
                {
                    int slot = rnr % windowSize;
                    NetworkException.Assert(storedMessages[slot].Message != null);
                    if (storedMessages[slot].NumSent == 1)
                    {
                        // just sent once; resend immediately since we found gap in ack sequence
                        OutgoingMessage rmsg = storedMessages[slot].Message;
                        if (now - storedMessages[slot].LastSent < (resendDelay * 0.35f))
                        {
                            // already resent recently
                        }
                        else
                        {
                            storedMessages[slot].LastSent = now;
                            storedMessages[slot].NumSent++;
                            connection.Statistics.MessageResent(MessageResendReason.HoleInSequence);
                            Interlocked.Increment(ref rmsg.recyclingCount); // increment this since it's being decremented in QueueSendMessage
                            connection.QueueSendMessage(rmsg, rnr);
                        }
                    }
                }
            }
            while (rnr != windowStart);
        }
    } // internal sealed class ReliableSenderChannel : ISenderChannel
} // namespace TridentFramework.RPC.Net.Channel
