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
using System.Net;

using TridentFramework.RPC.Http.Tools;

using TridentFramework.RPC.Utility;

namespace TridentFramework.RPC.Http.HttpMessages.Parser
{
    /// <summary>
    /// A HTTP parser using delegates to which parsing methods.
    /// </summary>
    public class HttpParser
    {
        private readonly BodyEventArgs bodyEventArgs = new BodyEventArgs();
        private readonly HeaderEventArgs headerEventArgs = new HeaderEventArgs();

        private readonly BufferReader reader = new BufferReader();

        private readonly RequestLineEventArgs requestEventArgs = new RequestLineEventArgs();
        private readonly ResponseLineEventArgs responseEventArgs = new ResponseLineEventArgs();

        private int bodyBytesLeft;
        private byte[] buffer;

        private string headerName;
        private string headerValue;

        private ParserMethod parserMethod;

        /*
        ** Properties
        */

        /// <summary>
        /// Gets or sets current line number.
        /// </summary>
        public int LineNumber { get; set; }

        /*
        ** Events
        */

        /// <summary>
        /// The request line has been parsed.
        /// </summary>
        public event EventHandler<RequestLineEventArgs> RequestLineParsed = delegate { };

        /// <summary>
        /// Response line has been parsed.
        /// </summary>
        public event EventHandler<ResponseLineEventArgs> ResponseLineParsed = delegate { };

        /// <summary>
        /// Parsed a header.
        /// </summary>
        public event EventHandler<HeaderEventArgs> HeaderParsed = delegate { };

        /// <summary>
        /// Received body bytes.
        /// </summary>
        public event EventHandler<BodyEventArgs> BodyBytesReceived = delegate { };

        /// <summary>
        /// A message have been successfully parsed.
        /// </summary>
        public event EventHandler MessageComplete = delegate { };

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpParser"/> class.
        /// </summary>
        public HttpParser()
        {
            parserMethod = ParseFirstLine;
        }

        /// <summary>
        /// Parser method to copy all body bytes.
        /// </summary>
        /// <returns></returns>
        /// <remarks>Needed since a TCP packet can contain multiple messages
        /// after each other, or partial messages.</remarks>
        private bool GetBody()
        {
            if (reader.RemainingLength == 0)
                return false;

            // Got enough bytes to complete body.
            if (reader.RemainingLength >= bodyBytesLeft)
            {
                OnBodyBytes(buffer, reader.Index, bodyBytesLeft);
                reader.Index += bodyBytesLeft;
                bodyBytesLeft = 0;
                OnComplete();
                return false;
            }

            // eat remaining bytes.
            OnBodyBytes(buffer, reader.Index, reader.RemainingLength);
            bodyBytesLeft -= reader.RemainingLength;
            reader.Index = reader.Length; // place it in the end
            return reader.Index != reader.Length;
        }

        /// <summary>
        /// Try to find a header name.
        /// </summary>
        /// <returns></returns>
        private bool GetHeaderName()
        {
            // empty line. body is begining.
            if (reader.Current == '\r' && (reader.Peek == '\n' || reader.Peek == '\0'))
            {
                // Eat the line break
                if (reader.Peek == '\n')
                    reader.Consume('\r', '\n');
                else
                    reader.Consume('\r', '\0');

                // Don't have a body?
                if (bodyBytesLeft == 0)
                {
                    OnComplete();
                    parserMethod = ParseFirstLine;
                }
                else
                    parserMethod = GetBody;

                return true;
            }

            headerName = reader.ReadUntil(':');
            if (headerName == null)
                return false;

            reader.Consume(); // eat colon
            parserMethod = GetHeaderValue;
            return true;
        }

        /// <summary>
        /// Get header values.
        /// </summary>
        /// <returns></returns>
        /// <remarks>Will also look for multi header values and automatically merge them to one line.</remarks>
        /// <exception cref="ParserException">Content length is not a number.</exception>
        private bool GetHeaderValue()
        {
            // remove white spaces.
            reader.Consume(' ', '\t');

            // multi line or empty value?
            if (reader.Current == '\r' && reader.Peek == '\n')
            {
                reader.Consume('\r', '\n');

                // empty value.
                if (reader.Current != '\t' && reader.Current != ' ')
                {
                    OnHeader(headerName, string.Empty);
                    headerName = null;
                    headerValue = string.Empty;
                    parserMethod = GetHeaderName;
                    return true;
                }

                if (reader.RemainingLength < 1)
                    return false;

                // consume one whitespace
                reader.Consume();

                // and fetch the rest.
                return GetHeaderValue();
            }

            string value = reader.ReadLine();
            if (value == null)
                return false;

            headerValue += value;
            if (string.Compare(headerName, "Content-Length", true) == 0)
            {
                if (!int.TryParse(value, out bodyBytesLeft))
                    throw new ParserException("Content length is not a number.");
            }

            OnHeader(headerName, value);

            headerName = null;
            headerValue = string.Empty;
            parserMethod = GetHeaderName;
            return true;
        }

