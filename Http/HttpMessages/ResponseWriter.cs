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
//
// Based on code from the C# WebServer project. (http://webserver.codeplex.com/)
// Copyright (c) 2012 Jonas Gauffin
// Licensed under the Apache 2.0 License (http://opensource.org/licenses/Apache-2.0)
//

using System;
#if DEBUG_PERF_TRACE
using System.Diagnostics;
#endif
using System.IO;
using System.IO.Compression;
using System.Text;

using TridentFramework.RPC.Http.Headers;

using TridentFramework.RPC.Utility;

using TridentFramework.Compression.zlib;

namespace TridentFramework.RPC.Http.HttpMessages
{
    /// <summary>
    /// Used to send a response back to the client.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Writes a <see cref="IResponse"/> object into a stream.
    /// </para>
    /// <para>
    /// Important! ResponseWriter do not throw any exceptions. Instead it just logs them and
    /// let them die peacefully. This is since the response writer is used from
    /// catch blocks here and there.
    /// </para>
    /// </remarks>
    public class ResponseWriter
    {
        private const int MEMORY_MAX_SIZE = 16777216;

        /*
        ** Events
        */

        /// <summary>
        ///
        /// </summary>
        public static event EventHandler HeadersSent = delegate { };

        /*
        ** Methods
        */

        /// <summary>
        /// Sends response using the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="response">The response.</param>
        public void Send(IHttpContext context, IResponse response)
        {
#if DEBUG_PERF_TRACE
            Stopwatch sw = new Stopwatch();
            sw.Start();
#endif
            bool closeResponseStream = false;
            Stream body = null;
            if (response.ContentEncoding != null)
            {
                if (response.Body.Length <= MEMORY_MAX_SIZE)
                {
                    closeResponseStream = true;
                    response.Body.Flush();
                    response.Body.Seek(0, SeekOrigin.Begin);

                    // which encoding type are we using?
                    switch (response.ContentEncoding.ToString().ToLowerInvariant())
                    {
                        case "gzip":
                            {
                                body = new MemoryStream();
                                using (GZipStream compress = new GZipStream(body, CompressionMode.Compress, true))
                                {
                                    WriteBody(response.Body, compress);
                                    compress.Flush();
                                }
                            }
                            break;

                        case "deflate":
                            {
                                body = new MemoryStream();
                                using (DeflaterOutputStream compress = new DeflaterOutputStream(body, new Deflater(Deflater.DEFAULT_COMPRESSION)))
                                {
                                    compress.IsStreamOwner = false;
                                    WriteBody(response.Body, compress);
                                    compress.Flush();
                                }
                            }
                            break;

                        case "compress":
                        case "br":
                        default:
                            RPCLogger.WriteWarning($"{response.ContentEncoding} is not a supported Content-Encoding; defaulting to none");
                            response.ContentEncoding = null; // reset this to default to prevent headers from being sent
                            break;
                    }
                }
                else
                    response.ContentEncoding = null; // reset this to default to prevent headers from being sent
            }

            if (body == null)
                body = response.Body;

            SendHeaders(context, response, body.Length);
            SendBody(context, body);

            try
            {
                if (context.Stream != null)
                    context.Stream.Flush();
                else
                    RPCLogger.WriteWarning("Context stream was null! Did the client disconnect perhaps?");
            }
            catch (Exception e)
            {
                RPCLogger.StackTrace("Failed to flush context stream.", e);
            }

#if DEBUG_PERF_TRACE
            sw.Stop();
            if (response.ContentEncoding != null)
                Trace.WriteLine($"ResponseWriter::Send(), uri = {context.Request.Uri}, method = {context.Request.Method}, body length = {response.Body.Length}, length = {body.Length}, type = {response.ContentType}, encoding = {response.ContentEncoding}, elapsed = {sw.Elapsed}");
            else
                Trace.WriteLine($"ResponseWriter::Send(), uri = {context.Request.Uri}, method = {context.Request.Method}, length = {response.Body.Length}, type = {response.ContentType}, elapsed = {sw.Elapsed}");
#endif

            if (closeResponseStream)
                body.Dispose();
        }

