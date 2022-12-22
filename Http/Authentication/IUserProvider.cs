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
using System.Collections.Generic;
using System.Security.Principal;
using System.Text;

namespace TridentFramework.RPC.Http.Authentication
{
    /// <summary>
    /// Provider returning user to be authenticated.
    /// </summary>
    public interface IUserProvider
    {
        /*
        ** Methods
        */

        /// <summary>
        /// Lookups the specified user
        /// </summary>
        /// <param name="userName">User name.</param>
        /// <param name="host">Typically web server domain name.</param>
        /// <returns>User if found; otherwise <c>null</c>.</returns>
        /// <remarks>
        /// User name can basically be anything. For instance name entered by user when using
        /// basic or digest authentication, or SID when using Windows authentication.
        /// </remarks>
        IAuthenticationUser Lookup(string userName, string host);

        /// <summary>
        /// Gets the principal to use.
        /// </summary>
        /// <param name="user">Successfully authenticated user.</param>
        /// <returns></returns>
        /// <remarks>
        /// Invoked when a user have successfully been authenticated.
        /// </remarks>
        /// <seealso cref="GenericPrincipal"/>
        /// <seealso cref="WindowsPrincipal"/>
        IPrincipal GetPrincipal(IAuthenticationUser user);
    } // public interface IUserProvider

    /// <summary>
    /// User information used during authentication process.
    /// </summary>
    public interface IAuthenticationUser
    {
        /*
        ** Properties
        */

        /// <summary>
        /// Gets or sets user name used during authentication.
        /// </summary>
        string Username { get; set; }

        /// <summary>
        /// Gets or sets unencrypted password.
        /// </summary>
        /// <remarks>
        /// Password as clear text. You could use <see cref="HA1"/> instead if your passwords
        /// are encrypted in the database.
        /// </remarks>
        string Password { get; set; }

        /// <summary>
        /// Gets or sets HA1 hash.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Digest authentication requires clear text passwords to work. If you
        /// do not have that, you can store a HA1 hash in your database (which is part of
        /// the Digest authentication process).
        /// </para>
        /// <para>
        /// A HA1 hash is simply a Md5 encoded string: "UserName:Realm:Password". The quotes should
        /// not be included. Realm is the currently requested Host (as in <c>Request.Headers["host"]</c>).
        /// </para>
        /// <para>
        /// Leave the string as <c>null</c> if you are not using HA1 hashes.
        /// </para>
        /// </remarks>
        string HA1 { get; set; }
    } // public interface IAuthenticationUser
} // namespace TridentFramework.RPC.Http.Authentication
