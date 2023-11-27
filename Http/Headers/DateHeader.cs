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

namespace TridentFramework.RPC.Http.Headers
{
    /// <summary>
    /// Header for "Date" and "If-Modified-Since"
    /// </summary>
    /// <remarks>
    /// <para>
    /// The field value is an HTTP-date, as described in section 3.3.1 in RFC2616;
    /// it MUST be sent in RFC 1123 [8]-date format. An example is
    ///<example>
    /// Date: Tue, 15 Nov 1994 08:12:31 GMT
    /// </example>
    ///</para><para>Origin servers MUST include a Date header field in all
    ///responses, except in these cases:
    ///<list type="number">
    /// <item>If the response status code is 100 (Continue) or 101 (Switching
    /// Protocols), the response MAY include a Date header field, at the server's
    /// option.
    ///</item><item>If the response status code conveys a server error, e.g. 500
    ///(Internal Server Error) or 503 (Service Unavailable), and it is inconvenient
    ///or impossible to generate a valid Date.
    ///</item><item>If the server does not have a clock that can provide a
    ///reasonable approximation of the current time, its responses MUST NOT include
    ///a Date header field. In this case, the rules in section 14.18.1 in RFC2616
    ///MUST be followed.
    /// </item>
    /// </list>
    ///</para><para>A received message that does not have a Date header field MUST
    ///be assigned one by the recipient if the message will be cached by that
    ///recipient or gatewayed via a protocol which requires a Date. An HTTP
    ///implementation without a clock MUST NOT cache responses without revalidating
    ///them on every use. An HTTP cache, especially a shared cache, SHOULD use a
    ///mechanism, such as NTP [28], to synchronize its clock with a reliable
    ///external standard.
    ///</para><para>Clients SHOULD only send a Date header field in messages that
    ///include an entity-body, as in the case of the PUT and POST requests, and
    ///even then it is optional. A client without a clock MUST NOT send a Date
    ///header field in a request.
    ///</para><para>The HTTP-date sent in a Date header SHOULD NOT represent a date
    ///and time subsequent to the generation of the message. It SHOULD represent
    ///the best available approximation of the date and time of message generation,
    ///unless the implementation has no means of generating a reasonably accurate
    ///date and time. In theory, the date ought to represent the moment just before
    ///the entity is generated. In practice, the date can be generated at any time
    ///during the message origination without affecting its semantic value.
    /// </para>
    /// </remarks>
    public class DateHeader : IHeader
    {
        /// <summary>
        /// Header name
        /// </summary>
        public const string NAME = "Date";

        /*
        ** Properties
        */

        /// <summary>
        /// Gets or sets date time.
        /// </summary>
        /// <value>Should be in UTC.</value>
        public DateTime Value { get; set; }

        /// <summary>
        /// Gets header name
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets Date header as a string
        /// </summary>
        public string HeaderValue
        {
            get { return Value.ToString("r"); }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="DateHeader"/> class.
        /// </summary>
        /// <param name="name">Header name.</param>
        /// <exception cref="ArgumentException">Name must not be empty.</exception>
        public DateHeader(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Name must not be empty.", "name");
            Name = name;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DateHeader"/> class.
        /// </summary>
        /// <param name="name">Header name.</param>
        /// <param name="value">Universal time.</param>
        public DateHeader(string name, DateTime value)
            : this(name)
        {
            Value = value;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return HeaderValue;
        }
    } // public class DateHeader : IHeader
} // namespace TridentFramework.RPC.Http.Headers
