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
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Text;

using TridentFramework.RPC.Net.Message;
using TridentFramework.RPC.Net.Encryption;

using TridentFramework.Cryptography.DiffieHellman;
using TridentFramework.Cryptography.DiffieHellman.Generators;
using TridentFramework.Cryptography.DiffieHellman.Parameters;
using TridentFramework.RPC.Utility;

namespace TridentFramework.RPC.Net.PeerConnection
{
    /// <summary>
    /// Occurs when data received from the peer.
    /// </summary>
    public delegate void PeerReceivedEventHandler(object sender, EventArgs e);

    /// <summary>
    /// Represents a local peer capable of holding zero, one or more connections to remote peers
    /// </summary>
    public partial class Peer
    {
        public const int DH_BIT_SIZE = 512;         // techincally -- this is a weak bit size; but in the name of speed I'm using it
        public const int DH_PRIME_PROBABILITY = 30; // don't change this...

        private static int initializedPeersCount;

        internal readonly List<Connection> connections;
        private readonly Dictionary<IPEndPoint, Connection> connectionLookup;
        private readonly Dictionary<long, Connection> uniqueIdLookup;

        private long uniqueId;
        private string shutdownReason;

        private static DHParameters dhParams;
        private AsymmetricCipherKeyPair dhKP;

        /*
        ** Events
        */

        /// <summary>
        /// Occurs when a new client is accepted.
        /// </summary>
        public event Action<object, long> OnClientAccepted;

        /// <summary>
        /// Occurs when a client disconnects.
        /// </summary>
        public event Action<object, long> OnClientDisconnected;

        /*
        ** Properties
        */

        /// <summary>
        /// Gets the status of the peer
        /// </summary>
        public PeerStatus Status
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the signaling event which can be waited on to determine when a message is queued for reading.
        /// Note that there is no guarantee that after the event is signaled the blocked thread will
        /// find the message in the queue. Other user created threads could be preempted and dequeue
        /// the message before the waiting thread wakes up.
        /// </summary>
        public AutoResetEvent MessageReceivedEvent
        {
            get
            {
                if (messageReceivedEvent == null)
                {
                    lock (messageReceivedEventCreationLock) // make sure we don't create more than one event object
                    {
                        if (messageReceivedEvent == null)
                            messageReceivedEvent = new AutoResetEvent(false);
                    }
                }
                return messageReceivedEvent;
            }
        }

        /// <summary>
        /// Gets a unique identifier for this peer based on MAC address and IP/Port. Note! Not available until Start() has been called!
        /// </summary>
        public long UniqueId
        {
            get { return uniqueId; }
        }

        /// <summary>
        /// Gets a unique identifier for this peer based on MAC address and IP/Port. Note! Not available until Start() has been called!
        /// </summary>
        public Guid Guid
        {
            get { return NetUtility.LongToGuid(uniqueId); }
        }

        /// <summary>
        /// Gets the port number this peer is listening and sending on, if Start() has been called
        /// </summary>
        public int Port
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets an UPnP object if enabled in the NetPeerConfiguration
        /// </summary>
        public UPnP UPnP
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the application defined object containing data about the peer
        /// </summary>
        public object Tag
        {
            get;
            set;
        }

        /// <summary>
        /// Gets a copy of the list of connections
        /// </summary>
        public List<Connection> Connections
        {
            get
            {
                lock (connections)
                    return new List<Connection>(connections);
            }
        }

        /// <summary>
        /// Gets a copy of the guid lookup dictionary.
        /// </summary>
        public Dictionary<long, Connection> UniqueIdLookup
        {
            get
            {
                lock (uniqueIdLookup)
                    return new Dictionary<long, Connection>(uniqueIdLookup);
            }
        }

        /// <summary>
        /// Gets the number of active connections
        /// </summary>
        public int ConnectionsCount
        {
            get { return connections.Count; }
        }

        /// <summary>
        /// Gets the statistics on this Peer since it was initialized
        /// </summary>
        public PeerStatistics Statistics
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the configuration used to instantiate this Peer
        /// </summary>
        public PeerConfiguration Configuration
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the generated Diffie-Hellman parameters.
        /// </summary>
        public static DHParameters DiffieHellmanParameters
        {
            get { return dhParams; }
        }

