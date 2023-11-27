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

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;

using TridentFramework.RPC.Http;
using TridentFramework.RPC.Http.Authentication;
using TridentFramework.RPC.Http.Headers;
using TridentFramework.RPC.Http.Routing;

using TridentFramework.RPC.Utility;

namespace TridentFramework.RPC.Http.Service
{
    /// <summary>
    /// This class implements an HTTP request worker, that handles HTTP requests.
    /// </summary>
    public class RequestWorker
    {
        private const string serverVersion = "RPC-EmbeddedHTTP/1.0";

        private Dictionary<Uri, IWebPage> pages;

        private DateTime startTime;

        internal string webroot = "TridentFramework.RPC.Http.Webroot";
        internal Assembly executingAssembly = null;
        internal bool embeddedMode = false;
        internal bool missingWithIndex = false;

        private readonly List<IRouter> routers = new List<IRouter>();

        /*
        ** Properties
        */

        /// <summary>
        /// Gets the authentication provider.
        /// </summary>
        /// <remarks>
        /// A authentication provider is used to keep track of all authentication types
        /// that can be used.
        /// </remarks>
        public AuthenticationProvider AuthenticationProvider { get; private set; }

        /// <summary>
        /// Gets or sets if the request have been handled.
        /// </summary>
        /// <remarks>
        /// The library will not attempt to send the response object
        /// back to the client if this property is set to <c>true</c>.
        /// </remarks>
        public bool IsHandled { get; set; }

        /*
        ** Events
        */

        /// <summary>
        /// Event that occurs on page processing.
        /// </summary>
        public event Func<IHttpListener, IHttpContext, bool> OnProcessRequest;

        /**
         * Internal Methods
         */

        /// <summary>
        /// Internal function to generate the root template variables.
        /// </summary>
        /// <param name="pageTemplate"></param>
        /// <param name="pageTitle"></param>
        /// <param name="requestURI"></param>
        internal void GenerateGlobalVars(Template pageTemplate, string pageTitle, string requestURI)
        {
            // assign root page variables
            pageTemplate.AssignVars(new Dictionary<string, string>()
            {
                { "PAGE_TITLE", pageTitle },
                { "S_CONTENT_ENCODING", "UTF-8" },
            });

            // check if the meta tag was set, if not set it to blank
            if (!pageTemplate.IsVarSet("META"))
                pageTemplate.AssignVar("META", "");
        }

        /// <summary>
        /// Internal function to display the server 404 page.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="requestURI">Page request URI</param>
        internal void Display404(IHttpContext context, string requestURI)
        {
            RPCLogger.Trace("request for [" + requestURI + "] resulted in a server 404!");
            string page = "<html><head><title>Error 404 - Object Not Found</title></head>" +
                "<body><h1>HTTP 404 - Object Not Found</h1><h1 size=\"1\" /><h6>" + serverVersion + "</h6></body>" +
                "</html>";

            RespondWithString(context, page, null, HttpStatusCode.NotFound);
        }

        /// <summary>
        /// Internal function to display the server 500 page.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="requestURI">Page request URI</param>
        /// <param name="throwable">Throwable exception</param>
        internal void Display500(IHttpContext context, string requestURI, Exception throwable)
        {
            RPCLogger.WriteWarning("request for [" + requestURI + "] resulted in a server 500!");
            string page = "<html><head><title>Error 500 - Internal Server Error</title></head>" +
                "<body><h1>HTTP 500 - Internal Server Error</h1>" +
                RPCLogger.HtmlStackTrace(throwable) + "<h1 size=\"1\" /><h6>" + serverVersion + "</h6></body>" +
                "</html>";

            try
            {
                RespondWithString(context, page, null, HttpStatusCode.InternalServerError);
            }
            catch (Exception) { /* do nothing for exception in Display500 */ }
        }

