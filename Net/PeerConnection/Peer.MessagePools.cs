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
using System.Text;

using TridentFramework.RPC.Net.Message;

namespace TridentFramework.RPC.Net.PeerConnection
{
    /// <summary>
    /// Represents a local peer capable of holding zero, one or more connections to remote peers
    /// </summary>
    public partial class Peer
    {
        private List<byte[]> storagePool; // sorted smallest to largest
        private ThreadSafeQueue<OutgoingMessage> outgoingMessagesPool;
        private ThreadSafeQueue<IncomingMessage> incomingMessagesPool;

        internal int storagePoolBytes;
        internal int storageSlotsUsedCount;
        private int maxCacheCount;

        /*
        ** Methods
        */

        /// <summary>
        /// Initialize the peer's message pools
        /// </summary>
        private void InitializePools()
        {
            storageSlotsUsedCount = 0;

            if (Configuration.UseMessageRecycling)
            {
                storagePool = new List<byte[]>(16);
                outgoingMessagesPool = new ThreadSafeQueue<OutgoingMessage>(4);
                incomingMessagesPool = new ThreadSafeQueue<IncomingMessage>(4);
            }
            else
            {
                storagePool = null;
                outgoingMessagesPool = null;
                incomingMessagesPool = null;
            }

            maxCacheCount = Configuration.RecycledCacheMaxCount;
        }

        /// <summary>
        /// Get storage for the message pools.
        /// </summary>
        /// <param name="minimumCapacity">Smallest size the pools can be.</param>
        /// <returns>Byte array</returns>
        internal byte[] GetStorage(int minimumCapacity)
        {
            if (storagePool == null)
                return new byte[minimumCapacity];

            lock (storagePool)
            {
                for (int i = 0; i < storagePool.Count; i++)
                {
                    byte[] retval = storagePool[i];
                    if (retval != null && retval.Length >= minimumCapacity)
                    {
                        storagePool[i] = null;
                        storageSlotsUsedCount--;
                        storagePoolBytes -= retval.Length;
                        return retval;
                    }
                }
            }
            Statistics.StorageBytesAllocated += minimumCapacity;
            return new byte[minimumCapacity];
        }

        /// <summary>
        /// Garbage collect the storage pools.
        /// </summary>
        /// <param name="storage">Byte array for storage pool</param>
        internal void Recycle(byte[] storage)
        {
            if (storagePool == null || storage == null)
                return;

            lock (storagePool)
            {
                int cnt = storagePool.Count;
                for (int i = 0; i < cnt; i++)
                {
                    if (storagePool[i] == null)
                    {
                        storageSlotsUsedCount++;
                        storagePoolBytes += storage.Length;
                        storagePool[i] = storage;
                        return;
                    }
                }

                if (storagePool.Count >= maxCacheCount)
                {
                    // pool is full; replace randomly chosen entry to keep size distribution
                    Random rnd = new Random();
                    var idx = rnd.Next(storagePool.Count);

                    storagePoolBytes -= storagePool[idx].Length;
                    storagePoolBytes += storage.Length;

                    storagePool[idx] = storage; // replace
                }
                else
                {
                    storageSlotsUsedCount++;
                    storagePoolBytes += storage.Length;
                    storagePool.Add(storage);
                }
            }
        }

        /// <summary>
        /// Creates a new message for sending
        /// </summary>
        /// <returns></returns>
        public OutgoingMessage CreateMessage()
        {
            return CreateMessage(Configuration.DefaultOutgoingMessageCapacity);
        }

        /// <summary>
        /// Creates a new message for sending and writes the provided string to it
        /// </summary>
        /// <returns></returns>
        public OutgoingMessage CreateMessage(string content)
        {
            OutgoingMessage om;

            // since this could be null.
            if (string.IsNullOrEmpty(content))
                om = CreateMessage(1); // one byte for the internal variable-length zero byte.
            else
                om = CreateMessage(2 + content.Length); // fair guess

            om.Write(content);
            return om;
        }

        /// <summary>
        /// Creates a new message for sending
        /// </summary>
        /// <param name="initialCapacity">initial capacity in bytes</param>
        /// <returns></returns>
        public OutgoingMessage CreateMessage(int initialCapacity)
        {
            OutgoingMessage retval;
            if (outgoingMessagesPool == null || !outgoingMessagesPool.TryDequeue(out retval))
                retval = new OutgoingMessage();

            NetworkException.Assert(retval.recyclingCount == 0, "Wrong recycling count! Should be zero" + retval.recyclingCount);

            if (initialCapacity > 0)
                retval.Data = GetStorage(initialCapacity);

            return retval;
        }

