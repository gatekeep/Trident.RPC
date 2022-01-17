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

using TridentFramework.RPC.Net.Channel;
using TridentFramework.RPC.Net.Message;

using TridentFramework.RPC.Utility;

namespace TridentFramework.RPC.Net.PeerConnection
{
    /// <summary>
    ///
    /// </summary>
    internal class ReceivedFragmentGroup
    {
        public byte[] Data;
        public BitVector ReceivedChunks;
    } // internal class ReceivedFragmentGroup

    /// <summary>
    /// Represents a local peer capable of holding zero, one or more connections to remote peers
    /// </summary>
    public partial class Peer
    {
        private int lastUsedFragmentGroup;

        private Dictionary<Connection, Dictionary<int, ReceivedFragmentGroup>> receivedFragmentGroups;

        /*
        ** Methods
        */

        /// <summary>
        /// Send fragmented message
        /// </summary>
        /// <param name="msg">Message to send</param>
        /// <param name="recipients">Endpoints to send to</param>
        /// <param name="method">Method of delivery</param>
        /// <param name="sequenceChannel"></param>
        /// <param name="handleEC"></param>
        private SendResult SendFragmentedMessage(OutgoingMessage msg, IList<Connection> recipients, DeliveryMethod method, int sequenceChannel, bool handleEC)
        {
            // Note: this group id is PER SENDING/NetPeer; ie. same id is sent to all recipients;
            // this should be ok however; as long as recipients differentiate between same id but different sender
            int group = Interlocked.Increment(ref lastUsedFragmentGroup);
            if (group >= NetUtility.MaxFragmentationGroups)
            {
                // @TODO: not thread safe; but in practice probably not an issue
                lastUsedFragmentGroup = 1;
                group = 1;
            }
            msg.fragmentGroup = group;

            // do not send msg; but set fragmentgroup in case user tries to recycle it immediately

            SendResult retval = SendResult.Sent;

            foreach (Connection recipient in recipients)
            {
                OutgoingMessage om = null;
                if (handleEC)
                {
                    om = CreateMessage(msg);

                    if (Configuration.EnableEncryption && Configuration.NegotiateEncryption)
                    {
                        om = CreateMessage(msg);
                        EncryptMessage(om, recipient);
                    }

                    if (Configuration.EnableCompression)
                        CompressMessage(om);
                }
                else
                    om = msg;

                // create fragmentation specifics
                int totalBytes = om.LengthBytes;

                // determine minimum mtu for all recipients
                int mtu = GetMTU(recipients);
                int bytesPerChunk = FragmentationHelper.GetBestChunkSize(group, totalBytes, mtu);

                int numChunks = totalBytes / bytesPerChunk;
                if (numChunks * bytesPerChunk < totalBytes)
                    numChunks++;

                int bitsPerChunk = bytesPerChunk * 8;
                int bitsLeft = om.BitLength;
                for (int i = 0; i < numChunks; i++)
                {
                    OutgoingMessage chunk = CreateMessage(mtu);

                    chunk.BitLength = bitsLeft > bitsPerChunk ? bitsPerChunk : bitsLeft;
                    chunk.Data = om.Data;
                    chunk.fragmentGroup = group;
                    chunk.fragmentGroupTotalBits = totalBytes * 8;
                    chunk.fragmentChunkByteSize = bytesPerChunk;
                    chunk.fragmentChunkNumber = i;

                    NetworkException.Assert(chunk.BitLength != 0);
                    NetworkException.Assert(chunk.GetEncodedSize() < mtu);

                    Interlocked.Add(ref chunk.recyclingCount, recipients.Count);

                    SendResult res = recipient.EnqueueMessage(chunk, method, sequenceChannel);
                    if (res == SendResult.Dropped)
                        Interlocked.Decrement(ref chunk.recyclingCount);
                    if ((int)res > (int)retval)
                        retval = res; // return "worst" result

                    bitsLeft -= bitsPerChunk;
                }
            }

            return retval;
        }

        /// <summary>
        /// Handle released fragment
        /// </summary>
        /// <param name="im">Incoming message</param>
        private void HandleReleasedFragment(IncomingMessage im)
        {
            VerifyNetworkThread();

            // read fragmentation header and combine fragments
            int group;
            int totalBits;
            int chunkByteSize;
            int chunkNumber;
            int ptr = FragmentationHelper.ReadHeader(im.Data, 0, out group, out totalBits, out chunkByteSize, out chunkNumber);

            NetworkException.Assert(im.LengthBytes > ptr);
            NetworkException.Assert(group > 0);
            NetworkException.Assert(totalBits > 0);
            NetworkException.Assert(chunkByteSize > 0);

            int totalBytes = NetUtility.BytesToHoldBits((int)totalBits);
            int totalNumChunks = totalBytes / chunkByteSize;
            if (totalNumChunks * chunkByteSize < totalBytes)
                totalNumChunks++;

            NetworkException.Assert(chunkNumber < totalNumChunks);

            if (chunkNumber >= totalNumChunks)
            {
                RPCLogger.Trace("Index out of bounds for chunk " + chunkNumber + " (total chunks " + totalNumChunks + ")");
                return;
            }

            Dictionary<int, ReceivedFragmentGroup> groups;
            if (!receivedFragmentGroups.TryGetValue(im.SenderConnection, out groups))
            {
                groups = new Dictionary<int, ReceivedFragmentGroup>();
                receivedFragmentGroups[im.SenderConnection] = groups;
            }

            ReceivedFragmentGroup info;
            if (!groups.TryGetValue(group, out info))
            {
                info = new ReceivedFragmentGroup();
                info.Data = new byte[totalBytes];
                info.ReceivedChunks = new BitVector(totalNumChunks);
                groups[group] = info;
            }

            info.ReceivedChunks[chunkNumber] = true;

            // copy to data
            int offset = chunkNumber * chunkByteSize;
            Buffer.BlockCopy(im.Data, ptr, info.Data, offset, im.LengthBytes - ptr);

            int cnt = info.ReceivedChunks.Count();
            RPCLogger.Trace("Received fragment " + chunkNumber + " of " + totalNumChunks + " (" + cnt + " chunks received)");
            if (info.ReceivedChunks.Count() == totalNumChunks)
            {
                // Done! Transform this incoming message
                im.Data = info.Data;
                im.BitLength = (int)totalBits;
                im.IsFragment = false;

                RPCLogger.Trace("Fragment group #" + group + " fully received in " + totalNumChunks + " chunks (" + (totalBits / 8) + " bytes)");
                ReleaseMessage(im);
            }
            else
            {
                // data has been copied; recycle this incoming message
                Recycle(im);
            }

            return;
        }
    } // public partial class Peer
} // namespace TridentFramework.RPC.Net.PeerConnection
