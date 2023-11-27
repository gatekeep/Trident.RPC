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
using System.Globalization;
using System.IO;
using System.Text;

namespace TridentFramework.RPC.Http.BodyDecoders
{
    /// <summary>
    /// Stream-based multipart handling.
    ///
    /// In this incarnation deals with an HttpInputStream as we are now using
    /// IntPtr-based streams instead of byte [].   In the future, we will also
    /// send uploads above a certain threshold into the disk (to implement
    /// limit-less HttpInputFiles). 
    /// </summary>
    /// <remarks>Taken from HttpRequest in mono (http://www.mono-project.com)</remarks>
    internal class HttpMultipart
    {
        private const byte CR = (byte) '\r';
        private const byte LF = (byte) '\n';
        
        private bool atEof;
        
        private string boundary;
        private byte[] boundaryBytes;
        
        private byte[] buffer;
        
        private Stream data;
        private Encoding encoding;
        
        private StringBuilder sb;

        /*
        ** Classes
        */

        /// <summary>
        /// 
        /// </summary>
        public class Element
        {
            public string ContentType;
            public string Filename;
            public long Length;
            public string Name;
            public long Start;

            /*
            ** Methods
            */

            /// <inheritdoc />
            public override string ToString()
            {
                return "ContentType " + ContentType + ", Name " + Name + ", Filename " + Filename + ", Start " +
                       Start.ToString() + ", Length " + Length.ToString();
            }
        } // public class Element

        /*
        ** Methods
        */

        // See RFC 2046 
        // In the case of multipart entities, in which one or more different
        // sets of data are combined in a single body, a "multipart" media type
        // field must appear in the entity's header.  The body must then contain
        // one or more body parts, each preceded by a boundary delimiter line,
        // and the last one followed by a closing boundary delimiter line.
        // After its boundary delimiter line, each body part then consists of a
        // header area, a blank line, and a body area.  Thus a body part is
        // similar to an RFC 822 message in syntax, but different in meaning.

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpMultipart"/> class.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="b"></param>
        /// <param name="encoding"></param>

