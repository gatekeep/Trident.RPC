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
using System.Net;

using TridentFramework.RPC.Net.Message;

namespace TridentFramework.RPC.Net.PeerConnection
{
    /// <summary>
    /// Represents a local peer capable of holding zero, one or more connections to remote peers
    /// </summary>
    public partial class Peer
    {
        /*
        ** Methods
        */

        /// <summary>
        /// Emit a discovery signal to all hosts on your subnet
        /// </summary>
        public void DiscoverLocalPeers(int serverPort)
        {
            OutgoingMessage om = CreateMessage(0);
            om.MessageType = MessageType.Discovery;
            unsentUnconnectedMessages.Enqueue(new Tuple<IPEndPoint, OutgoingMessage>(new IPEndPoint(IPAddress.Broadcast, serverPort), om));
        }

        /// <summary>
        /// Emit a discovery signal to a single known host
        /// </summary>
        /// <returns></returns>
        public bool DiscoverKnownPeer(string host, int serverPort)
        {
            IPAddress address = NetUtility.Resolve(host);
            if (address == null)
                return false;
            DiscoverKnownPeer(new IPEndPoint(address, serverPort));
            return true;
        }

        /// <summary>
        /// Emit a discovery signal to a single known host
        /// </summary>
        public void DiscoverKnownPeer(IPEndPoint endpoint)
        {
            OutgoingMessage om = CreateMessage(0);
            om.MessageType = MessageType.Discovery;
            unsentUnconnectedMessages.Enqueue(new Tuple<IPEndPoint, OutgoingMessage>(endpoint, om));
        }

        /// <summary>
        /// Send a discovery response message
        /// </summary>
        public void SendDiscoveryResponse(OutgoingMessage msg, IPEndPoint recipient)
        {
            if (recipient == null)
                throw new ArgumentNullException("recipient");

            if (msg == null)
                msg = CreateMessage(0);
            else if (msg.IsSent)
                throw new NetworkException("Message has already been sent!");

            if (msg.LengthBytes >= Configuration.MaximumTransmissionUnit)
                throw new NetworkException("Cannot send discovery message larger than MTU (currently " + Configuration.MaximumTransmissionUnit + " bytes)");

            msg.MessageType = MessageType.DiscoveryResponse;
            unsentUnconnectedMessages.Enqueue(new Tuple<IPEndPoint, OutgoingMessage>(recipient, msg));
        }
    } // public partial class Peer
} // namespace TridentFramework.RPC.Net.PeerConnection
