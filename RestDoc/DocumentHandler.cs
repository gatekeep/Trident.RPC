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
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;

using Newtonsoft.Json.Linq;

using TridentFramework.RPC.Http;
using TridentFramework.RPC.Http.Service;

namespace TridentFramework.RPC.RestDoc
{
    /// <summary>
    /// 
    /// </summary>
    internal enum DocumentType
    {
        /// <summary>
        /// 
        /// </summary>
        Unknown,
        /// <summary>
        /// 
        /// </summary>
        Service,
        /// <summary>
        /// 
        /// </summary>
        Method,
    } // internal enum DocumentType

    /// <summary>
    /// Internal helper to automatically generate documenation for a given REST endpoint.
    /// </summary>
    internal sealed class DocumentHandler
    {
        private const string CssTemplate =
            "body { margin-top: 0px; margin-left: 0px; color: #000; font-family: Verdana; background-color: #fff; }" +
            "p { margin-top: 0px; margin-bottom: 12px; color: #000; font-family: Verdana; }" +
            "pre { border-right: #f0f0e0 1px solid; border-top: #f0f0e0 1px solid; border-left: #f0f0e0 1px solid; border-bottom: #f0f0e0 1px solid; margin-top: -5px; padding: 5px 5px 5px 5px; font-size: 1.2em; font-family: Courier New; background-color: #e5e5cc; }" +
            "#page-content { font-size: 0.7em; padding-bottom: 2em; } " +
            "#api-help { margin-left: 30px; padding-top: 8px; }" +
            ".intro { margin-left: -15px; }" +
            ".main-heading { margin-top: 0px; margin-bottom: 0px; padding-left: 15px; padding-bottom: 10px; padding-top: 10px; width: 100%; font-weight: normal; font-size: 26px; font-family: Tahoma; background-color: #003366; color: #fff; }" +
            ".main-heading a, .main-heading a:active, .main-heading a:visited { color: #fff; }" +
            ".sub-heading { margin-top: 0px; margin-bottom: 0px; padding-left: 15px; padding-bottom: 10px; padding-top: 10px; width: 100%; font-weight: normal; font-size: 18px; font-family: Tahoma; background-color: #23819C; color: #fff; }" +
            ".propertyInfo, dl.parameterList dd { overflow: hidden; word-wrap: break-word; white-space: normal; word-break: break-word; }" +
            "dl.parameterList { display: flex; max-width: 100%; margin: 0; }" +
            ".parameterName, dl.parameterList dt { background-color: #e5e5cc; display: inline-block; padding: 5px 5px 5px 5px; font-size: 1.2em; font-weight: 600; font-family: Courier New; }" +
            "dl.parameterList dd { margin-left: 12px; padding-top: 5px; }" +
            ".propertyInfo+p, .propertyInfo p:first-child, .propertyInfo p:nth-child(2) { margin-top: 2px; }";

        private string documentationEndpoint;
        private RPCProxyHelper proxyHelper;

        private RestService restService;

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentHandler"/> class.
        /// </summary>
        /// <param name="proxyHelper"></param>
        /// <param name="restService"></param>
        /// <param name="docEndpoint"></param>
        public DocumentHandler(RPCProxyHelper proxyHelper, RestService restService, string docEndpoint)
        {
            this.documentationEndpoint = docEndpoint;

            this.proxyHelper = proxyHelper;
            this.restService = restService;
        }

