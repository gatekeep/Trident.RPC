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
using System.Text.RegularExpressions;

namespace TridentFramework.RPC.Http.Routing
{
    /// <summary>
    /// Class to make dynamic binding of redirects. Instead of having to specify a number of similar redirect rules
    /// a regular expression can be used to identify redirect URLs and their targets.
    /// </summary>
    /// <example>
    /// <![CDATA[
    /// new RegexRedirectRule("/(?<target>[a-z0-9]+)", "/users/${target}/?find=true", RegexOptions.IgnoreCase)
    /// ]]>
    /// </example>
    public class RegExRouter : SimpleRouter
    {
        private readonly Regex matchUrl;

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="RegExRouter"/> class.
        /// </summary>
        /// <param name="fromUrlExpression">Expression to match URL</param>
        /// <param name="toUrlExpression">Expression to generate URL</param>
        /// <example>
        /// <![CDATA[
        /// server.Add(new RegexRedirectRule("/(?<first>[a-zA-Z0-9]+)", "/user/${first}"));
        /// Result of ie. /employee1 will then be /user/employee1
        /// ]]>
        /// </example>
        public RegExRouter(string fromUrlExpression, string toUrlExpression)
            : this(fromUrlExpression, toUrlExpression, RegexOptions.None, true)
        {
            /* stub */
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RegExRouter"/> class.
        /// </summary>
        /// <param name="fromUrlExpression">Expression to match URL</param>
        /// <param name="toUrlExpression">Expression to generate URL</param>
        /// <param name="options">Regular expression options to use, can be <c>null</c></param>
        /// <example>
        /// <![CDATA[
        /// server.Add(new RegexRedirectRule("/(?<first>[a-zA-Z0-9]+)", "/user/{first}", RegexOptions.IgnoreCase));
        /// Result of ie. /employee1 will then be /user/employee1
        /// ]]>
        /// </example>
        public RegExRouter(string fromUrlExpression, string toUrlExpression, RegexOptions options)
            : this(fromUrlExpression, toUrlExpression, options, true)
        {
            /* stub */
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RegExRouter"/> class.
        /// </summary>
        /// <param name="fromUrlExpression">Expression to match URL</param>
        /// <param name="toUrlExpression">Expression to generate URL</param>
        /// <param name="options">Regular expression options to apply</param>
        /// <param name="shouldRedirect"><c>true</c> if request should be redirected, <c>false</c> if the request URI should be replaced.</param>
        /// <example>
        /// <![CDATA[
        /// server.Add(new RegexRedirectRule("/(?<first>[a-zA-Z0-9]+)", "/user/${first}", RegexOptions.None));
        /// Result of ie. /employee1 will then be /user/employee1
        /// ]]>
        /// </example>
        /// <exception cref="ArgumentNullException">Argument is <c>null</c>.</exception>
        /// <seealso cref="SimpleRouter.ShouldRedirect"/>
        public RegExRouter(string fromUrlExpression, string toUrlExpression, RegexOptions options, bool shouldRedirect)
            :
                base(fromUrlExpression, toUrlExpression, shouldRedirect)
        {
            if (string.IsNullOrEmpty(fromUrlExpression))
                throw new ArgumentNullException("fromUrlExpression");
            if (string.IsNullOrEmpty(toUrlExpression))
                throw new ArgumentNullException("toUrlExpression");

            matchUrl = new Regex(fromUrlExpression, options);
        }

        /// <inheritdoc />
        public override ProcessingResult Process(RequestContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            IRequest request = context.Request;
            IResponse response = context.Response;

            // If a match is found
            if (matchUrl.IsMatch(request.Uri.AbsolutePath))
            {
                // Return the replace result
                string resultUrl = matchUrl.Replace(request.Uri.AbsolutePath, ToUrl);
                if (!ShouldRedirect)
                {
                    request.Uri = new Uri(request.Uri, resultUrl);
                    return ProcessingResult.Continue;
                }

                response.Redirect(resultUrl);
                return ProcessingResult.SendResponse;
            }

            return ProcessingResult.Continue;
        }
    } // public class RegExRouter : SimpleRouter
} // namespace TridentFramework.RPC.Http.Routing
