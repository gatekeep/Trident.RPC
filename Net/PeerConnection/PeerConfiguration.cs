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
using System.Net;

using TridentFramework.RPC.Net.Encryption;
using TridentFramework.RPC.Net.Message;

namespace TridentFramework.RPC.Net.PeerConnection
{
    /// <summary>
    /// Behaviour of unreliable sends above MTU
    /// </summary>
    public enum UnreliableSizeBehavior
    {
        /// <summary>
        /// Sending an unreliable message will ignore MTU and send everything in a single packet; this is the new default
        /// </summary>
        IgnoreMTU = 0,

        /// <summary>
        /// Old behaviour; use normal fragmentation for unreliable messages - if a fragment is dropped, memory for received fragments are never reclaimed!
        /// </summary>
        NormalFragmentation = 1,

        /// <summary>
        /// Alternate behaviour; just drops unreliable messages above MTU
        /// </summary>
        DropAboveMTU = 2,
    } // public enum UnreliableSizeBehavior

    /// <summary>
    /// Type of compression to use if compression is enabled.
    /// </summary>
    public enum CompressionType
    {
        /// <summary>
        /// zlib Compression
        /// </summary>
        ZLIB = 0,

        /// <summary>
        /// LZMA Compression
        /// </summary>
        LZMA = 1
    } // public enum CompressionProvider

    /// <summary>
    /// Partly immutable after Peer has been initialized
    /// </summary>
    public sealed class PeerConfiguration
    {
        /**
         * Constants
         */
        private const string IS_LOCKED_MESSAGE = "You may not modify the PeerConfiguration after it has been used to initialize a peer";

        public const int RECEIVE_BUFFER_SIZE = 131071;
        public const int SEND_BUFFER_SIZE = 131071;

        /// <summary>
        /// Default MTU in bytes
        /// </summary>
        public const int DEFAULT_MTU = 1408;

        private bool isLocked;
        private string appIdentifier;
        private string networkThreadName;
        private IPAddress localAddress;
        private IPAddress broadcastAddress;
        private int port;
        private int receiveBufferSize;
        private int sendBufferSize;
        private bool acceptIncomingConnections;
        private int maximumConnections;
        private int defaultOutgoingMessageCapacity;
        private float pingInterval;
        private bool useMessageRecycling;
        private int recycledCacheMaxCount;
        private float connectionTimeout;
        private bool enableUPnP;
        private bool autoFlushSendQueue;
        private bool suppressUnreliableUnorderedAcks;

        private IncomingMessageType disabledTypes;
        private float resendHandshakeInterval;
        private int maximumHandshakeAttempts;

        // MTU
        private int maximumTransmissionUnit;

        private bool autoExpandMTU;
        private float expandMTUFrequency;
        private int expandMTUFailAttempts;
        private UnreliableSizeBehavior unreliableSizeBehavior;

        // Compression
        private bool enableCompression;

        private CompressionType compressionType;

        // Encryption
        private bool negotiateEncryption;

        private string encryptionKey;
        private bool enableEncryption;
        private IMessageEncryption encryptionProvider;

        /*
        ** Properties
        */

        /// <summary>
        /// Gets the identifier of this application; the library can only connect to matching app identifier peers
        /// </summary>
        public string AppIdentifier
        {
            get { return appIdentifier; }
        }

        /// <summary>
        /// Gets the name of the library network thread. Cannot be changed once peer is initialized.
        /// </summary>
        public string NetworkThreadName
        {
            get { return networkThreadName; }
            set
            {
                if (isLocked)
                    throw new NetworkException(IS_LOCKED_MESSAGE);
                networkThreadName = value;
            }
        }

        /// <summary>
        /// Gets or sets the local IP address to bind to. Defaults to IPAddress.Any. Cannot be changed once peer is initialized.
        /// </summary>
        public IPAddress LocalAddress
        {
            get { return localAddress; }
            set
            {
                if (isLocked)
                    throw new NetworkException(IS_LOCKED_MESSAGE);
                localAddress = value;
            }
        }

