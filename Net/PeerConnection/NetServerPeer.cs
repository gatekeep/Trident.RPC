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
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

using TridentFramework.RPC.Net.Channel;
using TridentFramework.RPC.Net.Message;
using TridentFramework.RPC.Net.PeerConnection;

namespace TridentFramework.RPC.Net.PeerConnection
{
    /// <summary>
    /// Specialized version of peer used for "server" peers
    /// </summary>
    public class NetServerPeer : Peer
    {
        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the Network class.
        /// </summary>
        public NetServerPeer(PeerConfiguration config)
            : base(config)
        {
            config.AcceptIncomingConnections = true;
        }

        /// <summary>
        /// Send a message to a specific client
        /// </summary>
        /// <param name="msg">The message to send</param>
        /// <param name="recipient">ID of recipient</param>
        /// <param name="method">How to deliver the message</param>
        /// <returns></returns>
        public SendResult SendMessageTo(OutgoingMessage msg, Guid recipient, DeliveryMethod method)
        {
            return SendMessageTo(msg, NetUtility.GuidToLong(recipient), method);
        }

        /// <summary>
        /// Send a message to a specific client
        /// </summary>
        /// <param name="msg">The message to send</param>
        /// <param name="recipient">ID of recipient</param>
        /// <param name="method">How to deliver the message</param>
        /// <returns></returns>
        public SendResult SendMessageTo(OutgoingMessage msg, long recipient, DeliveryMethod method)
        {
            Connection conn;
            if (UniqueIdLookup.TryGetValue(recipient, out conn))
            {
                // send message to specific connection
                return SendMessage(msg, conn, method);
            }
            return SendResult.Failed;
        }

        /// <summary>
        /// Send a message to all except specific connection
        /// </summary>
        /// <param name="msg">The message to send</param>
        /// <param name="recipient">ID of recipient</param>
        /// <param name="method">How to deliver the message</param>
        public void SendMessageExcept(OutgoingMessage msg, Guid recipient, DeliveryMethod method)
        {
            SendMessageExcept(msg, NetUtility.GuidToLong(recipient), method);
        }

        /// <summary>
        /// Send a message to all except specific connection
        /// </summary>
        /// <param name="msg">The message to send</param>
        /// <param name="recipient">ID of recipient</param>
        /// <param name="method">How to deliver the message</param>
        public void SendMessageExcept(OutgoingMessage msg, long recipient, DeliveryMethod method)
        {
            Connection conn;
            List<Connection> connections = Connections;
            if (UniqueIdLookup.TryGetValue(recipient, out conn))
            {
                if (connections.Contains(conn))
                    connections.Remove(conn);

                if (connections.Count < 1)
                    return;

                SendMessage(msg, connections, method, 0);
            }
        }

        /// <summary>
        /// Send a message to all connections
        /// </summary>
        /// <param name="msg">The message to send</param>
        /// <param name="method">How to deliver the message</param>
        public void SendToAll(OutgoingMessage msg, DeliveryMethod method)
        {
            if (Connections.Count <= 0)
                return;
            SendMessage(msg, this.Connections, method, 0);
        }

        /// <summary>
        /// Send a message to all connections except one
        /// </summary>
        /// <param name="msg">The message to send</param>
        /// <param name="except">Don't send to this particular connection</param>
        /// <param name="method">How to deliver the message</param>
        /// <param name="sequenceChannel">Which sequence channel to use for the message</param>
        public void SendToAll(OutgoingMessage msg, Connection except, DeliveryMethod method, int sequenceChannel)
        {
            var all = this.Connections;
            if (all.Count <= 0)
                return;

            List<Connection> recipients = new List<Connection>(all.Count - 1);
            foreach (var conn in all)
                if (conn != except)
                    recipients.Add(conn);

            if (recipients.Count > 0)
                SendMessage(msg, recipients, method, sequenceChannel);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return "[Network " + ConnectionsCount + " connections]";
        }
    } // public class Network : Peer
} // namespace TridentFramework.RPC.Net.Server
