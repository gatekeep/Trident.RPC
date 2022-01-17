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

using System;
using System.Collections.Generic;

namespace TridentFramework.RPC.Http.Service
{
    /// <summary>
    /// Abstract class representing a web server web page.
    /// </summary>
    public abstract class IWebPage
    {
        protected IHttpListener listener;
        protected IHttpContext context;
        private RequestWorker worker;

        protected Template pageTpl;

        protected string requestPath;
        protected string pageTitle;

        /*
        ** Properties
        */

        /// <summary>
        /// Gets or sets the page title.
        /// </summary>
        public string PageTitle
        {
            get { return pageTitle; }
            set { pageTitle = value; }
        }

        /// <summary>
        /// Gets or sets the page request path.
        /// </summary>
        public string RequestPath
        {
            get { return requestPath; }
            set { requestPath = value; }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="IWebPage"/> class.
        /// </summary>
        public IWebPage()
        {
            this.pageTitle = "DID-NOT-SET-PAGE_TITLE";
            this.pageTpl = null;

            this.requestPath = "/";
        }

        /// <summary>
        /// Display "main" page for this page.
        /// </summary>
        /// <param name="queryParameters"></param>
        protected abstract void MainPage(Dictionary<string, string> queryParameters);

        /// <summary>
        /// Handles various page list operations.
        /// </summary>
        /// <param name="queryParameters"></param>
        /// <returns>True, if page has been handled, otherwise false</returns>
        protected virtual bool PageLists(Dictionary<string, string> queryParameters)
        {
            return false;
        }

        /// <summary>
        /// Processes the web request.
        /// </summary>
        /// <param name="listener"></param>
        /// <param name="context"></param>
        /// <param name="worker"></param>
        /// <param name="queryParameters"></param>
        public virtual void ProcessRequest(IHttpListener listener, IHttpContext context, RequestWorker worker, Dictionary<string, string> queryParameters)
        {
            this.listener = listener;
            this.context = context;
            this.worker = worker;

            // execute default main page
            if (!PageLists(queryParameters))
                MainPage(queryParameters);
            return;
        }

        /// <summary>
        /// Respond with the loaded template as the page content.
        /// </summary>
        /// <param name="template"></param>
        /// <param name="headers"></param>
        protected void RespondWithPage(Template template, Dictionary<string, string> headers = null)
        {
            // make sure we have a page
            if (template != null)
                worker.RespondWithPage(context, template, pageTitle, requestPath, headers);
        }

        /// <summary>
        /// Responds with a single string as the page result.
        /// </summary>
        /// <param name="str"></param>
        protected void RespondWithString(string str, Dictionary<string, string> headers = null)
        {
            worker.RespondWithString(context, str, headers);
        }
    } // public abstract class IWebPage
} // namespace TridentFramework.RPC.Http
