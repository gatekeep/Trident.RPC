﻿/**
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
using TridentFramework.RPC.Net.PeerConnection;

namespace TridentFramework.RPC.Net.Channel
{
    /// <summary>
    /// Network receiver channel
    /// </summary>
    public abstract class IReceiverChannel
    {
        public Peer peer;
        public Connection connection;

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="IReceiverChannel"/> class.
        /// </summary>
        /// <param name="connection">Connection channel belongs to</param>
        public IReceiverChannel(Connection connection)
        {
            this.connection = connection;
            this.peer = connection.Peer;
        }

        /// <summary>
        /// Receive incoming message from the network.
        /// </summary>
        /// <param name="msg">Incoming message being received</param>
        public abstract void ReceiveMessage(IncomingMessage msg);
    } // internal abstract class IReceiverChannel
} // namespace TridentFramework.RPC.Net.Channel