        /// <summary>
        /// Creates a new message for sending, cloning data from an existing outgoing message.
        /// </summary>
        /// <param name="originalMessage"></param>
        /// <returns></returns>
        private OutgoingMessage CreateMessage(OutgoingMessage originalMessage)
        {
            OutgoingMessage retval;
            if (outgoingMessagesPool == null || !outgoingMessagesPool.TryDequeue(out retval))
                retval = new OutgoingMessage();

            NetworkException.Assert(retval.recyclingCount == 0, "Wrong recycling count! Should be zero" + retval.recyclingCount);

            retval.Data = GetStorage(originalMessage.Data.Length);
            for (int i = 0; i < originalMessage.Data.Length; i++)
                retval.Data[i] = originalMessage.Data[i];

            retval.BitLength = originalMessage.BitLength;
            retval.Position = originalMessage.Position;

            retval.MessageType = originalMessage.MessageType;
            retval.IsSent = originalMessage.IsSent;

            return retval;
        }

        /// <summary>
        /// Create an incoming message.
        /// </summary>
        /// <param name="tp">Message Type</param>
        /// <param name="useStorageData"></param>
        /// <returns>Incoming Message</returns>
        internal IncomingMessage CreateIncomingMessage(IncomingMessageType tp, byte[] useStorageData)
        {
            IncomingMessage retval;
            if (incomingMessagesPool == null || !incomingMessagesPool.TryDequeue(out retval))
                retval = new IncomingMessage(tp);
            else
                retval.MessageType = tp;
            retval.Data = useStorageData;
            return retval;
        }

        /// <summary>
        /// Create an incoming message.
        /// </summary>
        /// <param name="tp">Message Type</param>
        /// <param name="minimumByteSize">Minimum size for message</param>
        /// <returns>Incoming message</returns>
        internal IncomingMessage CreateIncomingMessage(IncomingMessageType tp, int minimumByteSize)
        {
            IncomingMessage retval;
            if (incomingMessagesPool == null || !incomingMessagesPool.TryDequeue(out retval))
                retval = new IncomingMessage(tp);
            else
                retval.MessageType = tp;
            retval.Data = GetStorage(minimumByteSize);
            return retval;
        }

        /// <summary>
        /// Recycles a IncomingMessage instance for reuse; taking pressure off the garbage collector
        /// </summary>
        public void Recycle(IncomingMessage msg)
        {
            if (incomingMessagesPool == null || msg == null)
                return;

            NetworkException.Assert(incomingMessagesPool.Contains(msg) == false, "Recyling already recycled incoming message! Thread race?");

            byte[] storage = msg.Data;
            msg.Data = null;
            Recycle(storage);
            msg.Reset();

            if (incomingMessagesPool.Count < maxCacheCount)
                incomingMessagesPool.Enqueue(msg);
        }

        /// <summary>
        /// Recycles a list of NetIncomingMessage instances for reuse; taking pressure off the garbage collector
        /// </summary>
        public void Recycle(IEnumerable<IncomingMessage> toRecycle)
        {
            if (incomingMessagesPool == null)
                return;

            foreach (var im in toRecycle)
                Recycle(im);
        }

        /// <summary>
        /// Recycles a OutgoingMessage instnace for reuse; taking pressure off the garbage collector
        /// </summary>
        /// <param name="msg"></param>
        internal void Recycle(OutgoingMessage msg)
        {
            if (outgoingMessagesPool == null)
                return;
#if DEBUG
            NetworkException.Assert(outgoingMessagesPool.Contains(msg) == false, "Recyling already recycled outgoing message! Thread race?");
            /*
            if (msg.recyclingCount != 0)
                Messages.Trace("Wrong recycling count! should be zero; found " + msg.recyclingCount);
            */
#endif
            if (!outgoingMessagesPool.Contains(msg))
                return;

            // setting recyclingCount to zero SHOULD be an unnecessary maneuver, if it's not zero something is wrong
            // however, in RELEASE, we'll just have to accept this and move on with life
            msg.recyclingCount = 0;

            byte[] storage = msg.Data;
            msg.Data = null;

            // message fragments cannot be recycled
            // TODO: find a way to recycle large message after all fragments has been acknowledged; or? possibly better just to garbage collect them
            if (msg.fragmentGroup == 0)
                Recycle(storage);

            msg.Reset();
            if (outgoingMessagesPool.Count < maxCacheCount)
                outgoingMessagesPool.Enqueue(msg);
        }

        /// <summary>
        /// Creates an incoming message with the required capacity for releasing to the application
        /// </summary>
        /// <returns></returns>
        internal IncomingMessage CreateIncomingMessage(IncomingMessageType tp, string text)
        {
            IncomingMessage retval;
            if (string.IsNullOrEmpty(text))
            {
                retval = CreateIncomingMessage(tp, 1);
                retval.Write(string.Empty);
                return retval;
            }

            int numBytes = System.Text.Encoding.UTF8.GetByteCount(text);
            retval = CreateIncomingMessage(tp, numBytes + (numBytes > 127 ? 2 : 1));
            retval.Write(text);

            return retval;
        }
    } // public partial class Peer
} // namespace TridentFramework.RPC.Net.PeerConnection