        /// <summary>
        /// Toggle body bytes event.
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        protected virtual void OnBodyBytes(byte[] bytes, int offset, int count)
        {
            bodyEventArgs.AssignInternal(bytes, offset, count);
            BodyBytesReceived(this, bodyEventArgs);
        }

        /// <summary>
        /// Raise the <see cref="MessageComplete"/> event, since we have successfully parsed a message and it's body.
        /// </summary>
        protected virtual void OnComplete()
        {
            Reset();
            MessageComplete(this, EventArgs.Empty);
        }

        /// <summary>
        /// First message line.
        /// </summary>
        /// <param name="words">Will always contain three elements.</param>
        /// <remarks>Used to raise the <see cref="RequestLineParsed"/> or <see cref="ResponseLineParsed"/> event
        /// depending on the words in the array.</remarks>
        /// <exception cref="BadRequestException"><c>BadRequestException</c>.</exception>
        protected virtual void OnFirstLine(string[] words)
        {
            string firstWord = words[0].ToUpper();
            if (firstWord.StartsWith("HTTP"))
            {
                responseEventArgs.Version = words[0];
                try
                {
                    responseEventArgs.StatusCode = (HttpStatusCode)Enum.Parse(typeof(HttpStatusCode), words[1]);
                }
                catch (ArgumentException err)
                {
                    int code;
                    if (!int.TryParse(words[1], out code))
                        throw new BadRequestException("Status code '" + words[1] + "' is not known.", err);
                }
                responseEventArgs.ReasonPhrase = words[1];
                ResponseLineParsed(this, responseEventArgs);
            }
            else
            {
                try
                {
                    requestEventArgs.Method = words[0].ToUpper();
                }
                catch (ArgumentException err)
                {
                    throw new BadRequestException("Unrecognized HTTP method: " + words[0], err);
                }

                requestEventArgs.UriPath = words[1];
                requestEventArgs.Version = words[2];
                RequestLineParsed(this, requestEventArgs);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        private void OnHeader(string name, string value)
        {
            headerEventArgs.Name = name;
            headerEventArgs.Value = value;
            HeaderParsed(this, headerEventArgs);
        }

        /// <summary>
        /// Continue parsing a message
        /// </summary>
        /// <param name="buffer">Byte buffer containing bytes</param>
        /// <param name="offset">Where to start the parsing</param>
        /// <param name="count">Number of bytes to parse</param>
        /// <returns>index where the parsing stopped.</returns>
        /// <exception cref="ParserException">Parsing failed.</exception>
        public int Parse(byte[] buffer, int offset, int count)
        {
            RPCLogger.TraceHex("Parsing " + count + " bytes from offset " + offset + " using " + parserMethod.Method.Name, buffer, (count < 512) ? 512 : count);
            this.buffer = buffer;
            reader.Assign(buffer, offset, count);

            while (parserMethod()) ;
            //Messages.Trace("Switched parser method to " + parserMethod.Method.Name + " at index " + reader.Index);

            return reader.Index;
        }

        /// <summary>
        /// Parses the first line in a request/response.
        /// </summary>
        /// <returns><c>true</c> if first line is well formatted; otherwise <c>false</c>.</returns>
        /// <exception cref="BadRequestException">Invalid request/response line.</exception>
        public bool ParseFirstLine()
        {
            reader.Consume('\r', '\n');

            // Do not contain a complete first line.
            if (!reader.Contains('\n'))
                return false;

            var words = new string[3];
            words[0] = reader.ReadUntil(' ');
            reader.Consume(); // eat delimiter
            words[1] = reader.ReadUntil(' ');
            reader.Consume(); // eat delimiter
            words[2] = reader.ReadLine();
            if (string.IsNullOrEmpty(words[0]) || string.IsNullOrEmpty(words[1]) || string.IsNullOrEmpty(words[2]))
                throw new BadRequestException("Invalid request/response line.");

            OnFirstLine(words);
            parserMethod = GetHeaderName;
            return true;
        }

        /// <summary>
        /// Reset parser to initial state.
        /// </summary>
        public void Reset()
        {
            //_logger.Info("Resetting..");
            headerValue = null;
            headerName = string.Empty;
            bodyBytesLeft = 0;
            parserMethod = ParseFirstLine;
        }

        /// <summary>
        /// Used to be able to quickly swap parser method.
        /// </summary>
        /// <returns></returns>
        private delegate bool ParserMethod();
    } // public class HttpParser
} // namespace TridentFramework.RPC.Http.HttpMessages.Parser
