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
using System.Text;

using TridentFramework.RPC.Net.Channel;

namespace TridentFramework.RPC.Net.PeerConnection
{
    internal enum MessageResendReason
    {
        /// <summary>
        /// Message was delayed
        /// </summary>
        Delay,

        /// <summary>
        /// There was a "hole" in the received sequence
        /// </summary>
        HoleInSequence
    }

    /// <summary>
    /// Helper class to collect statistics for connections.
    /// </summary>
    public sealed class ConnectionStatistics
    {
        private readonly Connection connection;

        internal int sentMessages;
        internal int receivedMessages;
        internal int receivedFragments;

        internal int resentMessagesDueToDelay;
        internal int resentMessagesDueToHole;

        /*
        ** Properties
        */

        /// <summary>
        /// Gets the single instance reference
        /// </summary>
        public static ConnectionStatistics Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the number of sent packets for this connection
        /// </summary>
        public int SentPackets
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the number of received packets for this connection
        /// </summary>
        public int ReceivedPackets
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the number of sent bytes for this connection
        /// </summary>
        public int SentBytes
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the number of received bytes for this connection
        /// </summary>
        public int ReceivedBytes
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the number of sent messages for this connection
        /// </summary>
        public long SentMessages
        {
            get { return sentMessages; }
        }

        /// <summary>
        /// Gets the number of received messages for this connection
        /// </summary>
        public long ReceivedMessages
        {
            get { return receivedMessages; }
        }

        /// <summary>
        /// Gets the number of resent reliable messages for this connection
        /// </summary>
        public int ResentMessages
        {
            get { return resentMessagesDueToHole + resentMessagesDueToDelay; }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the ConnectionStatistics class.
        /// </summary>
        /// <param name="conn">Network connection monitored</param>
        internal ConnectionStatistics(Connection conn)
        {
            connection = conn;
            Instance = this;
            Reset();
        }

        /// <summary>
        /// Reset connection statistics to default.
        /// </summary>
        private void Reset()
        {
            SentPackets = 0;
            ReceivedPackets = 0;
            SentBytes = 0;
            ReceivedBytes = 0;
            sentMessages = 0;
            receivedMessages = 0;
            receivedFragments = 0;
            resentMessagesDueToDelay = 0;
            resentMessagesDueToHole = 0;
        }

        /// <summary>
        /// Generate packet sent statistics.
        /// </summary>
        /// <param name="numBytes">Number of bytes</param>
        /// <param name="numMessages">Number of messages</param>
#if DEBUG

        internal void PacketSent(int numBytes, int numMessages)
        {
            NetworkException.Assert(numBytes > 0 && numMessages > 0);
            SentPackets++;
            SentBytes += numBytes;
            sentMessages += numMessages;
        }

#else
        internal void PacketSent(int numBytes, int numMessages)
        {
            return;
        }
#endif

        /// <summary>
        /// Generate packet received statistics.
        /// </summary>
        /// <param name="numBytes">Number of bytes</param>
        /// <param name="numMessages">Number of messages</param>
        /// <param name="numFragments">Number of fragments</param>
#if DEBUG

        internal void PacketReceived(int numBytes, int numMessages, int numFragments)
        {
            NetworkException.Assert(numBytes > 0 && numMessages > 0);
            ReceivedPackets++;
            ReceivedBytes += numBytes;
            receivedMessages += numMessages;
            receivedFragments += numFragments;
        }

#else
        internal void PacketReceived(int numBytes, int numMessages, int numFragments)
        {
            return;
        }
#endif

        /// <summary>
        /// Generate message resent statistics.
        /// </summary>
        /// <param name="reason">Reason for message resent</param>
#if DEBUG

        internal void MessageResent(MessageResendReason reason)
        {
            if (reason == MessageResendReason.Delay)
                resentMessagesDueToDelay++;
            else
                resentMessagesDueToHole++;
        }

#else
        internal void MessageResent(MessageResendReason reason)
        {
            return;
        }
#endif

        /// <inheritdoc />
        public override string ToString()
        {
            StringBuilder bdr = new StringBuilder();
            //bdr.AppendLine("Average round trip time: " + NetTime.ToReadable(connection.averageRoundtripTime));
            bdr.AppendLine("Sent " + SentBytes + " bytes in " + sentMessages + " messages in " + SentPackets + " packets");
            bdr.AppendLine("Received " + ReceivedBytes + " bytes in " + receivedMessages + " messages in " + ReceivedPackets + " packets");

            if (resentMessagesDueToDelay > 0)
                bdr.AppendLine("Resent messages (delay): " + resentMessagesDueToDelay);
            if (resentMessagesDueToDelay > 0)
                bdr.AppendLine("Resent messages (holes): " + resentMessagesDueToHole);

            int numUnsent = 0;
            int numStored = 0;
            foreach (ISenderChannel sendChan in connection.SendChannels)
            {
                if (sendChan == null)
                    continue;
                numUnsent += sendChan.queuedSends.Count;

                var relSendChan = sendChan as ReliableSenderChannel;
                if (relSendChan != null)
                {
                    for (int i = 0; i < relSendChan.storedMessages.Length; i++)
                        if (relSendChan.storedMessages[i].Message != null)
                            numStored++;
                }
            }

            int numWithheld = 0;
            foreach (IReceiverChannel recChan in connection.ReceiveChannels)
            {
                var relRecChan = recChan as ReliableOrderedReceiver;
                if (relRecChan != null)
                {
                    for (int i = 0; i < relRecChan.withheldMessages.Length; i++)
                        if (relRecChan.withheldMessages[i] != null)
                            numWithheld++;
                }
            }

            bdr.AppendLine("Unsent messages: " + numUnsent);
            bdr.AppendLine("Stored messages: " + numStored);
            bdr.AppendLine("Withheld messages: " + numWithheld);

            return bdr.ToString();
        }
    } // public sealed class ConnectionStatistics
} // namespace TridentFramework.RPC.Net
