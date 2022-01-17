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

using System;
using System.Threading;

using TridentFramework.RPC.Net.Message;
using TridentFramework.RPC.Net.PeerConnection;

using TridentFramework.RPC.Utility;

namespace TridentFramework.RPC.Net
{
    /// <summary>
    /// Defines the an abstract client handler all clients should derive from this.
    /// </summary>
    public abstract class INetClient : IClientServerBase
    {
        public const int OPEN_TIMEOUT = 30000;      // 30(s)
        public const int DISCONNECT_TIMEOUT = 5000; // 5(s)

        private Thread processMessages;
        private bool haltMessageProcessing;
        private bool isOpened = false;
        private bool isConnected = false;

        private ManualResetEvent connectEvent = new ManualResetEvent(false);
        private ManualResetEvent disconnectEvent = new ManualResetEvent(false);

        private Uri remoteUri;

        /*
        ** Properties
        */

        /// <summary>
        /// Gets the instance of the <see cref="NetClientPeer"/>.
        /// </summary>
        public NetClientPeer Peer
        {
            get { return (NetClientPeer)peer; }
        }

        /// <summary>
        /// Gets the network connection guid (identifier used by server).
        /// </summary>
        public long UniqueId
        {
            get { return connectionId; }
        }

        /// <summary>
        /// Flag indicating whether the network connection has been started.
        /// </summary>
        public bool IsOpened
        {
            get { return isOpened; }
        }

        /// <summary>
        /// Flag indicating whether we are connected to the remote endpoint.
        /// </summary>
        public bool IsConnected
        {
            get { return isConnected; }
        }

        /// <summary>
        /// Gets or sets the remote URI.
        /// </summary>
        public Uri RemoteUri
        {
            get { return remoteUri; }
            set
            {
                if (!isOpened && !isConnected)
                    remoteUri = value;
                else
                    throw new InvalidOperationException("Cannot change URI while connected and/or started");
            }
        }

        /*
        ** Events
        */

        /// <summary>
        /// Occurs when the client is fully connected to the server.
        /// </summary>
        public event EventHandler OnClientConnected;

        /// <summary>
        /// Occurs when the client begins disconnecting from the server.
        /// </summary>
        public event EventHandler OnClientDisconnecting;

        /// <summary>
        /// Occurs when the client is disconnected from the server.
        /// </summary>
        public event EventHandler OnClientDisconnected;

        /// <summary>
        /// Occurs when a new client gets a server discovery response.
        /// </summary>
        public event ClientDiscoveryResponseEventHandler OnDiscoveryResponse;

        /// <summary>
        /// Occurs when a raw byte-array network message is received.
        /// </summary>
        public event Action<object, long, byte[]> OnRawNetworkData;

        /// <summary>
        /// Occurs when a user-defined network message is received.
        /// </summary>
        public event Action<object, long, IncomingMessage> OnUserDefinedNetworkData;

        /// <summary>
        /// Occurs when the client initiates connection to the server.
        /// </summary>
        public event EventHandler OnClientInitiatedConnection;