        /// <summary>
        /// Gets or sets the local broadcast address to use when broadcasting
        /// </summary>
        public IPAddress BroadcastAddress
        {
            get { return broadcastAddress; }
            set
            {
                if (isLocked)
                    throw new NetworkException(IS_LOCKED_MESSAGE);
                broadcastAddress = value;
            }
        }

        /// <summary>
        /// Gets the local port to bind to. Defaults to 0. Cannot be changed once peer is initialized.
        /// </summary>
        public int Port
        {
            get { return port; }
        }

        /// <summary>
        /// Gets or sets the size in bytes of the receiving buffer. Defaults to 131071 bytes. Cannot be changed once NetPeer is initialized.
        /// </summary>
        public int ReceiveBufferSize
        {
            get { return receiveBufferSize; }
            set
            {
                if (isLocked)
                    throw new NetworkException(IS_LOCKED_MESSAGE);
                receiveBufferSize = value;
            }
        }

        /// <summary>
        /// Gets or sets the size in bytes of the sending buffer. Defaults to 131071 bytes. Cannot be changed once NetPeer is initialized.
        /// </summary>
        public int SendBufferSize
        {
            get { return sendBufferSize; }
            set
            {
                if (isLocked)
                    throw new NetworkException(IS_LOCKED_MESSAGE);
                sendBufferSize = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum amount of connections this peer can hold. Cannot be changed once peer is initialized.
        /// </summary>
        public int MaximumConnections
        {
            get { return maximumConnections; }
            set
            {
                if (isLocked)
                    throw new NetworkException(IS_LOCKED_MESSAGE);
                maximumConnections = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum amount of bytes to send in a single packet, excluding ip, udp and lidgren headers
        /// </summary>
        public int MaximumTransmissionUnit
        {
            get { return maximumTransmissionUnit; }
            set
            {
                if (isLocked)
                    throw new NetworkException(IS_LOCKED_MESSAGE);
                maximumTransmissionUnit = value;
            }
        }

        /// <summary>
        /// Gets or sets the default capacity in bytes when peer.CreateMessage() is called without argument
        /// </summary>
        public int DefaultOutgoingMessageCapacity
        {
            get { return defaultOutgoingMessageCapacity; }
            set { defaultOutgoingMessageCapacity = value; }
        }

        /// <summary>
        /// Gets or sets the time between latency calculating pings
        /// </summary>
        public float PingInterval
        {
            get { return pingInterval; }
            set { pingInterval = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the library should recycling messages to avoid excessive garbage collection. Cannot be changed once peer is initialized.
        /// </summary>
        public bool UseMessageRecycling
        {
            get { return useMessageRecycling; }
            set
            {
                if (isLocked)
                    throw new NetworkException(IS_LOCKED_MESSAGE);
                useMessageRecycling = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum number of incoming/outgoing messages to keep in the recycle cache.
        /// </summary>
        public int RecycledCacheMaxCount
        {
            get { return recycledCacheMaxCount; }
            set
            {
                if (isLocked)
                    throw new NetworkException(IS_LOCKED_MESSAGE);
                recycledCacheMaxCount = value;
            }
        }

        /// <summary>
        /// Gets or sets the number of seconds timeout will be postponed on a successful ping/pong
        /// </summary>
        public float ConnectionTimeout
        {
            get { return connectionTimeout; }
            set
            {
                if (value < pingInterval)
                    throw new NetworkException("Connection timeout cannot be lower than ping interval!");
                connectionTimeout = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not UPnP support is enabled; enabling port forwarding and getting external ip
        /// </summary>
        public bool EnableUPnP
        {
            get { return enableUPnP; }
            set
            {
                if (isLocked)
                    throw new NetworkException(IS_LOCKED_MESSAGE);
                enableUPnP = value;
            }
        }

        /// <summary>
        /// Enables or disables automatic flushing of the send queue. If disabled, you must manully call NetPeer.FlushSendQueue() to flush sent messages to network.
        /// </summary>
        public bool AutoFlushSendQueue
        {
            get { return autoFlushSendQueue; }
            set { autoFlushSendQueue = value; }
        }

        /// <summary>
        /// If true, will not send acks for unreliable unordered messages. This will save bandwidth, but disable flow control and duplicate detection for this type of messages.
        /// </summary>
        public bool SuppressUnreliableUnorderedAcks
        {
            get { return suppressUnreliableUnorderedAcks; }
            set
            {
                if (isLocked)
                    throw new NetworkException(IS_LOCKED_MESSAGE);
                suppressUnreliableUnorderedAcks = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the peer should accept incoming connections. This is automatically set to true in
        /// NetServer and false in NetClient.
        /// </summary>
        public bool AcceptIncomingConnections
        {
            get { return acceptIncomingConnections; }
            set { acceptIncomingConnections = value; }
        }

        /// <summary>
        /// Gets or sets the number of seconds between handshake attempts
        /// </summary>
        public float ResendHandshakeInterval
        {
            get { return resendHandshakeInterval; }
            set { resendHandshakeInterval = value; }
        }

        /// <summary>
        /// Gets or sets the maximum number of handshake attempts before failing to connect
        /// </summary>
        public int MaximumHandshakeAttempts
        {
            get { return maximumHandshakeAttempts; }
            set
            {
                if (value < 1)
                    throw new NetworkException("MaximumHandshakeAttempts must be at least 1");
                maximumHandshakeAttempts = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the peer should send large messages to try to expand
        /// the maximum transmission unit size
        /// </summary>
        public bool AutoExpandMTU
        {
            get { return autoExpandMTU; }
            set
            {
                if (isLocked)
                    throw new NetworkException(IS_LOCKED_MESSAGE);
                autoExpandMTU = value;
            }
        }

        /// <summary>
        /// Gets or sets how often to send large messages to expand MTU if AutoExpandMTU is enabled
        /// </summary>
        public float ExpandMTUFrequency
        {
            get { return expandMTUFrequency; }
            set { expandMTUFrequency = value; }
        }

        /// <summary>
        /// Gets or sets the number of failed expand mtu attempts to perform before setting final MTU
        /// </summary>
        public int ExpandMTUFailAttempts
        {
            get { return expandMTUFailAttempts; }
            set { expandMTUFailAttempts = value; }
        }

        /// <summary>
        /// Gets or sets the behaviour of unreliable sends above MTU
        /// </summary>
        public UnreliableSizeBehavior UnreliableSizeBehavior
        {
            get { return unreliableSizeBehavior; }
            set { unreliableSizeBehavior = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not stream compression support is enabled.
        /// </summary>
        public bool EnableCompression
        {
            get { return enableCompression; }
            set
            {
                if (isLocked)
                    throw new NetworkException(IS_LOCKED_MESSAGE);
                enableCompression = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating the type of compression to use.
        /// </summary>
        public CompressionType CompressionType
        {
            get { return compressionType; }
            set
            {
                if (isLocked)
                    throw new NetworkException(IS_LOCKED_MESSAGE);
                compressionType = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not stream encryption support is enabled.
        /// </summary>
        public bool EnableEncryption
        {
            get { return enableEncryption; }
            set
            {
                if (EncryptionProvider == null && isLocked)
                    throw new NetworkException(IS_LOCKED_MESSAGE);
                enableEncryption = value;
            }
        }

        /// <summary>
        /// Gets or sets the encryption key used for non-negotiated encryption.
        /// </summary>
        public string EncryptionKey
        {
            get { return encryptionKey; }
            set
            {
                if (isLocked)
                    throw new NetworkException(IS_LOCKED_MESSAGE);
                encryptionKey = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not encryption is negotiated automatically.
        /// </summary>
        public bool NegotiateEncryption
        {
            get { return negotiateEncryption; }
            set
            {
                if (EncryptionProvider == null && isLocked)
                    throw new NetworkException(IS_LOCKED_MESSAGE);
                negotiateEncryption = value;
            }
        }

        /// <summary>
        /// Gets or sets the encryption provider.
        /// </summary>
        public IMessageEncryption EncryptionProvider
        {
            get { return encryptionProvider; }
            set
            {
                if (isLocked)
                    throw new NetworkException(IS_LOCKED_MESSAGE);
                encryptionProvider = value;
            }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the PeerConfiguration class.
        /// </summary>
        public PeerConfiguration(string appIdentifier, int port)
        {
            if (string.IsNullOrEmpty(appIdentifier))
                throw new NetworkException("App identifier must be at least one character long");
            this.appIdentifier = appIdentifier.ToString(System.Globalization.CultureInfo.InvariantCulture);

            //
            // default values
            //
            disabledTypes = IncomingMessageType.ConnectionApproval | IncomingMessageType.UnconnectedData | IncomingMessageType.ConnectionLatencyUpdated;
            networkThreadName = "Peer-NetworkPump";
            localAddress = IPAddress.Any;
            broadcastAddress = IPAddress.Broadcast;
            var ip = NetUtility.GetBroadcastAddress();
            if (ip != null)
                broadcastAddress = ip;
            this.port = port;
            receiveBufferSize = RECEIVE_BUFFER_SIZE;
            sendBufferSize = SEND_BUFFER_SIZE;
            acceptIncomingConnections = false;
            maximumConnections = 32;
            defaultOutgoingMessageCapacity = 16;
            pingInterval = 25.0f; //10.0f; //4.0f;
            connectionTimeout = 45.0f; //30.0f; //25.0f;
            useMessageRecycling = true;
            recycledCacheMaxCount = 64;
            resendHandshakeInterval = 3.0f;
            maximumHandshakeAttempts = 5;
            autoFlushSendQueue = true;
            suppressUnreliableUnorderedAcks = false;

            // Maximum transmission unit
            // Ethernet can take 1500 bytes of payload, so lets stay below that.
            // The aim is for a max full packet to be 1440 bytes (30 x 48 bytes, lower than 1468)
            // -20 bytes IP header
            //  -8 bytes UDP header
            //  -4 bytes to be on the safe side and align to 8-byte boundary
            // Total 1408 bytes
            // Note that lidgren headers (5 bytes) are not included here; since it's part of the "mtu payload"
            maximumTransmissionUnit = 1408;
            autoExpandMTU = false;
            expandMTUFrequency = 2.0f;
            expandMTUFailAttempts = 5;
            unreliableSizeBehavior = UnreliableSizeBehavior.IgnoreMTU;

            isLocked = false;

            enableCompression = false;
            compressionType = CompressionType.ZLIB;

            negotiateEncryption = true;
            encryptionKey = "Not-A-Very-Safe-Key";
            enableEncryption = false;
            encryptionProvider = null;
        }

        /// <summary>
        /// Lock the configuration
        /// </summary>
        internal void Lock()
        {
            isLocked = true;
        }

        /// <summary>
        /// Enables receiving of the specified type of message
        /// </summary>
        public void EnableMessageType(IncomingMessageType type)
        {
            disabledTypes &= ~type;
        }

        /// <summary>
        /// Disables receiving of the specified type of message
        /// </summary>
        public void DisableMessageType(IncomingMessageType type)
        {
            disabledTypes |= type;
        }

        /// <summary>
        /// Enables or disables receiving of the specified type of message
        /// </summary>
        public void SetMessageTypeEnabled(IncomingMessageType type, bool enabled)
        {
            if (enabled)
                disabledTypes &= ~type;
            else
                disabledTypes |= type;
        }

        /// <summary>
        /// Gets if receiving of the specified type of message is enabled
        /// </summary>
        /// <returns></returns>
        public bool IsMessageTypeEnabled(IncomingMessageType type)
        {
            return !((disabledTypes & type) == type);
        }

        /// <summary>
        /// Creates a memberwise shallow clone of this configuration
        /// </summary>
        /// <returns></returns>
        public PeerConfiguration Clone()
        {
            PeerConfiguration retval = this.MemberwiseClone() as PeerConfiguration;
            retval.isLocked = false;
            return retval;
        }
    } // public sealed class PeerConfiguration
} // namespace TridentFramework.RPC.Net.PeerConnection