        /// <summary>
        /// Internal function to get the query get parameters.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private Dictionary<string, string> ReturnQueryGet(IHttpContext context)
        {
            Dictionary<string, string> queryParameters = new Dictionary<string, string>(10);

            // iterate through query string filling dictionary
            foreach (IParameter key in context.Request.QueryString)
            {
                // check if we are attempting to use the default null key
                if (key == null)
                {
                    string[] keys = context.Request.QueryString[null].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                    // iterate through keys
                    foreach (string key2 in keys)
                    {
                        // check if the parameters already contain the query key
                        if (!queryParameters.ContainsKey(key2))
                        {
                            RPCLogger.Trace("GET var [" + key2 + "] val [n/a]");
                            queryParameters.Add(key2, "");
                        }
                    }
                }
                else
                {
                    string queryValue = HttpUtility.UrlDecode(context.Request.QueryString.Get(key.Name).Value);

                    // check if the parameters already contain the query key
                    if (!queryParameters.ContainsKey(key.Name))
                        if (queryValue != string.Empty)
                        {
                            RPCLogger.Trace("GET var [" + key.Name + "] val [" + queryValue + "]");
                            queryParameters.Add(key.Name, queryValue);
                        }
                        else
                        {
                            RPCLogger.Trace("GET var [" + key.Name + "] val [n/a]");
                            queryParameters.Add(key.Name, "");
                        }
                    else
                    {
                        string curValue = string.Empty;
                        if (!queryParameters.TryGetValue(key.Name, out curValue))
                            RPCLogger.Trace("tried to get the value for [" + key.Name + "] failed!");

                        curValue += char.ConvertFromUtf32(0x1D161) + queryValue;
                        queryParameters[key.Name] = curValue;
                    }
                }
            }

            return queryParameters;
        }

        /// <summary>
        /// Internal function to get the query post parameters.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private Dictionary<string, string> ReturnQueryPost(IHttpContext context)
        {
            Dictionary<string, string> queryParameters = new Dictionary<string, string>(10);
            string postData = string.Empty;

            // get the raw input data
            using (var reader = new StreamReader(context.Request.Body, context.Request.Encoding))
            {
                postData = reader.ReadToEnd();
            }

            // build arguments array and iterate through it
            string[] arguments = postData.Split(new char[] { '&' });
            foreach (string arg in arguments)
            {
                string key = string.Empty;
                string value = string.Empty;

                // see if argument contains a value
                if (arg.Contains("="))
                {
                    key = arg.Split(new char[] { '=' })[0];
                    value = arg.Split(new char[] { '=' })[1];
                }
                else
                    key = arg;

                // discard empty keys
                if (key == string.Empty)
                    continue;

                // check if the parameters already contain the query key
                if (!queryParameters.ContainsKey(key))
                {
                    RPCLogger.Trace("POST var [" + key + "] val [" + value + "]");
                    queryParameters.Add(key, HttpUtility.UrlDecode(value));
                }
                else
                {
                    string curValue = string.Empty;
                    if (!queryParameters.TryGetValue(key, out curValue))
                        RPCLogger.Trace("tried to get the value for [" + key + "] failed!");

                    curValue += char.ConvertFromUtf32(0x1D161) + HttpUtility.UrlDecode(value);
                    queryParameters[key] = curValue;
                }
            }

            return queryParameters;
        }

        /// <summary>
        /// Static function to test if a multi-value query parameter is set for the given key.
        /// </summary>
        /// <param name="key">Key in query parameter to split</param>
        /// <param name="queryParameters">Query Parameters</param>
        /// <returns>True, if query is multi-value otherwise false.</returns>
        public static bool IsQueryMultiValue(string key, Dictionary<string, string> queryParameters)
        {
            if (queryParameters[key].Contains(char.ConvertFromUtf32(0x1D161)))
                return true;
            return false;
        }

