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
//
// Based on code from the C# WebServer project. (http://webserver.codeplex.com/)
// Copyright (c) 2012 Jonas Gauffin
// Licensed under the Apache 2.0 License (http://opensource.org/licenses/Apache-2.0)
//

using System;
using System.IO;
using System.Net.Sockets;

namespace TridentFramework.RPC.Http.Transports
{
    /// <summary>
    /// Custom network stream to mark sockets as reusable when disposing the stream.
    /// </summary>
    public class ReusableSocketNetworkStream : NetworkStream
    {
        private bool isDisposed;

        /*
        ** Methods
        */

        /// <summary>
        /// Creates a new instance of the <see cref="T:System.Net.Sockets.NetworkStream" /> class for the specified <see cref="T:System.Net.Sockets.Socket" />.
        /// </summary>
        /// <param name="socket">The <see cref="T:System.Net.Sockets.Socket" /> that the <see cref="T:System.Net.Sockets.NetworkStream" /> will use to send and receive data.</param>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="socket" /> parameter is <c>null</c>. </exception>
        /// <exception cref="T:System.IO.IOException">
        /// The <paramref name="socket" /> parameter is not connected.
        /// -or-
        /// The <see cref="P:System.Net.Sockets.Socket.SocketType" /> property of the <paramref name="socket" /> parameter is not <see cref="F:System.Net.Sockets.SocketType.Stream" />.
        /// -or-
        /// The <paramref name="socket" /> parameter is in a nonblocking state.
        /// </exception>
        public ReusableSocketNetworkStream(Socket socket)
            : base(socket)
        {
            /* stub */
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Net.Sockets.NetworkStream" /> class for the specified <see cref="T:System.Net.Sockets.Socket" /> with the specified <see cref="T:System.Net.Sockets.Socket" /> ownership.
        /// </summary>
        /// <param name="socket">The <see cref="T:System.Net.Sockets.Socket" /> that the <see cref="T:System.Net.Sockets.NetworkStream" /> will use to send and receive data.</param>
        /// <param name="ownsSocket">Set to <c>true</c> to indicate that the <see cref="T:System.Net.Sockets.NetworkStream" /> will take ownership of the
        /// <see cref="T:System.Net.Sockets.Socket" />; otherwise, <c>false</c>. </param>
        /// <exception cref="T:System.ArgumentNullException">
        /// The <paramref name="socket" /> parameter is <c>null</c>.</exception>
        /// <exception cref="T:System.IO.IOException">
        /// The <paramref name="socket" /> parameter is not connected.
        /// -or-
        /// the value of the <see cref="P:System.Net.Sockets.Socket.SocketType" /> property of the <paramref name="socket" /> parameter is not <see cref="F:System.Net.Sockets.SocketType.Stream" />.
        /// -or-
        /// the <paramref name="socket" /> parameter is in a nonblocking state.
        /// </exception>
        public ReusableSocketNetworkStream(Socket socket, bool ownsSocket)
            : base(socket, ownsSocket)
        {
            /* stub */
        }

        /// <summary>
        /// Creates a new instance of the <see cref="T:System.Net.Sockets.NetworkStream" /> class for the specified <see cref="T:System.Net.Sockets.Socket" /> with the specified access rights.
        /// </summary>
        /// <param name="socket">The <see cref="T:System.Net.Sockets.Socket" /> that the <see cref="T:System.Net.Sockets.NetworkStream" /> will use to send and receive data. </param>
        /// <param name="access">A bitwise combination of the <see cref="T:System.IO.FileAccess" /> values that specify the type of access given to the
        /// <see cref="T:System.Net.Sockets.NetworkStream" /> over the provided <see cref="T:System.Net.Sockets.Socket" />.</param>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="socket" /> parameter is <c>null</c>.</exception>
        /// <exception cref="T:System.IO.IOException">
        /// The <paramref name="socket" /> parameter is not connected.
        /// -or-
        /// the <see cref="P:System.Net.Sockets.Socket.SocketType" /> property of the <paramref name="socket" /> parameter is not <see cref="F:System.Net.Sockets.SocketType.Stream" />.
        /// -or-
        /// the <paramref name="socket" /> parameter is in a nonblocking state.
        /// </exception>
        public ReusableSocketNetworkStream(Socket socket, FileAccess access)
            : base(socket, access)
        {
            /* stub */
        }

        /// <summary>
        /// Creates a new instance of the <see cref="T:System.Net.Sockets.NetworkStream" /> class for the specified <see cref="T:System.Net.Sockets.Socket" /> with the specified access rights and the specified <see cref="T:System.Net.Sockets.Socket" /> ownership.
        /// </summary>
        /// <param name="socket">The <see cref="T:System.Net.Sockets.Socket" /> that the <see cref="T:System.Net.Sockets.NetworkStream" /> will use to send and receive data.</param>
        /// <param name="access">A bitwise combination of the <see cref="T:System.IO.FileAccess" /> values that specifies the type of access given to the <see cref="T:System.Net.Sockets.NetworkStream" />
        /// over the provided <see cref="T:System.Net.Sockets.Socket" />. </param>
        /// <param name="ownsSocket">Set to <c>true</c> to indicate that the <see cref="T:System.Net.Sockets.NetworkStream" /> will take ownership of the
        /// <see cref="T:System.Net.Sockets.Socket" />; otherwise, <c>false</c>.</param>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="socket" /> parameter is <c>null</c>.</exception>
        /// <exception cref="T:System.IO.IOException">
        /// The <paramref name="socket" /> parameter is not connected.
        /// -or-
        /// The <see cref="P:System.Net.Sockets.Socket.SocketType" /> property of the <paramref name="socket" /> parameter is not <see cref="F:System.Net.Sockets.SocketType.Stream" />.
        /// -or-
        /// The <paramref name="socket" /> parameter is in a nonblocking state.
        /// </exception>
        public ReusableSocketNetworkStream(Socket socket, FileAccess access, bool ownsSocket)
            : base(socket, access, ownsSocket)
        {
            /* stub */
        }

        /// <inheritdoc />
        public override void Close()
        {
            if (isDisposed) throw new ObjectDisposedException(GetType().FullName);
            if (Socket != null && Socket.Connected)
                Socket.Close(); //TODO: Maybe use Disconnect with reuseSocket=true? I tried but it took forever.

            base.Close();
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (isDisposed) return;

            try
            {
                if (disposing)
                {
                    if (Socket != null && Socket.Connected)
                    {
                        try
                        {
                            Socket.Disconnect(true);
                        }
                        catch (ObjectDisposedException) { }
                    }
                }
            }
            finally
            {
                base.Dispose(disposing);
                isDisposed = true;
            }
        }
    } // public class ReusableSocketNetworkStream : NetworkStream
} // namespace TridentFramework.RPC.Http.Transports