        /// <summary>
        /// Gets the generated asymmetric cipher keypair.
        /// </summary>
        public AsymmetricCipherKeyPair CipherKeyPair
        {
            get { return dhKP; }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Static initializer for the <see cref="Peer"/> class.
        /// </summary>
        static Peer()
        {
            // generate diffie-hellman parameters
            DHParametersGenerator generator = new DHParametersGenerator();
            generator.Init(DH_BIT_SIZE, DH_PRIME_PROBABILITY, new SecureRandom());

            RPCLogger.Trace("generating DH [" + DH_BIT_SIZE + "] parameters...");
            dhParams = generator.GenerateParameters();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Peer"/> class.
        /// </summary>
        /// <param name="config">Peer configuration</param>
        public Peer(PeerConfiguration config)
        {
            Configuration = config;
            Statistics = new PeerStatistics(this);

            releasedIncomingMessages = new ThreadSafeQueue<IncomingMessage>(4);
            unsentUnconnectedMessages = new ThreadSafeQueue<Tuple<IPEndPoint, OutgoingMessage>>(2);

            connections = new List<Connection>();
            connectionLookup = new Dictionary<IPEndPoint, Connection>();
            uniqueIdLookup = new Dictionary<long, Connection>();

            uniqueId = 0;

            handshakes = new Dictionary<IPEndPoint, Connection>();

            senderRemote = (EndPoint)new IPEndPoint(IPAddress.Any, 0);

            Status = PeerStatus.NotRunning;

            receivedFragmentGroups = new Dictionary<Connection, Dictionary<int, ReceivedFragmentGroup>>();
        }

        /// <summary>
        /// Generate a asymmetric cipher keypair from the given Diffie-Hellman parameters.
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static AsymmetricCipherKeyPair GenerateKeys(DHParameters parameters)
        {
            IAsymmetricCipherKeyPairGenerator keyGen = new DHKeyPairGenerator();
            DHKeyGenerationParameters kgp = new DHKeyGenerationParameters(new SecureRandom(), parameters);
            keyGen.Init(kgp);

            return keyGen.GenerateKeyPair();
        }

        /// <summary>
        /// Return the public key bytes from the given asymmetric cipher pair.
        /// </summary>
        /// <param name="cipher"></param>
        /// <returns></returns>
        public static byte[] GetPublicKey(AsymmetricCipherKeyPair cipher)
        {
            DHPublicKeyParameters dhPubK = cipher.Public as DHPublicKeyParameters;
            if (dhPubK != null)
                return dhPubK.Y.ToByteArray();
            return null;
        }

        /// <summary>
        /// Binds to socket and spawns the networking thread
        /// </summary>
        public void Start()
        {
            if (Status != PeerStatus.NotRunning)
            {
                // already running! Just ignore...
                RPCLogger.Trace("Start() called on already running peer - ignoring.");
                return;
            }

            Status = PeerStatus.Starting;

            // generate keypair
            dhKP = GenerateKeys(dhParams);

            // fix network thread name
            if (Configuration.NetworkThreadName == "Peer-NetworkPump")
            {
                int pc = Interlocked.Increment(ref initializedPeersCount);
                Configuration.NetworkThreadName = "Peer-NetworkPump-" + pc.ToString();
            }

            InitializeNetwork();

            // start network thread
            networkThread = new Thread(new ThreadStart(NetworkLoop));
            networkThread.Name = Configuration.NetworkThreadName;
            networkThread.IsBackground = true;
            networkThread.Start();

            // send upnp discovery
            if (UPnP != null)
                UPnP.Discover(this);

            // allow some time for network thread to start up in case they call Connect() or UPnP calls immediately
            Thread.Sleep(50);
        }

        /// <summary>
        /// Get the connection, if any, for a certain remote endpoint
        /// </summary>
        /// <returns></returns>
        public Connection GetConnection(IPEndPoint ep)
        {
            Connection retval;
            connectionLookup.TryGetValue(ep, out retval); // this should not pose a threading problem, afaict
            return retval;
        }

        /// <summary>
        /// Get the connection, if any, for a certain unique ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Connection GetConnection(Guid id)
        {
            return GetConnection(NetUtility.GuidToLong(id));
        }

        /// <summary>
        /// Get the connection, if any, for a certain unique ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Connection GetConnection(long id)
        {
            Connection retval;
            uniqueIdLookup.TryGetValue(id, out retval); // this should not pose a threading problem, afaict
            return retval;
        }

        /// <summary>
        /// Read a pending message from any connection, blocking up to maxMillis if needed
        /// </summary>
        /// <returns></returns>
        public IncomingMessage WaitMessage(int maxMillis)
        {
            IncomingMessage msg = ReadMessage();
            while (msg == null)
            {
                // this could return true...
                if (!MessageReceivedEvent.WaitOne(maxMillis))
                    return null;

                // ... while this will still returns null. That's why we need to cycle.
                msg = ReadMessage();
            }

            return msg;
        }

        /// <summary>
        /// Helper to decrypt the given message.
        /// </summary>
        /// <param name="msg">The message to receive</param>
        public void DecryptMessage(IncomingMessage msg)
        {
            byte messageType = (byte)msg.MessageType;
            if ((messageType > 0) && (messageType < 90))
            {
                if (Configuration.EnableEncryption)
                {
                    IMessageEncryption msgEnc = Configuration.EncryptionProvider;
                    byte[] key = null;

                    if (Configuration.NegotiateEncryption && msg.SenderConnection.AgreementKey != null)
                    {
                        RPCLogger.Trace("Decrypting Message " + msg.ToString() + " in negotiated mode");

                        // in negotiated encryption -- use the DH derived key for encryption
                        key = msg.SenderConnection.AgreementKey.ToByteArray();
                        msgEnc.SetKey(key, 0, key.Length);

                        msg.Decrypt(msgEnc);
                        return;
                    }

                    RPCLogger.Trace("Decrypting Message " + msg.ToString() + " in key mode");

                    // in key encryption -- use the configured static key for encryption
                    key = Encoding.ASCII.GetBytes(Configuration.EncryptionKey);
                    msgEnc.SetKey(key, 0, key.Length);

                    msg.Decrypt(msgEnc);
                }
            }
        }

        /// <summary>
        /// Helper to decompress the given message.
        /// </summary>
        /// <param name="msg"></param>
        public void DecompressMessage(IncomingMessage msg)
        {
            if (Configuration.EnableCompression)
            {
                switch (Configuration.CompressionType)
                {
                    case CompressionType.ZLIB:
                        msg.DecompressZlib();
                        break;

                    case CompressionType.LZMA:
                        msg.DecompressLzma();
                        break;

                    default:
                        throw new InvalidOperationException("unsupported compression type while trying to compress message");
                }
            }
        }

        /// <summary>
        /// Read a pending message from any connection, if any
        /// </summary>
        /// <returns></returns>
        public IncomingMessage ReadMessage()
        {
            IncomingMessage retval;
            if (releasedIncomingMessages.TryDequeue(out retval))
            {
                if (retval.MessageType == IncomingMessageType.StatusChanged)
                {
                    ConnectionStatus status = (ConnectionStatus)retval.PeekByte();
                    retval.SenderConnection.Status = status;
                }
                else if (retval.MessageType == IncomingMessageType.Data)
                {
                    DecompressMessage(retval);
                    DecryptMessage(retval);
                }
            }
            return retval;
        }

        /// <summary>
        /// Reads a pending message from any connection, if any.
        /// Returns true if message was read, otherwise false.
        /// </summary>
        /// <returns>True, if message was read.</returns>
        public bool ReadMessage(out IncomingMessage message)
        {
            message = ReadMessage();
            return message != null;
        }

        /// <summary>
        /// Read a pending message from any connection, if any
        /// </summary>
        public int ReadMessages(IList<IncomingMessage> addTo)
        {
            int added = releasedIncomingMessages.TryDrain(addTo);
            if (added > 0)
            {
                for (int i = 0; i < added; i++)
                {
                    var index = addTo.Count - added + i;
                    var nim = addTo[index];
                    if (nim.MessageType == IncomingMessageType.StatusChanged)
                    {
                        ConnectionStatus status = (ConnectionStatus)nim.PeekByte();
                        nim.SenderConnection.Status = status;
                    }
                }
            }
            return added;
        }

        /// <summary>
        /// Send internal message immediately.
        /// </summary>
        /// <param name="msg">Message to send</param>
        /// <param name="recipient">End point</param>
        internal void SendInternal(OutgoingMessage msg, IPEndPoint recipient)
        {
            VerifyNetworkThread();
            NetworkException.Assert(msg.IsSent == false);

            bool connReset;
            int len = msg.Encode(sendBuffer, 0, 0);
            SendPacket(len, recipient, 1, out connReset);

            // no reliability, no multiple recipients - we can just recycle this message immediately
            msg.recyclingCount = 0;
            Recycle(msg);
        }

        /// <summary>
        /// Create a connection to a remote endpoint
        /// </summary>
        /// <param name="host">Host to connect to</param>
        /// <param name="port">Port to connect to</param>
        /// <returns></returns>
        public Connection Connect(string host, int port)
        {
            return Connect(new IPEndPoint(NetUtility.Resolve(host), port), null);
        }

        /// <summary>
        /// Create a connection to a remote endpoint
        /// </summary>
        /// <param name="host">Host to connect to</param>
        /// <param name="port">Port to connect to</param>
        /// <param name="hailMessage">Message to send to server</param>
        /// <returns></returns>
        public Connection Connect(string host, int port, OutgoingMessage hailMessage)
        {
            return Connect(new IPEndPoint(NetUtility.Resolve(host), port), hailMessage);
        }

        /// <summary>
        /// Create a connection to a remote endpoint
        /// </summary>
        public Connection Connect(IPEndPoint remoteEndPoint)
        {
            return Connect(remoteEndPoint, null);
        }

        /// <summary>
        /// Create a connection to a remote endpoint
        /// </summary>
        /// <returns></returns>
        public virtual Connection Connect(IPEndPoint remoteEndpoint, OutgoingMessage hailMessage)
        {
            if (remoteEndpoint == null)
                throw new ArgumentNullException("remoteEndpoint");

            lock (connections)
            {
                if (Status == PeerStatus.NotRunning)
                    throw new NetworkException("Must call Start() first");

                if (connectionLookup.ContainsKey(remoteEndpoint))
                    throw new NetworkException("Already connected to that endpoint!");

                Connection hs;
                if (handshakes.TryGetValue(remoteEndpoint, out hs))
                {
                    // already trying to connect to that endpoint; make another try
                    switch (hs.Status)
                    {
                        case ConnectionStatus.InitiatedConnect:
                            // send another connect
                            hs.ConnectRequested = true;
                            break;

                        case ConnectionStatus.RespondedConnect:
                            // send another response
                            hs.SendConnectResponse((float)NetTime.Now, false);
                            break;

                        default:
                            // weird
                            RPCLogger.Trace("Weird situation; Connect() already in progress to remote endpoint; but hs status is " + hs.Status);
                            break;
                    }
                    return hs;
                }

                Connection conn = new Connection(this, remoteEndpoint);
                conn.Status = ConnectionStatus.InitiatedConnect;
                conn.LocalHailMessage = hailMessage;

                // handle on network thread
                conn.ConnectRequested = true;
                conn.ConnectionInitiator = true;

                handshakes.Add(remoteEndpoint, conn);

                return conn;
            }
        }

        /// <summary>
        /// Send raw bytes; only used for debugging
        /// </summary>
        internal void RawSend(byte[] arr, int offset, int length, IPEndPoint destination)
        {
            // wrong thread - this miiiight crash with network thread... but what's a boy to do.
            Array.Copy(arr, offset, sendBuffer, 0, length);
            bool unused;
            SendPacket(length, destination, 1, out unused);
        }

        /// <summary>
        /// Disconnects all active connections and closes the socket
        /// </summary>
        /// <param name="bye"></param>
        public void Shutdown(string bye = "")
        {
            // called on user thread
            if (Socket == null)
                return; // already shut down

            shutdownReason = bye;
            Status = PeerStatus.ShutdownRequested;
        }
    } // public partial class Peer
} // namespace TridentFramework.RPC.Net.PeerConnection