        public HttpMultipart(Stream data, string b, Encoding encoding)
        {
            this.data = data;
            
            boundary = b;
            boundaryBytes = encoding.GetBytes(b);
            
            buffer = new byte[boundaryBytes.Length + 2]; // CRLF or '--'
            
            this.encoding = encoding;
            
            sb = new StringBuilder();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        private bool CompareBytes(byte[] orig, byte[] other)
        {
            for (int i = orig.Length - 1; i >= 0; i--)
                if (orig[i] != other[i])
                    return false;

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="l"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private static string GetContentDispositionAttribute(string l, string name)
        {
            int idx = l.IndexOf(name + "=\"");
            if (idx < 0)
                return null;

            int begin = idx + name.Length + "=\"".Length;
            int end = l.IndexOf('"', begin);
            if (end < 0)
                return null;
            if (begin == end)
                return "";

            return l.Substring(begin, end - begin);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="s1"></param>
        /// <param name="s2"></param>
        /// <param name="ignoreCase"></param>
        /// <returns></returns>
        public static bool StartsWith(string s1, string s2, bool ignoreCase)
        {
            int l2 = s2.Length;
            if (l2 == 0)
                return true;

            int l1 = s1.Length;
            if (l2 > l1)
                return false;

            return (0 == string.Compare(s1, 0, s2, 0, l2, ignoreCase, CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="s1"></param>
        /// <param name="s2"></param>
        /// <param name="ignoreCase"></param>
        /// <returns></returns>
        public static bool EndsWith(string s1, string s2, bool ignoreCase)
        {
            int l2 = s2.Length;
            if (l2 == 0)
                return true;

            int l1 = s1.Length;
            if (l2 > l1)
                return false;

            return (0 == string.Compare(s1, l1 - l2, s2, 0, l2, ignoreCase, CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="l"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private string GetContentDispositionAttributeWithEncoding(string l, string name)
        {
            int idx = l.IndexOf(name + "=\"");
            if (idx < 0)
                return null;

            int begin = idx + name.Length + "=\"".Length;
            int end = l.IndexOf('"', begin);
            if (end < 0)
                return null;
            if (begin == end)
                return "";

            string temp = l.Substring(begin, end - begin);
            var source = new byte[temp.Length];
            for (int i = temp.Length - 1; i >= 0; i--)
                source[i] = (byte) temp[i];

            return encoding.GetString(source);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private long MoveToNextBoundary()
        {
            long retval = 0;
            bool gotCR = false;

            int state = 0;
            int c = data.ReadByte();
            while (true)
            {
                if (c == -1)
                    return -1;

                if (state == 0 && c == LF)
                {
                    retval = data.Position - 1;
                    if (gotCR)
                        retval--;

                    state = 1;
                    c = data.ReadByte();
                }
                else if (state == 0)
                {
                    gotCR = (c == CR);
                    c = data.ReadByte();
                }
                else if (state == 1 && c == '-')
                {
                    c = data.ReadByte();
                    if (c == -1)
                        return -1;

                    if (c != '-')
                    {
                        state = 0;
                        gotCR = false;
                        continue; // no ReadByte() here
                    }

                    int nread = data.Read(buffer, 0, buffer.Length);
                    int bl = buffer.Length;
                    if (nread != bl)
                        return -1;

                    if (!CompareBytes(boundaryBytes, buffer))
                    {
                        state = 0;
                        data.Position = retval + 2;
                        if (gotCR)
                        {
                            data.Position++;
                            gotCR = false;
                        }
                        
                        c = data.ReadByte();
                        continue;
                    }

                    if (buffer[bl - 2] == '-' && buffer[bl - 1] == '-')
                        atEof = true;
                    else if (buffer[bl - 2] != CR || buffer[bl - 1] != LF)
                    {
                        state = 0;
                        data.Position = retval + 2;
                        if (gotCR)
                        {
                            data.Position++;
                            gotCR = false;
                        }
                        c = data.ReadByte();
                        continue;
                    }

                    data.Position = retval + 2;
                    if (gotCR)
                        data.Position++;
                    break;
                }
                else
                {
                    // state == 1
                    state = 0; // no ReadByte() here
                }
            }

            return retval;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private bool ReadBoundary()
        {
            try
            {
                string line = ReadLine();
                while (line == "")
                    line = ReadLine();
                if (line[0] != '-' || line[1] != '-')
                    return false;

                if (!EndsWith(line, boundary, false))
                    return true;
            }
            catch
            {
                /* stub */
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private string ReadHeaders()
        {
            string s = ReadLine();
            if (s == "")
                return null;

            return s;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private string ReadLine()
        {
            // CRLF or LF are ok as line endings.
            bool gotCR = false;

            int b = 0;
            sb.Length = 0;
            while (true)
            {
                b = data.ReadByte();
                if (b == -1)
                    return null;

                if (b == LF)
                    break;

                gotCR = (b == CR);
                sb.Append((char) b);
            }

            if (gotCR)
                sb.Length--;

            return sb.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Element ReadNextElement()
        {
            if (atEof || ReadBoundary())
                return null;

            Element elem = new Element();
            string header;
            while ((header = ReadHeaders()) != null)
            {
                if (StartsWith(header, "Content-Disposition:", true))
                {
                    elem.Name = GetContentDispositionAttribute(header, "name");
                    elem.Filename = StripPath(GetContentDispositionAttributeWithEncoding(header, "filename"));
                }
                else if (StartsWith(header, "Content-Type:", true))
                    elem.ContentType = header.Substring("Content-Type:".Length).Trim();
            }

            long start = data.Position;
            elem.Start = start;
            long pos = MoveToNextBoundary();
            if (pos == -1)
                return null;

            elem.Length = pos - start;
            return elem;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static string StripPath(string path)
        {
            if (path == null || path.Length == 0)
                return path;

            if (path.IndexOf(":\\") != 1 && !path.StartsWith("\\\\"))
                return path;
            return path.Substring(path.LastIndexOf('\\') + 1);
        }
    } // internal class HttpMultipart
} // namespace TridentFramework.RPC.Http.BodyDecoders
