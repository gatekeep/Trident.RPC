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

namespace TridentFramework.RPC.Net.Message
{
    /// <summary>
    /// The type of a IncomingMessage
    /// </summary>
    public enum IncomingMessageType
    {
        /// <summary>
        /// Error; this value should never appear
        /// </summary>
        Error = 0,

        /// <summary>
        /// Data (string); status for a connection changed
        /// </summary>
        StatusChanged = 1 << 0,

        /// <summary>
        /// Data; sent using SendUnconnectedMessage
        /// </summary>
        UnconnectedData = 1 << 1,

        /// <summary>
        /// Data; connection approval is needed
        /// </summary>
        ConnectionApproval = 1 << 2,

        /// <summary>
        /// Data; application data
        /// </summary>
        Data = 1 << 3,

        /// <summary>
        /// Data; receipt of delivery
        /// </summary>
        Receipt = 1 << 4,

        /// <summary>
        /// (no data); discovery request for a response
        /// </summary>
        DiscoveryRequest = 1 << 5,

        /// <summary>
        /// Data; discovery response to a request
        /// </summary>
        DiscoveryResponse = 1 << 6,

        /// <summary>
        /// Data (string); verbose debug message
        /// </summary>
        TestMessage = 1 << 7,

        /// <summary>
        /// Data (as passed to master server); NAT introduction was successful
        /// </summary>
        NatIntroductionSuccess = 1 << 11,

        /// <summary>
        /// Seconds as a float; roundtrip was measured and AverageRoundtripTime was updated
        /// </summary>
        ConnectionLatencyUpdated = 1 << 12,
    } // public enum IncomingMessageType

    /// <summary>
    /// Network message types.
    /// </summary>
    public enum MessageType : byte
    {
        /// <summary>
        /// Unconnected Message
        /// </summary>
        Unconnected = 0,

        /// <summary>
        /// User Unreliable Message
        /// </summary>
        UserUnreliable = 1,

        /// <summary>
        /// User Sequenced Message (Seq 1)
        /// </summary>
        UserSequenced1 = 2,

        /// <summary>
        /// User Sequenced Message (Seq 2)
        /// </summary>
        UserSequenced2 = 3,

        /// <summary>
        /// User Sequenced Message (Seq 3)
        /// </summary>
        UserSequenced3 = 4,

        /// <summary>
        /// User Sequenced Message (Seq 4)
        /// </summary>
        UserSequenced4 = 5,

        /// <summary>
        /// User Sequenced Message (Seq 5)
        /// </summary>
        UserSequenced5 = 6,

        /// <summary>
        /// User Sequenced Message (Seq 6)
        /// </summary>
        UserSequenced6 = 7,

        /// <summary>
        /// User Sequenced Message (Seq 7)
        /// </summary>
        UserSequenced7 = 8,

        /// <summary>
        /// User Sequenced Message (Seq 8)
        /// </summary>
        UserSequenced8 = 9,

        /// <summary>
        /// User Sequenced Message (Seq 9)
        /// </summary>
        UserSequenced9 = 10,

        /// <summary>
        /// User Sequenced Message (Seq 10)
        /// </summary>
        UserSequenced10 = 11,

        /// <summary>
        /// User Sequenced Message (Seq 11)
        /// </summary>
        UserSequenced11 = 12,

        /// <summary>
        /// User Sequenced Message (Seq 12)
        /// </summary>
        UserSequenced12 = 13,

        /// <summary>
        /// User Sequenced Message (Seq 13)
        /// </summary>
        UserSequenced13 = 14,

        /// <summary>
        /// User Sequenced Message (Seq 14)
        /// </summary>
        UserSequenced14 = 15,

        /// <summary>
        /// User Sequenced Message (Seq 15)
        /// </summary>
        UserSequenced15 = 16,

        /// <summary>
        /// User Sequenced Message (Seq 16)
        /// </summary>
        UserSequenced16 = 17,

        /// <summary>
        /// User Reliable Ordered Message
        /// </summary>
        UserReliableUnordered = 34,

        /// <summary>
        /// User Reliable Sequenced Message (Seq 1)
        /// </summary>
        UserReliableSequenced1 = 35,

        /// <summary>
        /// User Reliable Sequenced Message (Seq 2)
        /// </summary>
        UserReliableSequenced2 = 36,

        /// <summary>
        /// User Reliable Sequenced Message (Seq 3)
        /// </summary>
        UserReliableSequenced3 = 37,

        /// <summary>
        /// User Reliable Sequenced Message (Seq 4)
        /// </summary>
        UserReliableSequenced4 = 38,

        /// <summary>
        /// User Reliable Sequenced Message (Seq 5)
        /// </summary>
        UserReliableSequenced5 = 39,

        /// <summary>
        /// User Reliable Sequenced Message (Seq 6)
        /// </summary>
        UserReliableSequenced6 = 40,

        /// <summary>
        /// User Reliable Sequenced Message (Seq 7)
        /// </summary>
        UserReliableSequenced7 = 41,

        /// <summary>
        /// User Reliable Sequenced Message (Seq 8)
        /// </summary>
        UserReliableSequenced8 = 42,

