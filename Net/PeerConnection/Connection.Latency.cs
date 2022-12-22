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

using TridentFramework.RPC.Net.Channel;
using TridentFramework.RPC.Net.Message;

using TridentFramework.RPC.Utility;

namespace TridentFramework.RPC.Net.PeerConnection
{
    /// <summary>
    /// Represents a connection to a remote peer
    /// </summary>
    public partial class Connection
    {
        private float sentPingTime;
        private int sentPingNumber;
        private double timeoutDeadline = float.MaxValue;

        // local time value + remoteTimeOffset = remote time value
        internal double remoteTimeOffset;

        /*
        ** Properties
        */

        /// <summary>
        /// Gets the current average round trip time in seconds
        /// </summary>
        public float AverageRoundTripTime
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the time offset between this peer and the remote peer
        /// </summary>
        public float RemoteTimeOffset
        {
            get { return (float)remoteTimeOffset; }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initialize the remote time offset
        /// </summary>
        /// <param name="remoteSendTime">Remote time</param>
        internal void InitializeRemoteTimeOffset(float remoteSendTime)
        {
            remoteTimeOffset = (remoteSendTime + (AverageRoundTripTime / 2.0)) - NetTime.Now;
        }

        /// <summary>
        /// Gets local time value comparable to NetTime.Now from a remote value
        /// </summary>
        /// <returns></returns>
        public double GetLocalTime(double remoteTimestamp)
        {
            return remoteTimestamp - remoteTimeOffset;
        }

        /// <summary>
        /// Gets the remote time value for a local time value produced by NetTime.Now
        /// </summary>
        /// <returns></returns>
        public double GetRemoteTime(double localTimestamp)
        {
            return localTimestamp + remoteTimeOffset;
        }

        /// <summary>
        /// Initialize ping requests
        /// </summary>
        internal void InitializePing()
        {
            float now = (float)NetTime.Now;

            // randomize ping sent time (0.25 - 1.0 x ping interval)
            sentPingTime = now;
            sentPingTime -= Peer.Configuration.PingInterval * 0.25f; // delay ping for a little while
            sentPingTime -= NetUtility.NextSingle() * (Peer.Configuration.PingInterval * 0.75f);
            timeoutDeadline = now + (Peer.Configuration.ConnectionTimeout * 2.0f); // initially allow a little more time

            // make it better, quick :-)
            SendPing();
        }

        /// <summary>
        /// Send ping to remote side
        /// </summary>
        internal void SendPing()
        {
            Peer.VerifyNetworkThread();

            sentPingNumber++;

            sentPingTime = (float)NetTime.Now;
            OutgoingMessage om = Peer.CreateMessage(1);
            om.Write((byte)sentPingNumber); // truncating to 0-255
            om.MessageType = MessageType.Ping;

            int len = om.Encode(Peer.sendBuffer, 0, 0);
            bool connectionReset;
            Peer.SendPacket(len, RemoteEndpoint, 1, out connectionReset);

            Statistics.PacketSent(len, 1);
        }

        /// <summary>
        /// Send response to ping
        /// </summary>
        /// <param name="pingNumber">Value sent with ping</param>
        internal void SendPong(int pingNumber)
        {
            Peer.VerifyNetworkThread();

            OutgoingMessage om = Peer.CreateMessage(5);
            om.Write((byte)pingNumber);
            om.Write((float)NetTime.Now); // we should update this value to reflect the exact point in time the packet is SENT
            om.MessageType = MessageType.Pong;

            int len = om.Encode(Peer.sendBuffer, 0, 0);
            bool connectionReset;

            Peer.SendPacket(len, RemoteEndpoint, 1, out connectionReset);

            Statistics.PacketSent(len, 1);
        }

        /// <summary>
        /// Received a pong
        /// </summary>
        /// <param name="now"></param>
        /// <param name="pongNumber">Value sent with pong</param>
        /// <param name="remoteSendTime">Remote time</param>
        internal void ReceivedPong(float now, int pongNumber, float remoteSendTime)
        {
            if ((byte)pongNumber != (byte)sentPingNumber)
            {
                RPCLogger.WriteWarning("Ping/Pong mismatch; dropped message?");
                return;
            }

            timeoutDeadline = now + Peer.Configuration.ConnectionTimeout;

            float rtt = now - sentPingTime;
            NetworkException.Assert(rtt >= 0);

            double diff = (remoteSendTime + (rtt / 2.0)) - now;

            if (AverageRoundTripTime < 0)
            {
                remoteTimeOffset = diff;
                AverageRoundTripTime = rtt;
                RPCLogger.Trace("Initiated average round trip time to " + NetTime.ToReadable(AverageRoundTripTime) + " Remote time is: " + (now + diff));
            }
            else
            {
                AverageRoundTripTime = (AverageRoundTripTime * 0.7f) + (float)(rtt * 0.3f);

                remoteTimeOffset = ((remoteTimeOffset * (double)(sentPingNumber - 1)) + diff) / (double)sentPingNumber;
                RPCLogger.Trace("Updated average round trip time to " + NetTime.ToReadable(AverageRoundTripTime) + ", remote time to " + (now + remoteTimeOffset) + " (ie. diff " + remoteTimeOffset + ")");
            }

            // update resend delay for all channels
            float resendDelay = GetResendDelay();
            foreach (var chan in sendChannels)
            {
                var rchan = chan as ReliableSenderChannel;
                if (rchan != null)
                    rchan.resendDelay = resendDelay;
            }

            // peer.LogVerbose("Timeout deadline pushed to  " + timeoutDeadline);

            // notify the application that average rtt changed
            if (Peer.Configuration.IsMessageTypeEnabled(IncomingMessageType.ConnectionLatencyUpdated))
            {
                IncomingMessage update = Peer.CreateIncomingMessage(IncomingMessageType.ConnectionLatencyUpdated, 4);
                update.SenderConnection = this;
                update.SenderEndpoint = this.RemoteEndpoint;
                update.Write(rtt);
                Peer.ReleaseMessage(update);
            }
        }
    } // public partial class Connection
} // namespace TridentFramework.RPC.Net.PeerConnection