        /// <summary>
        /// Static function to split a multi-value query parameter.
        /// </summary>
        /// <param name="key">Key in query parameter to split</param>
        /// <param name="queryParameters">Query Parameters</param>
        /// <returns>Split array</returns>
        public static string[] SplitMultiValueQuery(string key, Dictionary<string, string> queryParameters)
        {
            string[] str = new string[1];

            // split string
            if (queryParameters[key].Contains(char.ConvertFromUtf32(0x1D161)))
                str = queryParameters[key].Split(new string[] { char.ConvertFromUtf32(0x1D161) }, StringSplitOptions.RemoveEmptyEntries);
            else
                str[0] = queryParameters[key];
            return str;
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of <see cref="RequestWorker"/> class.
        /// </summary>
        public RequestWorker() : this(Environment.CurrentDirectory, null, false, false)
        {
            /* stub */
        }

        /// <summary>
        /// Initializes a new instance of <see cref="RequestWorker"/> class.
        /// </summary>
        /// <param name="webroot"></param>
        /// <param name="executingAssembly"></param>
        /// <param name="embedded"></param>
        /// <param name="missingWithIndex"></param>
        public RequestWorker(string webroot, Assembly executingAssembly, bool embedded, bool missingWithIndex)
        {
            if (executingAssembly == null)
                this.executingAssembly = Assembly.GetExecutingAssembly();
            else
                this.executingAssembly = executingAssembly;
            if (webroot != null)
                this.webroot = webroot;
            this.embeddedMode = embedded;
            this.missingWithIndex = missingWithIndex;

            AuthenticationProvider = new AuthenticationProvider();

            this.pages = new Dictionary<Uri, IWebPage>(10);

            this.IsHandled = false;
        }

        /// <summary>
        /// Helper to add scripted pages.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="page"></param>
        public void AddScriptedPage(Uri path, IWebPage page)
        {
            pages.Add(path, page);
        }

        /// <summary>
        /// Add a new router.
        /// </summary>
        /// <param name="router">Router to add</param>
        public void Add(IRouter router)
        {
            routers.Add(router);
        }

        /// <summary>
        /// Called before anything else.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="request"></param>
        /// <remarks>
        /// Looks after a <see cref="AuthorizationHeader"/> in the request and will
        /// use the <see cref="AuthenticationProvider"/> if found.
        /// </remarks>
        protected virtual void OnAuthentication(IHttpContext context, IRequest request)
        {
            AuthorizationHeader authHeader = (AuthorizationHeader)context.Request.Headers[AuthorizationHeader.NAME];
            if (authHeader != null)
                AuthenticationProvider.Authenticate(context.Request);
        }

        /// <summary>
        /// Requests authentication from the user.
        /// </summary>
        /// <param name="realm">Host/domain name that the server hosts.</param>
        /// <remarks>
        /// Used when calculating hashes in Digest authentication.
        /// </remarks>
        /// <seealso cref="DigestAuthentication"/>
        /// <seealso cref="DigestAuthentication.GetHA1"/>
        protected virtual void RequestAuthentication(string realm)
        {
            AuthenticationProvider.CreateChallenge(HttpContext.Current.Response, realm);
        }

        /// <summary>
        /// This function handles processing the raw HTTP request.
        /// </summary>
        /// <param name="listener"></param>
        /// <param name="args"></param>
        /// <param name="context"></param>
        public void ProcessRequest(IHttpListener listener, RequestEventArgs args, IHttpContext context)
        {
            this.startTime = DateTime.Now;

            string requestPath = context.Request.Uri.AbsolutePath;
            Dictionary<string, string> queryParameters;

            OnAuthentication(context, context.Request);

            try
            {
                foreach (IRouter router in routers)
                {
                    RequestContext ctx = new RequestContext()
                    {
                        HttpContext = context,
                        Request = context.Request,
                        Response = context.Response
                    };

                    if (router.Process(ctx) != ProcessingResult.SendResponse)
                        continue;

                    return;
                }

                // check if we're handling the page via event
                if (OnProcessRequest != null)
                {
                    if (OnProcessRequest(listener, context))
                    {
                        args.IsHandled = IsHandled;
                        return;
                    }
                }

                // get the proper query params based on HTTP method
                switch (context.Request.Method.ToUpper())
                {
                    case Method.Post:
                        queryParameters = ReturnQueryPost(context);
                        break;

                    case Method.Get:
                    default:
                        queryParameters = ReturnQueryGet(context);
                        break;
                }

                // iterate through the pages dictionary
                if (pages.Count > 0)
                {
                    foreach (KeyValuePair<Uri, IWebPage> kvp in pages)
                    {
                        // check for requested page
                        if ((kvp.Key == context.Request.Uri) && (kvp.Value != null))
                        {
                            // attempt to process page request
                            try
                            {
                                // set request path and process request
                                kvp.Value.RequestPath = requestPath.ToLower();
                                kvp.Value.ProcessRequest(listener, context, this, queryParameters);
                                return;
                            }
                            catch (Exception ex)
                            {
                                // caught exception thrown by page, fail miserably
                                RPCLogger.StackTrace(ex, false);
                                Display500(context, requestPath, ex);
                                return;
                            }
                        }
                    }
                }

                // does the requested file live in the server root?
                try
                {
                    string directoryPath = Path.GetDirectoryName(requestPath);
                    string requestFile = Path.GetFileName(requestPath);

                    // build the embedded assembly path and test if resource exists
                    if (embeddedMode)
                    {
                        directoryPath = directoryPath.Replace(Path.DirectorySeparatorChar, '.');
                        if (!directoryPath.EndsWith("."))
                            directoryPath += ".";
#if MONO_PATH_FIX
                        // bryanb: so it seems the fucking god damn .NET vs Mono is different in this regard...
                        // .NET will replace - with _ and Mono leaves them as-is when embedding files into the
                        // executable assembly
                        bool runningOnMono = Type.GetType ("Mono.Runtime") != null;
                        Messages.Trace(string.Format("runningOnMono = " + runningOnMono));
					    if (!runningOnMono && directoryPath.Contains("-"))
#endif
                        directoryPath = directoryPath.Replace('-', '_');

                        string assemblyPath = webroot + directoryPath + requestFile;
                        List<string> embedTest = new List<string>(executingAssembly.GetManifestResourceNames());
                        if (!embedTest.Contains(assemblyPath))
                        {
                            RPCLogger.WriteWarning("unable to locate resource [" + requestFile + "], assemblyPath = " + assemblyPath + ", webrootAssembly = " + executingAssembly.FullName);

                            if (missingWithIndex)
                            {
                                RespondWithPage(context, new Template("index.html"), "Index", "/");
                                return;
                            }
                            else
                            {
                                // fell through, display 404
                                Display404(context, requestPath);
                            }
                        }

                        // process index.html
                        if ((requestPath == "/") || requestPath.StartsWith("/index.html"))
                        {
                            RespondWithPage(context, new Template("index.html"), "Index", "/");
                            return;
                        }
                        else
                        {
                            // resource should exist ... read it from the assembly
                            Stream assemblyStream = executingAssembly.GetManifestResourceStream(assemblyPath);
                            if (assemblyStream != null)
                            {
                                byte[] fileBytes = ReadFully(assemblyStream, (int)assemblyStream.Length);
                                assemblyStream.Close();

                                context.Response.Connection.Type = context.Request.Connection.Type;
                                context.Response.ContentType.Value = ContentTypeHelper.GetType(Path.GetExtension(requestPath).ToLower());

                                // read file and send in response
                                context.Response.ContentLength.Value = fileBytes.Length;
                                context.Response.Body.Write(fileBytes, 0, fileBytes.Length);
                            }
                            else
                                Display404(context, requestPath);
                        }
                    }
                    else
                    {
                        LocalFileLocator locator = new LocalFileLocator(webroot + Path.DirectorySeparatorChar + directoryPath);
                        if (!locator.Exists(requestFile))
                        {
                            RPCLogger.WriteWarning("unable to locate resource [" + requestFile + "], webroot = " + webroot);

                            if (missingWithIndex)
                            {
                                RespondWithPage(context, new Template("index.html"), "Index", "/");
                                return;
                            }
                            else
                            {
                                // fell through, display 404
                                Display404(context, requestPath);
                            }
                        }

                        // process index.html
                        if ((requestPath == "/") || requestPath.StartsWith("/index.html"))
                        {
                            RespondWithPage(context, new Template("index.html"), "Index", "/");
                            return;
                        }
                        else
                        {
                            // resource should exist
                            try
                            {
                                Stream fileStream = locator.Open(requestFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                                if (fileStream != null)
                                {
                                    byte[] fileBytes = ReadFully(fileStream, (int)fileStream.Length);
                                    fileStream.Close();

                                    context.Response.Connection.Type = context.Request.Connection.Type;
                                    context.Response.ContentType.Value = ContentTypeHelper.GetType(Path.GetExtension(requestPath).ToLower());

                                    // read file and send in response
                                    context.Response.ContentLength.Value = fileBytes.Length;
                                    context.Response.Body.Write(fileBytes, 0, fileBytes.Length);
                                }
                                else
                                    Display404(context, requestPath);
                            }
                            catch (Exception)
                            {
                                Display404(context, requestPath); // treat the file as missing -- error 404
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    RPCLogger.StackTrace(e, false);

                    // fell through, display 404
                    Display404(context, requestPath);
                }
            }
            catch (IOException ioe)
            {
                RPCLogger.StackTrace(ioe, false);
            }
            catch (Exception ex)
            {
                RPCLogger.StackTrace(ex, false);
                Display500(context, requestPath, ex);
            }
            return;
        }

        /// <summary>
        /// Helper function to display an HTML page generated from a <see cref="Template"/>.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="pageContent">Template containing the page content</param>
        /// <param name="pageTitle">String containing the page title</param>
        /// <param name="requestURI">Page request URI</param>
        /// <param name="pageHeaders">Headers to append to response</param>
        public void RespondWithPage(IHttpContext context, Template pageContent, string pageTitle = "DID-NOT-SET-PAGE_TITLE", string requestURI = "", Dictionary<string, string> pageHeaders = null)
        {
            if (context == null)
                throw new InvalidOperationException("Cannot respond with no HTTP context");

            // generate global variables
            GenerateGlobalVars(pageContent, pageTitle, requestURI);

            // generate "page load" time
            DateTime endTime = DateTime.Now;
            TimeSpan timeDiff = endTime - startTime;
            pageContent.AssignVar("LOAD_SECONDS", "" + timeDiff.TotalSeconds);

            string compiledTemplate = pageContent.Compile(this);
            if (compiledTemplate == null)
            {
                Display500(context, requestURI, new InvalidOperationException("compiledTemplate == null"));
                return;
            }

            // respond with the given data
            RespondWithString(context, compiledTemplate, pageHeaders);
        }

        /// <summary>
        /// Helper function to display an HTML page contained inside a string builder.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="str">String to respond with</param>
        /// <param name="headers">Headers to append to response</param>
        /// <param name="statusCode"></param>
        /// <param name="contentType"></param>
        /// <param name="compress"></param>
        public void RespondWithString(IHttpContext context, string str, Dictionary<string, string> headers = null,
            HttpStatusCode statusCode = HttpStatusCode.OK, string contentType = null, bool compress = false)
        {
            if (context == null)
                throw new InvalidOperationException("Cannot respond with no incoming HTTP context");

            // convert the entire string builder to a single byte array
            byte[] htmlBytes = Encoding.UTF8.GetBytes(str);

            // prepare and write HTML response page
            try
            {
                context.Response.Status = statusCode;
                context.Response.Connection.Type = (context.Request.Connection != null) ? context.Request.Connection.Type : ConnectionType.KeepAlive;
                if (contentType == null)
                    context.Response.ContentType.Value = ContentTypeHelper.GetType(".html");
                else
                    context.Response.ContentType.Value = contentType;
                context.Response.ContentLength.Value = htmlBytes.Length;
                if (compress)
                {
                    if (context.Request.AcceptEncoding != null)
                    {
                        if (context.Request.AcceptEncoding.Count > 0)
                            context.Response.ContentEncoding = new ContentEncodingHeader(context.Request.AcceptEncoding[0]);
                    }
                }

                RPCLogger.Trace("request " + context.Request.Uri.AbsoluteUri + ", method: " + context.Request.Method);

                // append default headers
                context.Response.Add(new StringHeader("Server", serverVersion));
                context.Response.Add(new StringHeader("X-Powered-By", serverVersion));
                context.Response.Add(new StringHeader("Date", DateTime.Now.ToString("R")));

                // append headers
                if (headers != null)
                {
                    if (headers.Count > 0)
                    {
                        // iterate through header dictionary and add the headers to response
                        foreach (KeyValuePair<string, string> kvp in headers)
                        {
                            RPCLogger.Trace("adding HTTP header [" + kvp.Key + "][" + kvp.Value + "]");
                            if (kvp.Key.ToLower() == "server")
                                continue;
                            if (kvp.Key.ToLower() == "date")
                                continue;
                            context.Response.Add(new StringHeader(kvp.Key, kvp.Value));
                        }
                    }
                }

                context.Response.Body.Write(htmlBytes, 0, htmlBytes.Length);
            }
            catch (ObjectDisposedException)
            {
                RPCLogger.WriteWarning("Context object was disposed when we tried to use it! This shouldn't happen.");
            }
        }

        /**
         * Stream Manipulation Methods
         */

        /// <summary>
        /// Read bytes until buffer filled or EOF.
        /// </summary>
        /// <param name="stream">The stream to read.</param>
        /// <param name="buffer">The buffer to populate.</param>
        /// <param name="offset">Offset in the buffer to start.</param>
        /// <param name="length">The number of bytes to read.</param>
        /// <returns>The number of bytes actually read.</returns>
        private int ReadFully(Stream stream, byte[] buffer, int offset, int length)
        {
            int totalRead = 0;
            int numRead = stream.Read(buffer, offset, length);
            while (numRead > 0)
            {
                totalRead += numRead;
                if (totalRead == length)
                    break;

                numRead = stream.Read(buffer, offset + totalRead, length - totalRead);
            }

            return totalRead;
        }

        /// <summary>
        /// Read bytes until buffer filled or throw IOException.
        /// </summary>
        /// <param name="stream">The stream to read.</param>
        /// <param name="count">The number of bytes to read.</param>
        /// <returns>The data read from the stream.</returns>
        private byte[] ReadFully(Stream stream, int count)
        {
            byte[] buffer = new byte[count];
            if (ReadFully(stream, buffer, 0, count) == count)
                return buffer;
            else
                throw new IOException("Unable to complete read of " + count + " bytes");
        }
    } // public class RequestWorker
} // namespace TridentFramework.RPC.Http
