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
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web;

using TridentFramework.RPC.Http.Headers;

namespace TridentFramework.RPC.Http.BodyDecoders
{
    /// <summary>
    /// Decodes forms that have multiple sections.
    /// </summary>
    /// <remarks>
    /// http://www.faqs.org/rfcs/rfc1867.html
    /// </remarks>
    public class MultiPartDecoder : IBodyDecoder
    {
        /// <summary>
        /// form-data
        /// </summary>
        public const string FormData = "form-data";

        /// <summary>
        /// multipart/form-data
        /// </summary>
        public const string MimeType = "multipart/form-data";

        /*
        ** Properties
        */

        /// <summary>
        /// All content types that the decoder can parse.
        /// </summary>
        /// <returns>A collection of all content types that the decoder can handle.</returns>
        public IEnumerable<string> ContentTypes
        {
            get { return new[] { MimeType, FormData }; }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Decode body stream.
        /// </summary>
        /// <param name="stream">Stream containing the content</param>
        /// <param name="contentType">Content type header</param>
        /// <param name="encoding">Stream encoding</param>
        /// <returns>Decoded data.</returns>
        /// <exception cref="FormatException">Body format is invalid for the specified content type.</exception>
        /// <exception cref="InternalServerException">Something unexpected failed.</exception>
        /// <exception cref="ArgumentNullException"><c>stream</c> is <c>null</c>.</exception>
        public DecodedData Decode(Stream stream, ContentTypeHeader contentType, Encoding encoding)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");
            if (contentType == null)
                throw new ArgumentNullException("contentType");
            if (encoding == null)
                throw new ArgumentNullException("encoding");

            // multipart/form-data, boundary=AaB03x
            string boundry = contentType.Parameters["boundary"];
            if (boundry == null)
                throw new FormatException("Missing boundary in content type.");

            HttpMultipart multipart = new HttpMultipart(stream, boundry, encoding);
            DecodedData form = new DecodedData();

            HttpMultipart.Element element;
            while ((element = multipart.ReadNextElement()) != null)
            {
                if (string.IsNullOrEmpty(element.Name))
                    throw new FormatException("Error parsing request. Missing value name.\nElement: " + element);

                if (!string.IsNullOrEmpty(element.Filename))
                {
                    if (string.IsNullOrEmpty(element.ContentType))
                        throw new FormatException("Error parsing request. Value '" + element.Name +
                                                  "' lacks a content type.");

                    // read the file data
                    byte[] buffer = new byte[element.Length];
                    stream.Seek(element.Start, SeekOrigin.Begin);
                    stream.Read(buffer, 0, (int) element.Length);

                    // generate a filename
                    string originalFileName = element.Filename;
                    string internetCache = Environment.GetFolderPath(Environment.SpecialFolder.InternetCache);

                    // if the internet path doesn't exist, assume mono and /var/tmp
                    string path = string.IsNullOrEmpty(internetCache) ? Path.Combine("var", "tmp") : Path.Combine(internetCache.Replace("\\\\", "\\"), "tmp");
                    element.Filename = Path.Combine(path, Math.Abs(element.Filename.GetHashCode()) + ".tmp");

                    // if the file exists generate a new filename
                    while (File.Exists(element.Filename))
                        element.Filename = Path.Combine(path, Math.Abs(element.Filename.GetHashCode() + 1) + ".tmp");

                    if (!Directory.Exists(path))
                        Directory.CreateDirectory(path);

                    File.WriteAllBytes(element.Filename, buffer);

                    HttpFile file = new HttpFile
                    {
                        Name = element.Name,
                        OriginalFileName = originalFileName,
                        ContentType = element.ContentType,
                        TempFileName = element.Filename
                    };
                    form.Files.Add(file);
                }
                else
                {
                    byte[] buffer = new byte[element.Length];
                    stream.Seek(element.Start, SeekOrigin.Begin);
                    stream.Read(buffer, 0, (int) element.Length);

                    form.Parameters.Add(HttpUtility.UrlDecode(element.Name), encoding.GetString(buffer));
                }
            }

            return form;
        }
    } // public class MultiPartDecoder : IBodyDecoder
} // namespace TridentFramework.RPC.Http.BodyDecoders