        /// <summary>
        /// Occurs during ListenMessages loop.
        /// </summary>
        protected event EventHandler OnListenTick;

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="INetClient"/> class.
        /// </summary>
        /// <param name="remoteUri"></param>
        /// <param name="peerConfiguration">Network Peer Configuration</param>
        public INetClient(Uri remoteUri, PeerConfiguration peerConfiguration) : this(peerConfiguration)
        {
            this.remoteUri = remoteUri;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="INetClient"/> class.
        /// </summary>
        /// <param name="peerConfiguration">Network Peer Configuration</param>
        public INetClient(PeerConfiguration peerConfiguration) : base(peerConfiguration)
        {
            this.connectionId = 0L;
            this.peer = new NetClientPeer(peerConfiguration);
        }

        /// <inheritdoc />
        protected override void OnDispose()
        {
            Close();

            // close the network
            if (peer != null)
            {
                peer.Shutdown();
                peer = null;
            }
        }

        /// <summary>
        /// Helper to connect to the remote server.
        /// </summary>
        public void Connect()
        {
            if (!isOpened)
                throw new InvalidOperationException("Network client should be started before trying to connect");

            if (remoteUri.Host == null)
            {
                RPCLogger.WriteError("failed to start client! Uri host is empty");
                return;
            }

            if (!isConnected)
            {
                connectEvent.Reset();
                Peer.Connect(remoteUri.Host, remoteUri.Port);
            }
        }

        /// <summary>
        /// Helper to disconnect from the remote server.
        /// </summary>
        public void Disconnect()
        {
            // if the network isn't null, try to disconnect
            if (Peer != null)
            {
                disconnectEvent.Reset();

                if (Peer.ConnectionStatus != ConnectionStatus.Disconnected)
                {
                    Peer.Disconnect("Shutdown");
                    isConnected = ((Peer.ConnectionStatus == ConnectionStatus.Connected) ||
                        (Peer.ConnectionStatus == ConnectionStatus.ConnectedSecured)) ? true : false;
                }
                Peer.Shutdown();
            }
        }

        /// <inheritdoc />
        public override void Open(string threadName = "")
        {
            if (isOpened)
                throw new InvalidOperationException("Network client is already opened");
            RPCLogger.Trace("opening communication");

            try
            {
                // instantiate network client and connect
                Peer.Start();
                isOpened = true;

                Connect();
                this.connectionId = this.peer.UniqueId;

                if (processMessages == null)
                {
                    processMessages = new Thread(ListenMessages);
                    processMessages.Name = "INetClient-ListenMessages" + ((threadName != string.Empty) ? "-" + threadName : string.Empty);
                    processMessages.IsBackground = true;
                    haltMessageProcessing = false;
                    processMessages.Start();
                }

                // wait until we are fully connected
                connectEvent.WaitOne(OPEN_TIMEOUT);

                isConnected = ((Peer.ConnectionStatus == ConnectionStatus.Connected) ||
                    (Peer.ConnectionStatus == ConnectionStatus.ConnectedSecured)) ? true : false;
            }
            catch (Exception ioex)
            {
                // Output debug info if required
                RPCLogger.StackTrace(ioex, false);

                return;
            }
        }

        /// <inheritdoc />
        public override void Close()
        {
            RPCLogger.Trace("closing communication");
            Disconnect();

            disconnectEvent.WaitOne(DISCONNECT_TIMEOUT);

            try
            {
                if (processMessages != null)
                {
                    while (processMessages.IsAlive)
                    {
                        haltMessageProcessing = true;
                        processMessages.Join();
                        processMessages.Abort();
                    }
                    processMessages = null;
                }
            }
            catch (Exception) { }

            isOpened = false;
        }

        /// <summary>
        /// Internal helper to notify the server we're connected and ready.
        /// </summary>
        private void HandleServerConnectNotify()
        {
            // prepare a message to send the current local client to the server
            OutgoingMessage outMsg = Peer.CreateMessage();
            outMsg.MessageType = MessageType.UserReliableOrdered1;
            outMsg.Write(MessageToTransmit.CLIENT_CONNECTED);

            // pack the connection GUID
            outMsg.Write(UniqueId);

            // transmit message
            Peer.SendMessage(outMsg, DeliveryMethod.ReliableOrdered);
            if (!Peer.Configuration.AutoFlushSendQueue)
                Peer.FlushSendQueue();
        }

        /// <summary>
        /// Internal function to listen for client-side raw network messages.
        /// </summary>
        private void ListenMessages()
        {
            do
            {
                IncomingMessage msg;
                while ((msg = Peer.WaitMessage(1)) != null)
                {
                    switch (msg.MessageType)
                    {
                        // Handle network status changed
                        case IncomingMessageType.StatusChanged:
                            {
                                ConnectionStatus status = (ConnectionStatus)msg.ReadByte();
                                switch (status)
                                {
                                    // Handle: Initiated Connection
                                    case ConnectionStatus.InitiatedConnect:
                                        if (OnClientInitiatedConnection != null)
                                            OnClientInitiatedConnection(this, new EventArgs());
                                        break;

                                    // Handle: Connection established
                                    case ConnectionStatus.Connected:
                                        {
                                            RPCLogger.Trace("handling client connection #" + UniqueId.ToString());
                                            connectEvent.Set();

                                            if (Peer.Configuration.EnableEncryption && !Peer.Configuration.NegotiateEncryption)
                                                HandleServerConnectNotify();
                                            break;
                                        }

                                    // Handle: Secured
                                    case ConnectionStatus.ConnectedSecured:
                                        {
                                            if (Peer.Configuration.EnableEncryption && Peer.Configuration.NegotiateEncryption)
                                                HandleServerConnectNotify();
                                            break;
                                        }

                                    // Handle: Disconnecting or Disconnected
                                    case ConnectionStatus.Disconnecting:
                                        {
                                            RPCLogger.Trace("handling client disconnecting #" + UniqueId.ToString());

                                            // prepare a message to send the current local client to the server
                                            OutgoingMessage outMsg = Peer.CreateMessage();
                                            outMsg.MessageType = MessageType.UserReliableOrdered1;
                                            outMsg.Write(MessageToTransmit.CLIENT_DISCONNECTED);

                                            // pack the connection GUID
                                            outMsg.Write(UniqueId);

                                            // transmit message
                                            Peer.SendMessage(outMsg, DeliveryMethod.ReliableOrdered);
                                            if (!Peer.Configuration.AutoFlushSendQueue)
                                                Peer.FlushSendQueue();

                                            if (OnClientDisconnecting != null)
                                                OnClientDisconnecting(this, new EventArgs());

                                            break;
                                        }
                                    case ConnectionStatus.Disconnected:
                                        {
                                            disconnectEvent.Set();

                                            if (OnClientDisconnected != null)
                                                OnClientDisconnected(this, new EventArgs());
                                            break;
                                        }

                                    default:
                                        {
                                            RPCLogger.Trace("unhandled net status change: " + status + " " + msg.LengthBytes + " bytes");
                                            break;
                                        }
                                }
                                break;
                            }

                        // Handle incoming message data
                        case IncomingMessageType.Data:
                            {
                                MessageToTransmit mtt = msg.ReadMessageToTransmit();

                                // get the connection ID
                                long connId = 0L;
                                try
                                {
                                    connId = msg.ReadInt64();
                                }
                                catch (Exception)
                                {
                                    // stub
                                }

                                // handle message to transmit type
                                switch (mtt)
                                {
                                    case MessageToTransmit.CLIENT_CONNECTED:
                                        {
                                            if (OnClientConnected != null)
                                                OnClientConnected(this, new EventArgs());
                                            break;
                                        }

                                    case MessageToTransmit.RAW_BYTES:
                                        {
                                            byte[] raw = ProcessRawBytes(msg);
                                            if (OnRawNetworkData != null)
                                                OnRawNetworkData(this, connId, raw);
                                            break;
                                        }

                                    case MessageToTransmit.USER_DEFINED:
                                        {
                                            if (OnUserDefinedNetworkData != null)
                                                OnUserDefinedNetworkData(this, connId, msg);
                                            break;
                                        }

                                    default:
                                        break;
                                }
                                break;
                            }

                        // Handle: Network session discovery response
                        case IncomingMessageType.DiscoveryResponse:
                            {
                                int currentClientCount = msg.ReadVariableInt32();
                                string hostName = msg.SenderEndpoint.Address.ToString();
                                int openPrivateSlots = msg.ReadVariableInt32();
                                int openPublicSlots = msg.ReadVariableInt32();
                                var averageRoundtripTime = new TimeSpan(0, 0, 0, 0, (int)(msg.SenderConnection.AverageRoundTripTime * 1000));

                                if (OnDiscoveryResponse != null)
                                    OnDiscoveryResponse(this, new ClientDiscoveryResponseEventArgs()
                                    {
                                        CurrentClientCount = currentClientCount,
                                        Hostname = hostName,
                                        OpenPrivateSlots = openPrivateSlots,
                                        OpenPublicSlots = openPublicSlots,
                                        AverageRoundTripTime = averageRoundtripTime
                                    });
                                break;
                            }

                        case IncomingMessageType.TestMessage:
                            RPCLogger.Trace("network test message received");
                            break;

                        default:
                            {
                                RPCLogger.Trace("unhandled type: " + msg.MessageType + " " + msg.LengthBytes + " bytes");
                                break;
                            }
                    }
                }

                if (OnListenTick != null)
                    OnListenTick(this, new EventArgs());
            } while (!haltMessageProcessing);
        }
    } // public abstract class INetClient : IClientServerBase

    /// <summary>
    /// Occurs when the client receives a discovery response.
    /// </summary>
    public delegate void ClientDiscoveryResponseEventHandler(object sender, ClientDiscoveryResponseEventArgs e);

    /// <summary>
    /// Client receives a discovery response event arguments.
    /// </summary>
    public class ClientDiscoveryResponseEventArgs : EventArgs
    {
        /*
        ** Properties
        */

        /// <summary>
        /// Gets or sets the current count of clients connected.
        /// </summary>
        public int CurrentClientCount
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the hostname of the discovered server.
        /// </summary>
        public string Hostname
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the number of open private slots on the discovered server.
        /// </summary>
        public int OpenPrivateSlots
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the number of open public slots on the discovered server.
        /// </summary>
        public int OpenPublicSlots
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public TimeSpan AverageRoundTripTime
        {
            get;
            set;
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientDiscoveryResponseEventArgs"/> class.
        /// </summary>
        public ClientDiscoveryResponseEventArgs()
        {
            /* stub */
        }
    } // public class ServerClientDisconnectedEventArgs : INetworkEventArgs
} // namespace namespace TridentFramework.RPC.Net