        /// <summary>
        /// Converts and sends a string.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="data"></param>
        /// <param name="encoding">Encoding used to transfer string</param>
        public void Send(IHttpContext context, string data, Encoding encoding)
        {
            try
            {
                byte[] buffer = encoding.GetBytes(data);
                RPCLogger.Trace("Sending " + buffer.Length + " bytes");
                if (data.Length < 4000)
                    RPCLogger.Trace(data);

                if (context.Stream != null)
                    context.Stream.Write(buffer, 0, buffer.Length);
                else
                    RPCLogger.WriteWarning("Context stream was null! Did the client disconnect perhaps?");
            }
            catch (Exception e)
            {
                RPCLogger.StackTrace("Failed to send data through context stream.", e);
            }
        }

        /// <summary>
        /// Write a body to the given context stream.
        /// </summary>
        /// <param name="body">Body to send</param>
        /// <param name="contextStream">Context stream.</param>
        private void WriteBody(Stream body, Stream contextStream)
        {
            var buffer = new byte[4196];
            int bytesRead = body.Read(buffer, 0, 4196);
            while (bytesRead > 0)
            {
                if (contextStream != null)
                {
                    contextStream.Write(buffer, 0, bytesRead);
                    bytesRead = body.Read(buffer, 0, 4196);
                }
                else
                    bytesRead = 0;
            }
        }

        /// <summary>
        /// Send a body to the client
        /// </summary>
        /// <param name="context">Context containing the stream to use.</param>
        /// <param name="body">Body to send</param>
        public void SendBody(IHttpContext context, Stream body)
        {
            try
            {
                body.Flush();
                body.Seek(0, SeekOrigin.Begin);

                WriteBody(body, context.Stream);
            }
            catch (Exception e)
            {
                RPCLogger.StackTrace("Failed to send body through context stream.", e, false);
            }
        }

        /// <summary>
        /// Send all headers to the client
        /// </summary>
        /// <param name="context">Content used to send headers.</param>
        /// <param name="response">Response containing call headers.</param>
        /// <param name="responseLength"></param>
        public void SendHeaders(IHttpContext context, IResponse response, long responseLength)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("{0} {1} {2}\r\n", response.HttpVersion, (int)response.Status, response.Reason);

            // replace content-type name with the actual one used.
            //response.ContentType.Parameters["charset"] = response.Encoding.WebName;

            // go through all property headers.
            sb.AppendFormat("{0}: {1}\r\n", response.ContentType.Name, response.ContentType);
            if (response.ContentEncoding != null)
            {
                sb.AppendFormat("{0}: {1}\r\n", response.ContentEncoding.Name, response.ContentEncoding);
                sb.AppendFormat("{0}: {1}\r\n", response.ContentLength.Name, responseLength);
            }
            else
                sb.AppendFormat("{0}: {1}\r\n", response.ContentLength.Name, response.ContentLength);
            sb.AppendFormat("{0}: {1}\r\n", response.Connection.Name, response.Connection);

            if (response.Cookies != null && response.Cookies.Count > 0)
            {
                //Set-Cookie: <name>=<value>[; <name>=<value>][; expires=<date>][; domain=<domain_name>][; path=<some_path>][; secure][; httponly]
                foreach (ResponseCookie cookie in response.Cookies)
                {
                    sb.Append("Set-Cookie: ");
                    sb.Append(cookie.ToString());
                    sb.Append("\r\n");
                }
            }

            foreach (IHeader header in response)
                sb.AppendFormat("{0}: {1}\r\n", header.Name, header);

            sb.Append("\r\n");
            Send(context, sb.ToString(), response.Encoding);
            HeadersSent(this, EventArgs.Empty);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="context"></param>
        /// <param name="response"></param>
        /// <param name="exception"></param>
        public void SendErrorPage(IHttpContext context, IResponse response, Exception exception)
        {
            string htmlTemplate = @"<html>
    <head>
        <meta http-equiv=""Content-Type"" content=""text/html; charset=utf-8"" />
        <title>{1}</title>
    </head>
    <body>
        <h1>{0} - {1}</h1>
        <pre>{2}</pre>
    </body>
</html>";

            var body = string.Format(htmlTemplate, (int)response.Status, response.Reason, exception);
            byte[] bodyBytes = response.Encoding.GetBytes(body);
            response.Body.Write(bodyBytes, 0, bodyBytes.Length);
            Send(context, response);
        }
    } // public class ResponseWriter
} // namespace TridentFramework.RPC.Http.HttpMessages
