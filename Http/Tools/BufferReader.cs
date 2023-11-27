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
//
// Based on code from the C# WebServer project. (http://webserver.codeplex.com/)
// Copyright (c) 2012 Jonas Gauffin
// Licensed under the Apache 2.0 License (http://opensource.org/licenses/Apache-2.0)
//

using System;
using System.Text;

namespace TridentFramework.RPC.Http.Tools
{
    /// <summary>
    /// Reads strings from a byte array.
    /// </summary>
    public class BufferReader : ITextReader
    {
        private readonly Encoding encoding;
        private byte[] buffer;

        /*
        ** Properties
        */

        /// <summary>
        /// Gets or sets line number.
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Gets if end of buffer have been reached
        /// </summary>
        /// <value></value>
        public bool EOF
        {
            get { return Index >= Length; }
        }

        /// <summary>
        /// Gets if more bytes can be processed.
        /// </summary>
        /// <value></value>
        public bool HasMore
        {
            get { return Index < Length; }
        }

        /// <summary>
        /// Gets next character
        /// </summary>
        /// <value><see cref="char.MinValue"/> if end of buffer.</value>
        public char Peek
        {
            get { return Index < Length - 1 ? (char)buffer[Index + 1] : char.MinValue; }
        }

        /// <summary>
        /// Gets current character
        /// </summary>
        /// <value><see cref="char.MinValue"/> if end of buffer.</value>
        public char Current
        {
            get { return HasMore ? (char)buffer[Index] : char.MinValue; }
        }

        /// <summary>
        /// Gets or sets current position in buffer.
        /// </summary>
        /// <remarks>
        /// THINK before you manually change the position since it can blow up
        /// the whole parsing in your face.
        /// </remarks>
        public int Index { get; set; }

        /// <summary>
        /// Gets total length of buffer.
        /// </summary>
        /// <value></value>
        public int Length { get; private set; }

