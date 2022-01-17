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
//
// Based on code from the C# WebServer project. (http://webserver.codeplex.com/)
// Copyright (c) 2012 Jonas Gauffin
// Licensed under the Apache 2.0 License (http://opensource.org/licenses/Apache-2.0)
//

using System;

namespace TridentFramework.RPC.Http.Tools
{
    /// <summary>
    /// Base interface to read string tokens from different sources.
    /// </summary>
    public interface ITextReader
    {
        /*
        ** Properties
        */

        /// <summary>
        /// Gets current character
        /// </summary>
        /// <value><see cref="char.MinValue"/> if end of buffer.</value>
        char Current { get; }

        /// <summary>
        /// Gets if end of buffer have been reached
        /// </summary>
        bool EOF { get; }

        /// <summary>
        /// Gets if more bytes can be processed.
        /// </summary>
        bool HasMore { get; }

        /// <summary>
        /// Gets or sets current position in buffer.
        /// </summary>
        /// <remarks>
        /// THINK before you manually change the position since it can blow up
        /// the whole parsing in your face.
        /// </remarks>
        int Index { get; set; }

        /// <summary>
        /// Gets total length of buffer.
        /// </summary>
        int Length { get; }

        /// <summary>
        /// Gets or sets line number.
        /// </summary>
        int LineNumber { get; set; }

        /// <summary>
        /// Gets next character
        /// </summary>
        /// <value><see cref="char.MinValue"/> if end of buffer.</value>
        char Peek { get; }

        /// <summary>
        /// Gets number of bytes left.
        /// </summary>
        int RemainingLength { get; }

        /*
        ** Methods
        */

        /// <summary>
        /// Assign a new buffer
        /// </summary>
        /// <param name="buffer">Buffer to process.</param>
        /// <param name="offset">Where to start process buffer</param>
        /// <param name="count">Buffer length</param>
        void Assign(object buffer, int offset, int count);

        /// <summary>
        /// Assign a new buffer
        /// </summary>
        /// <param name="buffer">Buffer to process</param>
        void Assign(object buffer);

        /// <summary>
        /// Consume current character.
        /// </summary>
        void Consume();

        /// <summary>
        /// Consume specified characters
        /// </summary>
        /// <param name="chars">One or more characters.</param>
        void Consume(params char[] chars);

        /// <summary>
        /// Consumes horizontal white spaces (space and tab).
        /// </summary>
        void ConsumeWhiteSpaces();

        /// <summary>
        /// Consume horizontal white spaces and the specified character.
        /// </summary>
        /// <param name="extraCharacter">Extra character to consume</param>
        void ConsumeWhiteSpaces(char extraCharacter);

        /// <summary>
        /// Checks if one of the remaining bytes are a specified character.
        /// </summary>
        /// <param name="ch">Character to find.</param>
        /// <returns><c>true</c> if found; otherwise <c>false</c>.</returns>
        bool Contains(char ch);

        /// <summary>
        /// Read a character.
        /// </summary>
        /// <returns>Character if not EOF; otherwise <c>null</c>.</returns>
        char Read();

        /// <summary>
        /// Get a text line.
        /// </summary>
        /// <returns></returns>
        /// <remarks>Will merge multiline headers.</remarks>
        string ReadLine();

        /// <summary>
        /// Read quoted string
        /// </summary>
        /// <returns>string if current character (in buffer) is a quote; otherwise <c>null</c>.</returns>
        string ReadQuotedString();

        /// <summary>
        /// Read until end of string, or to one of the delimiters are found.
        /// </summary>
        /// <param name="delimiters">characters to stop at</param>
        /// <returns>A string (can be <see cref="string.Empty"/>).</returns>
        /// <remarks>
        /// Will not consume the delimiter.
        /// </remarks>
        string ReadToEnd(string delimiters);

        /// <summary>
        /// Read until end of string, or to one of the delimiters are found.
        /// </summary>
        /// <returns>A string (can be <see cref="string.Empty"/>).</returns>
        /// <remarks>
        /// Will not consume the delimiter.
        /// </remarks>
        string ReadToEnd();

        /// <summary>
        /// Read to end of buffer, or until specified delimiter is found.
        /// </summary>
        /// <param name="delimiter">Delimiter to find.</param>
        /// <returns>A string (can be <see cref="string.Empty"/>).</returns>
        /// <remarks>
        /// Will not consume the delimiter.
        /// </remarks>
        string ReadToEnd(char delimiter);

        /// <summary>
        /// Will read until specified delimiter is found.
        /// </summary>
        /// <param name="delimiter">Character to stop at.</param>
        /// <returns>A string if the delimiter was found; otherwise <c>null</c>.</returns>
        /// <remarks>
        /// Will trim away spaces and tabs from the end.
        /// Will not consume the delimiter.
        /// </remarks>
        string ReadUntil(char delimiter);

        /// <summary>
        /// Read until one of the delimiters are found.
        /// </summary>
        /// <param name="delimiters">characters to stop at</param>
        /// <returns>A string if one of the delimiters was found; otherwise <c>null</c>.</returns>
        /// <remarks>
        /// Will trim away spaces and tabs from the end.
        /// Will not consume the delimiter.
        /// </remarks>
        string ReadUntil(string delimiters);

        /// <summary>
        /// Read until a horizontal white space occurs.
        /// </summary>
        /// <returns>A string if a white space was found; otherwise <c>null</c>.</returns>
        string ReadWord();
    } // public interface ITextReader
} // namespace TridentFramework.RPC.Http.Tools
