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
using System.Text;

namespace TridentFramework.RPC.Net.PeerConnection
{
    /// <summary>
    /// Statistics for a peer instance
    /// </summary>
    public sealed class PeerStatistics
    {
        private readonly Peer peer;

        /*
        ** Properties
        */

        /// <summary>
        /// Gets the single instance reference
        /// </summary>
        public static PeerStatistics Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the number of sent packets since the peer was initialized
        /// </summary>
        public int SentPackets
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the number of received packets since the peer was initialized
        /// </summary>
        public int ReceivedPackets
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the number of sent messages since the peer was initialized
        /// </summary>
        public int SentMessages
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the number of received messages since the peer was initialized
        /// </summary>
        public int ReceivedMessages
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the number of received message fragements since the peer was initialized
        /// </summary>
        public int ReceivedFragements
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the number of sent bytes since the peer was initialized
        /// </summary>
        public int SentBytes
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the number of received bytes since the peer was initialized
        /// </summary>
        public int ReceivedBytes
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the number of bytes allocated (and possibly garbage collected) for message storage
        /// </summary>
        public long StorageBytesAllocated
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the number of bytes in the recycled pool
        /// </summary>
        public int BytesInRecyclePool
        {
            get { return peer.storagePoolBytes; }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the PeerStatistics class.
        /// </summary>
        /// <param name="peer">Peer being monitored</param>
        internal PeerStatistics(Peer peer)
        {
            this.peer = peer;
            Instance = this;
            Reset();
        }

        /// <summary>
        /// Reset connection statistics to default.
        /// </summary>
        internal void Reset()
        {
            SentPackets = 0;
            ReceivedPackets = 0;

            SentMessages = 0;
            ReceivedMessages = 0;

            ReceivedFragements = 0;

            SentBytes = 0;
            ReceivedBytes = 0;

            StorageBytesAllocated = 0;
        }

        /// <summary>
        /// Generate packet sent statistics.
        /// </summary>
        /// <param name="numBytes">Number of bytes</param>
        /// <param name="numMessages">Number of messages</param>
#if DEBUG

        internal void PacketSent(int numBytes, int numMessages)
        {
            SentPackets++;
            SentBytes += numBytes;
            SentMessages += numMessages;
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
        /// <param name="numFragments"></param>
#if DEBUG

        internal void PacketReceived(int numBytes, int numMessages, int numFragments)
        {
            ReceivedPackets++;
            ReceivedBytes += numBytes;
            ReceivedMessages += numMessages;
            ReceivedFragements += numFragments;
        }

#else
        internal void PacketReceived(int numBytes, int numMessages, int numFragments)
        {
            return;
        }
#endif

        /// <inheritdoc />
        public override string ToString()
        {
            StringBuilder bdr = new StringBuilder();
            bdr.AppendLine(peer.ConnectionsCount.ToString() + " connections");
            bdr.AppendLine("Sent " + SentBytes + " bytes in " + SentMessages + " messages in " + SentPackets + " packets");
            bdr.AppendLine("Received " + ReceivedBytes + " bytes in " + ReceivedMessages + " messages in " + ReceivedPackets + " packets");
            bdr.AppendLine("Storage allocated " + StorageBytesAllocated + " bytes");
            bdr.AppendLine("Recycled pool " + peer.storagePoolBytes + " bytes");
            return bdr.ToString();
        }
    } // public sealed class PeerStatistics
} // namespace TridentFramework.RPC.Net.PeerConnection
