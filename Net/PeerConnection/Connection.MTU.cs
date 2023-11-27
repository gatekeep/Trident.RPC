/**
 * Copyright (c) 2008-2023 Bryan Biedenkapp., All Rights Reserved.
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

using TridentFramework.RPC.Net.Message;

using TridentFramework.RPC.Utility;

namespace TridentFramework.RPC.Net.PeerConnection
{
    /// <summary>
    /// Represents a connection to a remote peer
    /// </summary>
    public partial class Connection
    {
        private const int PROTOCOL_MAX_MTU = (int)(((float)ushort.MaxValue / 8.0f) - 1.0f);

        private enum ExpandMTUStatus
        {
            /// <summary>
            /// No expand MTU in progress
            /// </summary>
            None,

            /// <summary>
            /// Expand MTU is in progress
            /// </summary>
            InProgress,

            /// <summary>
            /// Finished expanding MTU
            /// </summary>
            Finished
        }

        private ExpandMTUStatus expandMTUStatus;

        private int largestSuccessfulMTU;
        private int smallestFailedMTU;

        private int lastSentMTUAttemptSize;
        private double lastSentMTUAttemptTime;
        private int mtuAttemptFails;

        internal int currentMTU;

        /*
        ** Methods
        */

        /// <summary>
        /// Expand the current MTU
        /// </summary>
        /// <param name="now"></param>
        internal void InitExpandMTU(double now)
        {
            lastSentMTUAttemptTime = now + Peer.Configuration.ExpandMTUFrequency + 1.5f + AverageRoundTripTime; // wait a tiny bit before starting to expand mtu
            largestSuccessfulMTU = 512;
            smallestFailedMTU = -1;
            currentMTU = Peer.Configuration.MaximumTransmissionUnit;
        }

        /// <summary>
        /// MTU expansion heartbeat
        /// </summary>
        /// <param name="now"></param>
        private void MTUExpansionHeartbeat(double now)
        {
            if (expandMTUStatus == ExpandMTUStatus.Finished)
                return;

            if (expandMTUStatus == ExpandMTUStatus.None)
            {
                if (Peer.Configuration.AutoExpandMTU == false)
                {
                    FinalizeMTU(currentMTU);
                    return;
                }

                // begin expansion
                ExpandMTU(now, true);
                return;
            }

            if (now > lastSentMTUAttemptTime + Peer.Configuration.ExpandMTUFrequency)
            {
                mtuAttemptFails++;
                if (mtuAttemptFails == 3)
                {
                    FinalizeMTU(currentMTU);
                    return;
                }

                // timed out; ie. failed
                smallestFailedMTU = lastSentMTUAttemptSize;
                ExpandMTU(now, false);
            }
        }

        /// <summary>
        /// Expand MTU size
        /// </summary>
        /// <param name="now"></param>
        /// <param name="succeeded"></param>
        private void ExpandMTU(double now, bool succeeded)
        {
            int tryMTU;

            // we've never encountered failure
            if (smallestFailedMTU == -1)
            {
                // we've never encountered failure; expand by 25% each time
                tryMTU = (int)((float)currentMTU * 1.25f);
            }
            else
            {
                // we HAVE encountered failure; so try in between
                tryMTU = (int)(((float)smallestFailedMTU + (float)largestSuccessfulMTU) / 2.0f);
            }

            if (tryMTU > PROTOCOL_MAX_MTU)
                tryMTU = PROTOCOL_MAX_MTU;

            if (tryMTU == largestSuccessfulMTU)
            {
                FinalizeMTU(largestSuccessfulMTU);
                return;
            }

            SendExpandMTU(now, tryMTU);
        }

        /// <summary>
        /// Send expand MTU message
        /// </summary>
        /// <param name="now"></param>
        /// <param name="size">Size to expand</param>
        private void SendExpandMTU(double now, int size)
        {
            OutgoingMessage om = Peer.CreateMessage(size);
            byte[] tmp = new byte[size];
            om.Write(tmp);
            om.MessageType = MessageType.ExpandMTURequest;
            int len = om.Encode(Peer.sendBuffer, 0, 0);

            bool ok = Peer.SendMTUPacket(len, RemoteEndpoint);
            if (ok == false)
            {
                //peer.LogDebug("Send MTU failed for size " + size);

                // failure
                if (smallestFailedMTU == -1 || size < smallestFailedMTU)
                {
                    smallestFailedMTU = size;
                    mtuAttemptFails++;
                    if (mtuAttemptFails >= Peer.Configuration.ExpandMTUFailAttempts)
                    {
                        FinalizeMTU(largestSuccessfulMTU);
                        return;
                    }
                }
                ExpandMTU(now, false);
                return;
            }

            lastSentMTUAttemptSize = size;
            lastSentMTUAttemptTime = now;

            Statistics.PacketSent(len, 1);
        }

        /// <summary>
        /// Finalize MTU size
        /// </summary>
        /// <param name="size">MTU size</param>
        private void FinalizeMTU(int size)
        {
            if (expandMTUStatus == ExpandMTUStatus.Finished)
                return;
            expandMTUStatus = ExpandMTUStatus.Finished;
            currentMTU = size;
            if (currentMTU != Peer.Configuration.MaximumTransmissionUnit)
                RPCLogger.Trace("Expanded Maximum Transmission Unit to: " + currentMTU + " bytes");
            return;
        }

        /// <summary>
        /// Send MTU expand success message
        /// </summary>
        /// <param name="size">Size expanded</param>
        private void SendMTUSuccess(int size)
        {
            OutgoingMessage om = Peer.CreateMessage(1);
            om.Write(size);
            om.MessageType = MessageType.ExpandMTUSuccess;
            int len = om.Encode(Peer.sendBuffer, 0, 0);
            bool connectionReset;
            Peer.SendPacket(len, RemoteEndpoint, 1, out connectionReset);

            Statistics.PacketSent(len, 1);
        }

        /// <summary>
        /// Handle MTU expansion success
        /// </summary>
        /// <param name="now"></param>
        /// <param name="size">Size expanded</param>
        private void HandleExpandMTUSuccess(double now, int size)
        {
            if (size > largestSuccessfulMTU)
                largestSuccessfulMTU = size;

            if (size < currentMTU)
                return;

            currentMTU = size;
            ExpandMTU(now, true);
        }
    } // public partial class Connection
} // namespace TridentFramework.RPC.Net
