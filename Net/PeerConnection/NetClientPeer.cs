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
using System.IO;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;

using TridentFramework.RPC.Net.Channel;
using TridentFramework.RPC.Net.Message;
using TridentFramework.RPC.Net.PeerConnection;

using TridentFramework.RPC.Utility;

namespace TridentFramework.RPC.Net.PeerConnection
{
    /// <summary>
    /// Specialized version of peer used for a "client" connection. It does not accept any incoming connections and
    /// maintains a ServerConnection property
    /// </summary>
    public class NetClientPeer : Peer
    {
        /*
        ** Properties
        */

        /// <summary>
        /// Gets the connection to the server, if any
        /// </summary>
        public Connection ServerConnection
        {
            get
            {
                Connection retval = null;
                if (connections.Count > 0)
                {
                    try
                    {
                        retval = connections[0];
                    }
                    catch
                    {
                        // preempted!
                        return null;
                    }
                }
                return retval;
            }
        }

        /// <summary>
        /// Gets the connection status of the server connection (or ConnectionStatus.Disconnected if no connection)
        /// </summary>
        public ConnectionStatus ConnectionStatus
        {
            get
            {
                var conn = ServerConnection;
                if (conn == null)
                    return ConnectionStatus.Disconnected;
                return conn.Status;
            }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the Network class.
        /// </summary>
        /// <param name="config">Network peer configuration</param>
        public NetClientPeer(PeerConfiguration config)
            : base(config)
        {
            config.AcceptIncomingConnections = false;
        }

        /// <inheritdoc />
        public override Connection Connect(IPEndPoint remoteEndpoint, OutgoingMessage hailMessage)
        {
            lock (connections)
            {
                if (connections.Count > 0)
                {
                    RPCLogger.Trace("Connect attempt failed; Already connected");
                    return null;
                }
            }
            return base.Connect(remoteEndpoint, hailMessage);
        }

        /// <summary>
        /// Disconnect from server
        /// </summary>
        /// <param name="byeMessage">reason for disconnect</param>
        public void Disconnect(string byeMessage)
        {
            Connection serverConnection = ServerConnection;
            if (serverConnection == null)
            {
                lock (handshakes)
                {
                    if (handshakes.Count > 0)
                    {
                        RPCLogger.Trace("Aborting connection attempt");
                        foreach (var hs in handshakes)
                            hs.Value.Disconnect(byeMessage);
                        return;
                    }
                }

                RPCLogger.Trace("Disconnect requested when not connected!");
                return;
            }
            serverConnection.Disconnect(byeMessage);
        }

        /// <summary>
        /// Sends message to server
        /// </summary>
        /// <returns></returns>
        public SendResult SendMessage(OutgoingMessage msg, DeliveryMethod method)
        {
            Connection serverConnection = ServerConnection;
            if (serverConnection == null)
            {
                RPCLogger.Trace("Cannot send message, no server connection!");
                return SendResult.Failed;
            }

            return serverConnection.SendMessage(msg, method, 0);
        }

        /// <summary>
        /// Sends message to server
        /// </summary>
        /// <returns></returns>
        public SendResult SendMessage(OutgoingMessage msg, DeliveryMethod method, int sequenceChannel)
        {
            Connection serverConnection = ServerConnection;
            if (serverConnection == null)
            {
                RPCLogger.Trace("Cannot send message, no server connection!");
                return SendResult.Failed;
            }

            return serverConnection.SendMessage(msg, method, sequenceChannel);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return "[Network " + ServerConnection + "]";
        }
    } // public class Network : Peer
} // namespace TridentFramework.RPC.Net.Client