        /// <summary>
        /// Generates the documentation of the given type for the given REST API endpoint.
        /// </summary>
        /// <param name="documentType"></param>
        /// <param name="requestWorker"></param>
        /// <param name="context"></param>
        /// <param name="basePath"></param>
        /// <param name="interfaceType"></param>
        /// <returns></returns>
        public bool Generate(RequestWorker requestWorker, IHttpContext context, Uri basePath, Type interfaceType)
        {
            DocumentType documentType = DocumentType.Service;
            string result = string.Empty;

            // are we trying to access the documentation?
            if (!context.Request.Uri.AbsolutePath.Contains(documentationEndpoint))
                return false;

            // the only parameter passable is the HTTP method
            string httpMethod = context.Request.Uri.GetComponents(UriComponents.Query, UriFormat.SafeUnescaped);
            switch (httpMethod)
            {
                case Method.Get:
                case Method.Post:
                case Method.Put:
                case Method.Delete:
                    break;
                default:
                    httpMethod = context.Request.Method;
                    break;
            }

            httpMethod = httpMethod.ToUpperInvariant();

            UriTemplateMatch utm = null;

            string methodName = string.Empty;
            MethodInfo[] interfaceMethods = proxyHelper.GetInheritanceMethods(interfaceType);
            foreach (MethodInfo info in interfaceMethods)
            {
                RestDocMethodIgnoreAttribute webDocIgnoreAttribute = info.GetCustomAttribute<RestDocMethodIgnoreAttribute>();
                if (webDocIgnoreAttribute != null)
                    continue;

                RestMethodAttribute restInvoke = info.GetCustomAttribute<RestMethodAttribute>();
                if (restInvoke != null)
                {
                    UriTemplate template = new UriTemplate(restInvoke.UriTemplate);

                    string requestUri = context.Request.Uri.AbsoluteUri;
                    requestUri = proxyHelper.BuildUri(context.Request.Uri, context.Request.Uri.Segments.Length - 1).TrimEnd(new char[] { '/' });
                    requestUri = context.Request.Uri.GetComponents(UriComponents.SchemeAndServer, UriFormat.SafeUnescaped) + requestUri;

                    utm = template.Match(basePath, new Uri(requestUri));
                    if (utm != null)
                    {
                        if (restInvoke.Method == httpMethod)
                        {
                            methodName = info.Name;
                            break;
                        }
                    }
                }
            }

            if (methodName != string.Empty)
                documentType = DocumentType.Method;

            try
            {
                switch (documentType)
                {
                    case DocumentType.Service:
                        result = GetServiceDocumentation(interfaceType);
                        requestWorker.RespondWithString(context, result, null, HttpStatusCode.OK, "text/html");
                        break;
                    case DocumentType.Method:
                        result = GetMethodDocumentation(httpMethod, context, basePath, interfaceType, methodName);
                        requestWorker.RespondWithString(context, result, null, HttpStatusCode.OK, "text/html");
                        break;
                    default:
                        requestWorker.Display404(context, string.Empty);
                        return false;
                }
            }
            catch (Exception ex)
            {
                requestWorker.Display500(context, string.Empty, ex);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Get <see cref="UriTemplate"/> for operation.
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        private string GetUriTemplate(MethodInfo info)
        {
            RestMethodAttribute restInvoke = info.GetCustomAttribute<RestMethodAttribute>();
            if (restInvoke != null)
            {
                if (restInvoke.UriTemplate != null)
                    return restInvoke.UriTemplate;
            }

            return string.Empty;
        }

        /// <summary>
        /// Format the given code block
        /// </summary>
        /// <param name="codeBlock"></param>
        /// <returns></returns>
        private static void FormatCodeBlock(ref string codeBlock)
        {
            if (string.IsNullOrEmpty(codeBlock))
                return;

            string id = Guid.NewGuid().ToString();
            codeBlock = WebUtility.HtmlEncode(codeBlock);
            codeBlock = string.Format("<div id=\"{0}\"><pre lang=\"xml\">{1}</pre></div>", id, codeBlock);
        }

        /// <summary>
        /// Get the request example.
        /// </summary>
        /// <param name="httpMethod"></param>
        /// <param name="basePath"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        public string GetRequestExample(string httpMethod, Uri basePath, MethodInfo info)
        {
            try
            {
                string result = string.Format("{2} {0}/{1}", basePath, GetUriTemplate(info), httpMethod.ToUpperInvariant());

                if (httpMethod == Method.Get)
                    return result;
                else
                {
                    result += "\r\n";

                    JObject requestBody = new JObject();
                    ParameterInfo[] parameters = info.GetParameters();
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        ParameterInfo pinfo = parameters[i];
                        Type paramType = pinfo.ParameterType;
                        if (!paramType.IsByRef)
                        {
                            object val = null;
                            if (paramType == typeof(string))
                                val = string.Empty;
                            else
                                val = Activator.CreateInstance(paramType);

                            requestBody.Add(JsonObject.JTokenFromObject(pinfo.Name, val, paramType, false));
                        }
                    }

                    result += requestBody.ToString();
                    return result;
                }
            }
            catch (Exception e)
            {
                return string.Format("Could not generate example for request. Failed with error: {0}", e.Message);
            }
        }

        /// <summary>
        /// Get the request argument details.
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public string GetRequestDetails(MethodInfo info)
        {
            try
            {
                string result = string.Empty;

                List<RestDocMethodArgAttribute> restMethodArgDocAttrs = info.GetCustomAttributes<RestDocMethodArgAttribute>().ToList();

                RestDocMethodAttribute restDocMethodAttr = info.GetCustomAttribute<RestDocMethodAttribute>();
                if (restDocMethodAttr.IgnoreMetadata)
                {
                    foreach (RestDocMethodArgAttribute attr in restMethodArgDocAttrs)
                    {
                        if (attr.OutputArgument)
                            continue;

                        string argDesc = attr.Description;
                        string argType = string.Empty;
                        if (attr.ArgumentType != null)
                            argType = attr.ArgumentType.ToString();

                        string id = Guid.NewGuid().ToString();

                        result += string.Format("<div class=\"propertyInfo\" id=\"{0}\"><dl class=\"parameterList\">", id);
                        result += string.Format("<dt>{0}</dt>", WebUtility.HtmlEncode(attr.Name));
                        result += string.Format("<dd>{0}</dd>", WebUtility.HtmlEncode(argType));
                        result += string.Format("</dl><p>{0}</p>", WebUtility.HtmlEncode(argDesc));
                        result += string.Format("</div>");
                    }

                    return result;
                }

                ParameterInfo[] parameters = info.GetParameters();
                for (int i = 0; i < parameters.Length; i++)
                {
                    ParameterInfo pinfo = parameters[i];

                    string argDesc = string.Empty;
                    if (restMethodArgDocAttrs != null)
                    {
                        RestDocMethodArgAttribute restMethodArgDocAttr = restMethodArgDocAttrs.Find((x) => x.Name == pinfo.Name);
                        if (restMethodArgDocAttr != null)
                            argDesc = restMethodArgDocAttr.Description;
                    }

                    string id = Guid.NewGuid().ToString();

                    result += string.Format("<div class=\"propertyInfo\" id=\"{0}\"><dl class=\"parameterList\">", id);
                    result += string.Format("<dt>{0}</dt>", WebUtility.HtmlEncode(pinfo.Name));
                    result += string.Format("<dd>{0}</dd>", WebUtility.HtmlEncode(pinfo.ParameterType.ToString()));
                    result += string.Format("</dl><p>{0}</p>", WebUtility.HtmlEncode(argDesc));
                    result += string.Format("</div>");
                }

                return result;
            }
            catch (Exception e)
            {
                return string.Format("Could not generate details for request. Failed with error: {0}", e.Message);
            }
        }

        /// <summary>
        /// Get the response example.
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public string GetResponseExample(MethodInfo info)
        {
            try
            {
                RestDocMethodAttribute restDocMethodAttr = info.GetCustomAttribute<RestDocMethodAttribute>();
                if (restDocMethodAttr.IgnoreMetadata)
                    return "No response example available.";

                RestResult<string> result = new RestResult<string>();
                JObject json = (JObject)JsonObject.JTokenFromObject(null, result, typeof(RestResult<string>), false);
                if (json["data"] != null)
                    json.Remove("data");

                JObject dataJson = new JObject();

                // process actual result
                if (info.ReturnType != typeof(void))
                {
                    // handle primitive types
                    object ret = null;
                    if (info.ReturnType == typeof(string))
                        ret = string.Empty;
                    else if (info.ReturnType == typeof(string[]))
                        ret = new string[1] { string.Empty };
                    else
                        ret = Activator.CreateInstance(info.ReturnType);

                    List<Type> intf = new List<Type>(info.ReturnType.GetInterfaces());

                    // wrap the result in our result structure if its a plain unwrapped type
                    if (info.ReturnType != typeof(IRestResult) && !intf.Contains(typeof(IRestResult)))
                        dataJson.Add(JsonObject.JTokenFromObject(info.Name + RPCProxyHelper.RPC_MSG_RSP_SUFFIX, ret, info.ReturnType, false));
                    else
                    {
                        JObject retJson = (JObject)JsonObject.JTokenFromObject(null, ret, info.ReturnType, false);
                        if (retJson["data"] == null)
                            throw new InvalidOperationException("Method return derives from IRestResult but does not have data property?");

                        foreach (JProperty prop in retJson.Properties())
                            dataJson.Add(prop.Name, prop.Value);
                    }
                }

                // process method parameters
                List<Tuple<string, Type>> outTypes = new List<Tuple<string, Type>>();
                ParameterInfo[] parameters = info.GetParameters();
                for (int i = 0; i < parameters.Length; i++)
                {
                    ParameterInfo pinfo = parameters[i];
                    Type paramType = pinfo.ParameterType;
                    if (paramType.IsByRef)
                    {
                        paramType = paramType.GetElementType();
                        outTypes.Add(new Tuple<string, Type>(pinfo.Name, paramType));
                    }
                }

                // process out types
                for (int i = 0; i < outTypes.Count; i++)
                {
                    string typeName = outTypes[i].Item1;
                    Type paramType = outTypes[i].Item2;

                    // handle primitive types
                    object obj = null;
                    if (paramType == typeof(string))
                        obj = string.Empty;
                    else
                        obj = Activator.CreateInstance(paramType);

                    dataJson.Add(JsonObject.JTokenFromObject(typeName, obj, paramType, false));
                }

                json.Add("data", dataJson);

                return json.ToString();
            }
            catch (Exception e)
            {
                return string.Format("Could not generate example for response. Failed with error: {0}", e.Message);
            }
        }

        /// <summary>
        /// Get the response details.
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public string GetResponseDetails(MethodInfo info)
        {
            try
            {
                string result = string.Empty;

                List<RestDocMethodArgAttribute> restMethodArgDocAttr = info.GetCustomAttributes<RestDocMethodArgAttribute>().ToList();

                RestDocMethodAttribute restDocMethodAttr = info.GetCustomAttribute<RestDocMethodAttribute>();
                if (restDocMethodAttr.IgnoreMetadata)
                {
                    RestDocMethodReturnAttribute attr = info.GetCustomAttribute<RestDocMethodReturnAttribute>();
                    if (attr != null)
                    {
                        string retDesc = attr.Description;
                        string retType = string.Empty;
                        if (attr.ReturnType != null)
                            retType = attr.ReturnType.ToString();

                        string id = Guid.NewGuid().ToString();

                        result += string.Format("<div class=\"propertyInfo\" id=\"{0}\"><dl class=\"parameterList\">", id);
                        result += string.Format("<dt>{0}</dt>", WebUtility.HtmlEncode(info.Name + RPCProxyHelper.RPC_MSG_RSP_SUFFIX));
                        result += string.Format("<dd>{0}</dd>", WebUtility.HtmlEncode(retType));
                        result += string.Format("</dl><p>{0}</p>", WebUtility.HtmlEncode(retDesc));
                        result += string.Format("</div>");
                    }

                    foreach (RestDocMethodArgAttribute argAttr in restMethodArgDocAttr)
                    {
                        if (!argAttr.OutputArgument)
                            continue;

                        string argDesc = argAttr.Description;
                        string argType = string.Empty;
                        if (argAttr.ArgumentType != null)
                            argType = argAttr.ArgumentType.ToString();

                        string id = Guid.NewGuid().ToString();

                        result += string.Format("<div class=\"propertyInfo\" id=\"{0}\"><dl class=\"parameterList\">", id);
                        result += string.Format("<dt>{0}</dt>", WebUtility.HtmlEncode(argAttr.Name));
                        result += string.Format("<dd>{0}</dd>", WebUtility.HtmlEncode(argType));
                        result += string.Format("</dl><p>{0}</p>", WebUtility.HtmlEncode(argDesc));
                        result += string.Format("</div>");
                    }

                    return result;
                }

                // process actual result
                if (info.ReturnType != typeof(void))
                {
                    string retDesc = string.Empty;
                    RestDocMethodReturnAttribute restMethodReturnDocAttr = info.GetCustomAttribute<RestDocMethodReturnAttribute>();
                    if (restMethodReturnDocAttr != null)
                        retDesc = restMethodReturnDocAttr.Description;

                    string id = Guid.NewGuid().ToString();

                    result += string.Format("<div class=\"propertyInfo\" id=\"{0}\"><dl class=\"parameterList\">", id);
                    result += string.Format("<dt>{0}</dt>", WebUtility.HtmlEncode(info.Name + RPCProxyHelper.RPC_MSG_RSP_SUFFIX));
                    result += string.Format("<dd>{0}</dd>", WebUtility.HtmlEncode(info.ReturnType.ToString()));
                    result += string.Format("</dl><p>{0}</p>", WebUtility.HtmlEncode(retDesc));
                    result += string.Format("</div>");
                }

                // process method parameters
                ParameterInfo[] parameters = info.GetParameters();
                for (int i = 0; i < parameters.Length; i++)
                {
                    ParameterInfo pinfo = parameters[i];
                    Type paramType = pinfo.ParameterType;
                    if (paramType.IsByRef)
                    {
                        string argDesc = string.Empty;
                        if (restMethodArgDocAttr != null)
                        {
                            RestDocMethodArgAttribute restDocMethodArgAttr = restMethodArgDocAttr.Find((x) => x.Name == pinfo.Name);
                            if (restDocMethodArgAttr != null)
                                argDesc = restDocMethodArgAttr.Description;
                        }

                        string id = Guid.NewGuid().ToString();

                        result += string.Format("<div class=\"propertyInfo\" id=\"{0}\"><dl class=\"parameterList\">", id);
                        result += string.Format("<dt>{0}</dt>", WebUtility.HtmlEncode(pinfo.Name));
                        result += string.Format("<dd>{0}</dd>", WebUtility.HtmlEncode(pinfo.ParameterType.ToString()));
                        result += string.Format("</dl><p>{0}</p>", WebUtility.HtmlEncode(argDesc));
                        result += string.Format("</div>");
                    }
                }

                return result;
            }
            catch (Exception e)
            {
                return string.Format("Could not generate details for response. Failed with error: {0}", e.Message);
            }
        }

        /// <summary>
        /// Get the response return details.
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public string GetResponseReturnDetails(MethodInfo info)
        {
            try
            {
                string result = string.Empty;

                // process a data contract return type
                if (info.ReturnType != typeof(void))
                {
                    DataContractAttribute dataContractAttribute = info.ReturnType.GetCustomAttribute<DataContractAttribute>();
                    if (dataContractAttribute != null)
                    {
                        Type retType = info.ReturnType;

                        // handle fields
                        FieldInfo[] fields = retType.GetFields();
                        for (int i = 0; i < fields.Length; i++)
                        {
                            FieldInfo fieldInfo = fields[i];
                            DataMemberAttribute dataMemberAttribute = fieldInfo.GetCustomAttribute<DataMemberAttribute>();
                            if (dataMemberAttribute != null)
                            {
                                string fieldDesc = string.Empty;
                                RestDocMethodArgAttribute webMethodArgDocAttribute = fieldInfo.GetCustomAttribute<RestDocMethodArgAttribute>();
                                if (webMethodArgDocAttribute != null)
                                    fieldDesc = webMethodArgDocAttribute.Description;

                                string id = Guid.NewGuid().ToString();

                                result += string.Format("<div class=\"propertyInfo\" id=\"{0}\"><dl class=\"parameterList\">", id);
                                result += string.Format("<dt>{0}</dt>", WebUtility.HtmlEncode(fieldInfo.Name));
                                result += string.Format("<dd>{0}</dd>", WebUtility.HtmlEncode(fieldInfo.FieldType.ToString()));
                                result += string.Format("</dl><p>{0}</p>", WebUtility.HtmlEncode(fieldDesc));
                                result += string.Format("</div>");
                            }
                        }

                        // handle properties
                        PropertyInfo[] properties = retType.GetProperties();
                        for (int i = 0; i < properties.Length; i++)
                        {
                            PropertyInfo propInfo = properties[i];
                            DataMemberAttribute dataMemberAttribute = propInfo.GetCustomAttribute<DataMemberAttribute>();
                            if (dataMemberAttribute != null)
                            {
                                string propDesc = string.Empty;
                                RestDocMethodArgAttribute webMethodArgDocAttribute = propInfo.GetCustomAttribute<RestDocMethodArgAttribute>();
                                if (webMethodArgDocAttribute != null)
                                    propDesc = webMethodArgDocAttribute.Description;

                                string id = Guid.NewGuid().ToString();

                                result += string.Format("<div class=\"propertyInfo\" id=\"{0}\"><dl class=\"parameterList\">", id);
                                result += string.Format("<dt>{0}</dt>", WebUtility.HtmlEncode(propInfo.Name));
                                result += string.Format("<dd>{0}</dd>", WebUtility.HtmlEncode(propInfo.PropertyType.ToString()));
                                result += string.Format("</dl><p>{0}</p>", WebUtility.HtmlEncode(propDesc));
                                result += string.Format("</div>");
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception e)
            {
                return string.Format("Could not generate details for response return type. Failed with error: {0}", e.Message);
            }
        }

        /// <summary>
        /// Builds the service's documentation.
        /// </summary>
        /// <param name="interfaceType"></param>
        /// <returns></returns>
        private string GetServiceDocumentation(Type interfaceType)
        {
            XmlDocument doc = new XmlDocument();

            XmlDeclaration declaration = doc.CreateXmlDeclaration("1.0", "utf-8", null);
            doc.AppendChild(declaration);
            
            RestDocServiceAttribute docAttribute = interfaceType.GetCustomAttribute<RestDocServiceAttribute>();
            string pageTitle = docAttribute == null || string.IsNullOrEmpty(docAttribute.Title) ? string.Format("{0} REST API Documentation", interfaceType.FullName) : docAttribute.Title;

            XmlElement root = doc.CreateElement("html");
            XmlElement title = doc.CreateElement("title");
            XmlElement head = doc.CreateElement("head");
            XmlElement style = doc.CreateElement("style");
            XmlAttribute type = doc.CreateAttribute("type", "text/css");
            style.Attributes.Append(type);
            style.InnerText = CssTemplate;
            title.InnerXml = WebUtility.HtmlEncode(pageTitle);

            XmlElement body = doc.CreateElement("body");
            StringBuilder bodyXml = new StringBuilder();

            bodyXml.Append("<div id=\"page-content\">");
            {
                bodyXml.AppendFormat("<p class=\"main-heading\">{0}</p>", WebUtility.HtmlEncode(pageTitle));
                bodyXml.Append("<div id=\"api-help\">");
                {
                    if (docAttribute != null)
                    {
                        if (!string.IsNullOrEmpty(docAttribute.Description))
                            bodyXml.AppendFormat("<p class=\"intro\">{0}</p>", docAttribute.Description);
                    }

                    MethodInfo[] interfaceMethods = proxyHelper.GetInheritanceMethods(interfaceType);
                    foreach (MethodInfo info in interfaceMethods)
                    {
                        RestDocMethodIgnoreAttribute restDocIgnoreAttr = info.GetCustomAttribute<RestDocMethodIgnoreAttribute>();
                        if (restDocIgnoreAttr != null)
                            continue;

                        RestDocMethodAttribute restDocMethodAttr = info.GetCustomAttribute<RestDocMethodAttribute>();
                        RestMethodAttribute restInvoke = info.GetCustomAttribute<RestMethodAttribute>();
                        if (restInvoke != null)
                        {
                            string uriMethod = restInvoke.UriTemplate;
                            if (uriMethod.Contains("?"))
                                uriMethod = uriMethod.Substring(0, uriMethod.IndexOf('?'));

                            bodyXml.AppendFormat("<p class=\"intro\"><pre><b>{4}</b> {0} : <a href=\"{1}{3}?{4}\">{2}</a>", WebUtility.HtmlEncode(restDocMethodAttr != null ? restDocMethodAttr.Name : info.Name),
                                WebUtility.HtmlEncode(uriMethod), WebUtility.HtmlEncode(restInvoke.UriTemplate), WebUtility.HtmlEncode(documentationEndpoint),
                                restInvoke.Method.ToUpperInvariant());
                            bodyXml.AppendFormat("</pre></p>");
                        }
                    }

                    // display other endpoints (if any)
                    if (restService.ServiceEndpoints != null)
                    {
                        if (restService.ServiceEndpoints.Count > 1)
                        {
                            bodyXml.AppendLine("<hr size=\"1\" />");

                            bodyXml.AppendFormat("<p class=\"intro\"><b>Published Services:</b><br />");

                            foreach (KeyValuePair<string, Type> services in restService.ServiceEndpoints)
                            {
                                Type serviceType = services.Value;
                                docAttribute = serviceType.GetCustomAttribute<RestDocServiceAttribute>();
                                if (docAttribute != null)
                                {
                                    bodyXml.AppendFormat("<p class=\"intro\"><pre><a href=\"{1}{2}\">{0}</a>", WebUtility.HtmlEncode(docAttribute.Title != null ? docAttribute.Title : serviceType.Name),
                                        WebUtility.HtmlEncode(services.Key), WebUtility.HtmlEncode(documentationEndpoint));
                                    bodyXml.AppendFormat("</pre></p>");
                                }
                                else
                                {
                                    bodyXml.AppendFormat("<p class=\"intro\"><pre><a href=\"{1}{2}\">{0}</a>", WebUtility.HtmlEncode(serviceType.Name),
                                        WebUtility.HtmlEncode(services.Key), WebUtility.HtmlEncode(documentationEndpoint));
                                    bodyXml.AppendFormat("</pre></p>");
                                }
                            }

                            bodyXml.Append("</p>");
                        }
                    }
                }
                bodyXml.AppendFormat("</div>");
            }
            bodyXml.AppendFormat("</div>");

            body.InnerXml = bodyXml.ToString();

            head.AppendChild(style);
            head.AppendChild(title);
            root.AppendChild(head);
            root.AppendChild(body);
            doc.AppendChild(root);

            return doc.OuterXml;
        }

        /// <summary>
        /// Builds the method's documentation.
        /// </summary>
        /// <param name="httpMethod"></param>
        /// <param name="context"></param>
        /// <param name="basePath"></param>
        /// <param name="interfaceType"></param>
        /// <param name="methodName"></param>
        /// <returns></returns>
        private string GetMethodDocumentation(string httpMethod, IHttpContext context, Uri basePath, Type interfaceType, string methodName)
        {
            MethodInfo info = proxyHelper.GetInheritanceMethod(methodName, interfaceType);

            XmlDocument doc = new XmlDocument();

            XmlDeclaration declaration = doc.CreateXmlDeclaration("1.0", "utf-8", null);
            doc.AppendChild(declaration);

            // fetch end point for the service hosting this method
            string serviceEndpoint = "#";
            if (restService.ServiceEndpoints != null)
            {
                if (restService.ServiceEndpoints.Count > 1)
                {
                    foreach (KeyValuePair<string, Type> services in restService.ServiceEndpoints)
                    {
                        if (services.Value == interfaceType)
                        {
                            serviceEndpoint = services.Key;
                            break;
                        }
                    }
                }
            }

            RestDocServiceAttribute docAttribute = interfaceType.GetCustomAttribute<RestDocServiceAttribute>();
            string pageTitle = docAttribute == null || string.IsNullOrEmpty(docAttribute.Title) ? string.Format("{0} REST API Documentation", interfaceType.FullName) : docAttribute.Title;

            XmlElement root = doc.CreateElement("html");
            XmlElement title = doc.CreateElement("title");
            XmlElement head = doc.CreateElement("head");
            XmlElement style = doc.CreateElement("style");
            XmlAttribute type = doc.CreateAttribute("type", "text/css");
            style.Attributes.Append(type);
            style.InnerText = CssTemplate;
            title.InnerXml = WebUtility.HtmlEncode(pageTitle);

            XmlElement body = doc.CreateElement("body");
            StringBuilder bodyXml = new StringBuilder();

            bodyXml.Append("<div id=\"page-content\">");
            {
                RestDocMethodAttribute methodDocAttribute = info.GetCustomAttribute<RestDocMethodAttribute>();

                bodyXml.AppendFormat("<p class=\"main-heading\"><a href=\"{1}{2}\">{0}</a></p>", pageTitle, WebUtility.HtmlEncode(serviceEndpoint), WebUtility.HtmlEncode(documentationEndpoint));
                bodyXml.AppendFormat("<p class=\"sub-heading\">{0}</p>",
                    methodDocAttribute != null ? string.Format("<b>{0}</b> &bull; {1}", WebUtility.HtmlEncode(methodDocAttribute.Name), WebUtility.HtmlEncode(info.Name)) :
                                                        string.Format("<b>{0}</b>", WebUtility.HtmlEncode(info.Name)));

                bodyXml.Append("<div id=\"api-help\">");
                {
                    if (methodDocAttribute != null)
                    {
                        if (!string.IsNullOrEmpty(methodDocAttribute.Description))
                            bodyXml.AppendFormat("<p class=\"intro\">{0}</p>", WebUtility.HtmlEncode(methodDocAttribute.Description));
                    }

                    RestMethodAttribute restInvoke = info.GetCustomAttribute<RestMethodAttribute>();
                    if (restInvoke != null)
                    {
                        string requestExample = GetRequestExample(restInvoke.Method, basePath, info);
                        FormatCodeBlock(ref requestExample);

                        string requestDetails = GetRequestDetails(info);

                        bodyXml.AppendFormat("<p class=\"intro\"><b>Request:</b><br />");
                        bodyXml.AppendLine(requestExample);
                        bodyXml.Append("</p>");

                        if (requestDetails != string.Empty)
                        {
                            bodyXml.AppendFormat("<p class=\"intro\"><b>Parameters:</b><br />");
                            bodyXml.AppendLine(requestDetails);
                            bodyXml.Append("</p>");
                        }

                        bodyXml.AppendLine("<hr size=\"1\" />");

                        string responseExample = GetResponseExample(info);
                        FormatCodeBlock(ref responseExample);

                        string responseDetails = GetResponseDetails(info);

                        bodyXml.AppendFormat("<p class=\"intro\"><b>Response:</b><br />");
                        bodyXml.AppendLine(responseExample);
                        bodyXml.AppendLine("</p>");

                        if (responseDetails != string.Empty)
                        {
                            bodyXml.AppendFormat("<p class=\"intro\"><b>Returns:</b><br />");
                            bodyXml.AppendLine(responseDetails);
                            bodyXml.Append("</p>");
                        }

                        string responseReturnDetails = GetResponseReturnDetails(info);
                        if (responseReturnDetails != string.Empty)
                        {
                            bodyXml.AppendLine("<hr size=\"1\" />");

                            bodyXml.AppendFormat("<p class=\"intro\"><b>{0} Type Description:</b><br />", WebUtility.HtmlEncode(info.ReturnType.ToString()));
                            bodyXml.AppendLine(responseReturnDetails);
                            bodyXml.Append("</p>");
                        }
                    }
                }
                bodyXml.AppendLine("</div>");
            }
            bodyXml.AppendLine("</div>");

            body.InnerXml = bodyXml.ToString();

            head.AppendChild(style);
            root.AppendChild(head);
            root.AppendChild(body);
            doc.AppendChild(root);

            return doc.OuterXml;
        }
    } // internal sealed class DocumentHandler   
} // namespace TridentFramework.RPC.RestDoc