        /// <summary>
        /// User Reliable Sequenced Message (Seq 9)
        /// </summary>
        UserReliableSequenced9 = 43,

        /// <summary>
        /// User Reliable Sequenced Message (Seq 10)
        /// </summary>
        UserReliableSequenced10 = 44,

        /// <summary>
        /// User Reliable Sequenced Message (Seq 11)
        /// </summary>
        UserReliableSequenced11 = 45,

        /// <summary>
        /// User Reliable Sequenced Message (Seq 12)
        /// </summary>
        UserReliableSequenced12 = 46,

        /// <summary>
        /// User Reliable Sequenced Message (Seq 13)
        /// </summary>
        UserReliableSequenced13 = 47,

        /// <summary>
        /// User Reliable Sequenced Message (Seq 14)
        /// </summary>
        UserReliableSequenced14 = 48,

        /// <summary>
        /// User Reliable Sequenced Message (Seq 15)
        /// </summary>
        UserReliableSequenced15 = 49,

        /// <summary>
        /// User Reliable Sequenced Message (Seq 16)
        /// </summary>
        UserReliableSequenced16 = 50,

        /// <summary>
        /// User Reliable Ordered Message (Seq 1)
        /// </summary>
        UserReliableOrdered1 = 67,

        /// <summary>
        /// User Reliable Ordered Message (Seq 2)
        /// </summary>
        UserReliableOrdered2 = 68,

        /// <summary>
        /// User Reliable Ordered Message (Seq 3)
        /// </summary>
        UserReliableOrdered3 = 69,

        /// <summary>
        /// User Reliable Ordered Message (Seq 4)
        /// </summary>
        UserReliableOrdered4 = 70,

        /// <summary>
        /// User Reliable Ordered Message (Seq 5)
        /// </summary>
        UserReliableOrdered5 = 71,

        /// <summary>
        /// User Reliable Ordered Message (Seq 6)
        /// </summary>
        UserReliableOrdered6 = 72,

        /// <summary>
        /// User Reliable Ordered Message (Seq 7)
        /// </summary>
        UserReliableOrdered7 = 73,

        /// <summary>
        /// User Reliable Ordered Message (Seq 8)
        /// </summary>
        UserReliableOrdered8 = 74,

        /// <summary>
        /// User Reliable Ordered Message (Seq 9)
        /// </summary>
        UserReliableOrdered9 = 75,

        /// <summary>
        /// User Reliable Ordered Message (Seq 10)
        /// </summary>
        UserReliableOrdered10 = 76,

        /// <summary>
        /// User Reliable Ordered Message (Seq 11)
        /// </summary>
        UserReliableOrdered11 = 77,

        /// <summary>
        /// User Reliable Ordered Message (Seq 12)
        /// </summary>
        UserReliableOrdered12 = 78,

        /// <summary>
        /// User Reliable Ordered Message (Seq 13)
        /// </summary>
        UserReliableOrdered13 = 79,

        /// <summary>
        /// User Reliable Ordered Message (Seq 14)
        /// </summary>
        UserReliableOrdered14 = 80,

        /// <summary>
        /// User Reliable Ordered Message (Seq 15)
        /// </summary>
        UserReliableOrdered15 = 81,

        /// <summary>
        /// User Reliable Ordered Message (Seq 16)
        /// </summary>
        UserReliableOrdered16 = 82,

        /// <summary>
        /// Unused message
        /// </summary>
        Unused = 90,

        /// <summary>
        /// Internal Error Message
        /// </summary>
        InternalError = 128,

        /// <summary>
        /// Ping Message
        /// </summary>
        Ping = 129, // used for RTT calculation

        /// <summary>
        /// Pong Message
        /// </summary>
        Pong = 130, // used for RTT calculation

        /// <summary>
        /// Connect Message
        /// </summary>
        Connect = 131,

        /// <summary>
        /// Connection Response Message
        /// </summary>
        ConnectResponse = 132,

        /// <summary>
        /// Connection Established Message
        /// </summary>
        ConnectionEstablished = 133,

        /// <summary>
        /// Acknowledgement Message
        /// </summary>
        Acknowledge = 134,

        /// <summary>
        /// Disconnect Message
        /// </summary>
        Disconnect = 135,

        /// <summary>
        /// Discovery Message
        /// </summary>
        Discovery = 136,

        /// <summary>
        /// Discovery Response Message
        /// </summary>
        DiscoveryResponse = 137,

        /// <summary>
        /// NAT Punch-Through Message
        /// </summary>
        NatPunchMessage = 138, // send between peers

        /// <summary>
        /// NAT Introduction Message
        /// </summary>
        NatIntroduction = 139, // send to master server

        /// <summary>
        /// Expand MTU Request Message
        /// </summary>
        ExpandMTURequest = 140,

        /// <summary>
        /// Expand MTU Successful Message
        /// </summary>
        ExpandMTUSuccess = 141,

        /// <summary>
        /// Diffie-Hellman Exchange Request Message
        /// </summary>
        DiffieHellmanRequest = 142,

        /// <summary>
        /// Diffie-Hellman Exchange Response Message
        /// </summary>
        DiffieHellmanResponse = 143,
    } // internal enum MessageType : byte
} // namespace TridentFramework.RPC.Net.Message