        /// <summary>
        /// Gets number of bytes left.
        /// </summary>
        public int RemainingLength
        {
            get { return Length - Index; }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="BufferReader"/> class.
        /// </summary>
        public BufferReader()
        {
            encoding = Encoding.Default;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BufferReader"/> class.
        /// </summary>
        /// <param name="encoding">Encoding to use when converting byte array to strings.</param>
        public BufferReader(Encoding encoding)
        {
            this.encoding = encoding;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BufferReader"/> class.
        /// </summary>
        /// <param name="buffer">Buffer to read from.</param>
        /// <param name="encoding">Encoding to use when converting byte array to strings.</param>
        public BufferReader(byte[] buffer, Encoding encoding)
        {
            this.buffer = buffer;
            Length = buffer.Length;
            this.encoding = encoding;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="startIndex"></param>
        /// <param name="endIndex"></param>
        /// <returns></returns>
        private string GetString(int startIndex, int endIndex)
        {
            return encoding.GetString(buffer, startIndex, endIndex - startIndex);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="startIndex"></param>
        /// <param name="endIndex"></param>
        /// <param name="trimEnd"></param>
        /// <returns></returns>
        private string GetString(int startIndex, int endIndex, bool trimEnd)
        {
            if (trimEnd)
            {
                --endIndex; // need to move one back to be able to trim.
                while (endIndex > 0 && buffer[endIndex] == ' ' || buffer[endIndex] == '\t')
                    --endIndex;
                ++endIndex;
            }
            return encoding.GetString(buffer, startIndex, endIndex - startIndex);
        }

        /// <summary>
        /// Assign a new buffer
        /// </summary>
        /// <param name="buffer">Buffer to process.</param>
        /// <param name="offset">Where to start process buffer</param>
        /// <param name="count">Buffer length</param>
        /// <exception cref="ArgumentException">Buffer needs to be a byte array</exception>
        public void Assign(object buffer, int offset, int count)
        {
            if (!(buffer is byte[]))
                throw new ArgumentException("Buffer needs to be a byte array", "buffer");

            this.buffer = (byte[])buffer;
            Index = offset;
            Length = count;
        }

        /// <summary>
        /// Assign a new buffer
        /// </summary>
        /// <param name="buffer">Buffer to process</param>
        /// <exception cref="ArgumentException">Buffer needs to be a byte array</exception>
        public void Assign(object buffer)
        {
            if (!(buffer is byte[]))
                throw new ArgumentException("Buffer needs to be a byte array", "buffer");

            this.buffer = (byte[])buffer;
            Index = 0;
            Length = this.buffer.Length;
        }

        /// <summary>
        /// Consume current character.
        /// </summary>
        public void Consume()
        {
            ++Index;
        }

        /// <summary>
        /// Get a text line.
        /// </summary>
        /// <returns></returns>
        /// <remarks>Will merge multi line headers.</remarks>
        public string ReadLine()
        {
            int startIndex = Index;
            while (HasMore && Current != '\n')
                Consume();

            // EOF? Then we havent enough bytes.
            if (EOF)
            {
                Index = startIndex;
                return null;
            }

            Consume(); // eat \n too.

            string thisLine = encoding.GetString(buffer, startIndex, Index - startIndex - 2);

            // Multi line message?
            if (Current == '\t' || Current == ' ')
            {
                Consume();
                string extra = ReadLine();

                // Multiline isn't complete, wait for more bytes.
                if (extra == null)
                {
                    Index = startIndex;
                    return null;
                }

                return thisLine + " " + extra.TrimStart(' ', '\t');
            }

            return thisLine;
        }

        /// <summary>
        /// Read quoted string
        /// </summary>
        /// <returns>string if current character (in buffer) is a quote; otherwise <c>null</c>.</returns>
        public string ReadQuotedString()
        {
            Consume(' ', '\t');
            if (Current != '\"')
                return null;

            int startPos = Index;
            Consume();
            string buffer = string.Empty;
            while (!EOF)
            {
                switch (Current)
                {
                    case '\\':
                        break;

                    case '"':
                        Consume();
                        return buffer;

                    default:
                        buffer += Current;
                        break;
                }
            }

            Index = startPos;
            return null;
        }

        /// <summary>
        /// Read until end of string, or to one of the delimiters are found.
        /// </summary>
        /// <param name="delimiters">characters to stop at</param>
        /// <returns>
        /// A string (can be <see cref="string.Empty"/>).
        /// </returns>
        /// <remarks>
        /// Will not consume the delimiter.
        /// </remarks>
        /// <exception cref="InvalidOperationException"><c>InvalidOperationException</c>.</exception>
        public string ReadToEnd(string delimiters)
        {
            if (EOF)
                return string.Empty;

            int startIndex = Index;

            bool isDelimitersNewLine = delimiters.IndexOfAny(new[] { '\r', '\n' }) != -1;
            while (true)
            {
                if (EOF)
                    return GetString(startIndex, Index);

                if (delimiters.IndexOf(Current) != -1)
                    return GetString(startIndex, Index, true);

                // Delimiter is not new line and we got one.
                if (isDelimitersNewLine && Current == '\r' || Current == '\n')
                    throw new InvalidOperationException("Unexpected new line: " + GetString(startIndex, Index) +
                                                        "[CRLF].");

                ++Index;
            }
        }

        /// <summary>
        /// Read until end of string, or to one of the delimiters are found.
        /// </summary>
        /// <returns>A string (can be <see cref="string.Empty"/>).</returns>
        /// <remarks>
        /// Will not consume the delimiter.
        /// </remarks>
        public string ReadToEnd()
        {
            int index = Index;
            Index = Length;
            return encoding.GetString(buffer, index, Length - index);
        }

        /// <summary>
        /// Read to end of buffer, or until specified delimiter is found.
        /// </summary>
        /// <param name="delimiter">Delimiter to find.</param>
        /// <returns>
        /// A string (can be <see cref="string.Empty"/>).
        /// </returns>
        /// <remarks>
        /// Will not consume the delimiter.
        /// </remarks>
        /// <exception cref="InvalidOperationException"><c>InvalidOperationException</c>.</exception>
        public string ReadToEnd(char delimiter)
        {
            if (EOF)
                return string.Empty;

            int startIndex = Index;

            while (true)
            {
                if (EOF)
                    return GetString(startIndex, Index);

                if (Current == delimiter)
                    return GetString(startIndex, Index, true);

                // Delimiter is not new line and we got one.
                if (delimiter != '\r' && delimiter != '\n' && Current == '\r' || Current == '\n')
                    throw new InvalidOperationException("Unexpected new line: " + GetString(startIndex, Index) +
                                                        "[CRLF].");

                ++Index;
            }
        }

        /// <summary>
        /// Consume specified characters
        /// </summary>
        /// <param name="chars">One or more characters.</param>
        public void Consume(params char[] chars)
        {
            while (HasMore)
            {
                foreach (var ch in chars)
                {
                    if (ch == Current)
                        ++Index;
                    else
                        return;
                }
            }
        }

        /// <summary>
        /// Consumes horizontal white spaces (space and tab).
        /// </summary>
        public void ConsumeWhiteSpaces()
        {
            Consume(' ', '\t');
        }

        /// <summary>
        /// Consume horizontal white spaces and the specified character.
        /// </summary>
        /// <param name="extraCharacter">Extra character to consume</param>
        public void ConsumeWhiteSpaces(char extraCharacter)
        {
            Consume(' ', '\t', extraCharacter);
        }

        /// <summary>
        /// Read a character.
        /// </summary>
        /// <returns>
        /// Character if not EOF; otherwise <c>null</c>.
        /// </returns>
        public char Read()
        {
            return (char)buffer[Index++];
        }

        /// <summary>
        /// Will read until specified delimiter is found.
        /// </summary>
        /// <param name="delimiter">Character to stop at.</param>
        /// <returns>
        /// A string if the delimiter was found; otherwise <c>null</c>.
        /// </returns>
        /// <remarks>
        /// Will trim away spaces and tabs from the end.</remarks>
        /// <exception cref="InvalidOperationException"><c>InvalidOperationException</c>.</exception>
        public string ReadUntil(char delimiter)
        {
            if (EOF)
                return null;

            int startIndex = Index;

            while (true)
            {
                if (EOF)
                {
                    Index = startIndex;
                    return null;
                }

                if (Current == delimiter)
                    return GetString(startIndex, Index, true);

                // Delimiter is not new line and we got one.
                if (delimiter != '\r' && delimiter != '\n' && Current == '\r' || Current == '\n')
                    throw new InvalidOperationException("Unexpected new line: " + GetString(startIndex, Index) +
                                                        "[CRLF].");

                ++Index;
            }
        }

        /// <summary>
        /// Read until one of the delimiters are found.
        /// </summary>
        /// <param name="delimiters">characters to stop at</param>
        /// <returns>
        /// A string if one of the delimiters was found; otherwise <c>null</c>.
        /// </returns>
        /// <remarks>
        /// Will not consume the delimiter.
        /// </remarks>
        /// <exception cref="InvalidOperationException"><c>InvalidOperationException</c>.</exception>
        public string ReadUntil(string delimiters)
        {
            if (EOF)
                return null;

            int startIndex = Index;

            bool isDelimitersNewLine = delimiters.IndexOfAny(new[] { '\r', '\n' }) != -1;
            while (true)
            {
                if (EOF)
                {
                    Index = startIndex;
                    return null;
                }

                if (delimiters.IndexOf(Current) != -1)
                    return GetString(startIndex, Index, true);

                // Delimiter is not new line and we got one.
                if (isDelimitersNewLine && Current == '\r' || Current == '\n')
                    throw new InvalidOperationException("Unexpected new line: " + GetString(startIndex, Index) +
                                                        "[CRLF].");

                ++Index;
            }
        }

        /// <summary>
        /// Read until a horizontal white space occurs.
        /// </summary>
        /// <returns>A string if a white space was found; otherwise <c>null</c>.</returns>
        public string ReadWord()
        {
            return ReadUntil(" \t");
        }

        /// <summary>
        /// Checks if one of the remaining bytes are a specified character.
        /// </summary>
        /// <param name="ch">Character to find.</param>
        /// <returns>
        /// 	<c>true</c> if found; otherwise <c>false</c>.
        /// </returns>
        public bool Contains(char ch)
        {
            int index = Index;
            while (index < Length)
            {
                ++index;
                if (ch == buffer[index])
                    return true;
            }

            return false;
        }
    } // public class BufferReader : ITextReader
} // namespace TridentFramework.RPC.Http.Tools
