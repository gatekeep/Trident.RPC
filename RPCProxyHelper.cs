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

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using TridentFramework.RPC.Http;
using TridentFramework.RPC.Http.Service;
using TridentFramework.RPC.Remoting;

namespace TridentFramework.RPC
{
    /// <summary>
    /// Defines an RPC "channel" or client connection to a RPC service.
    /// </summary>
    internal class RPCProxyHelper
    {
        internal const string RPC_MSG_PROP_REQ_ID = "RequestId";
        internal const string RPC_MSG_PROP_CONN_ID = "ConnectionId";
        internal const string RPC_MSG_PROP_OP_NAME = "OperationName";

        internal const string RPC_MSG_PROP_HTTP_REQUEST_WORKER = "RequestWorker";
        internal const string RPC_MSG_PROP_HTTP_REQUEST_METHOD = "HttpRequestMethod";
        internal const string RPC_MSG_PROP_HTTP_QUERY_TEMPLATE = "HttpQueryTemplate";
        internal const string RPC_MSG_PROP_HTTP_CONTEXT = "HttpContext";
        internal const string RPC_MSG_PROP_HTTP_REQUEST = "HttpRequest";

        private const string RPC_MSG_FAULT = "FaultResponse";
        internal const string RPC_MSG_RSP_SUFFIX = "Result";
        private const string RPC_MSG_INARG_COUNT = "inArgs";
        private const string RPC_MSG_OUTARG_COUNT = "outArgs";
        private const string RPC_MSG_ARGS = "args";
        private const string RPC_MSG_RET = "ret";

        private const string RPC_PARAM_ARGS = "args";
        private const string RPC_PARAM_VAL = "value";
        private const string RPC_PARAM_OAQN = "oaqn";
        private const string RPC_PARAM_NAME = "name";
        private const string RPC_PARAM_AQN = "aqn";
        private const string RPC_PARAM_IN = "in";
        private const string RPC_PARAM_OUT = "out";

        private const string FAULT_RSP_PARAM_MSG = "message";
        private const string FAULT_RSP_PARAM_ACTOR = "actor";
        private const string FAULT_RSP_PARAM_DETAIL = "detail";

        internal static bool CaseInsensitiveRESTURIs = true;

        /*
        ** Properties
        */

        /// <summary>
        /// Gets or sets the instance of the <see cref="IAuthenticationGuard"/> for this RPC channel.
        /// </summary>
        public IAuthenticationGuard AuthenticationGuard
        {
            get; set;
        }

        /*
        ** Classes
        */

        /// <summary>
        ///
        /// </summary>
        private class DefaultAuthGuard : IAuthenticationGuard
        {
            /**
             * Methods
             */

            /// <summary>
            /// Checks authentication for the given operation when access to a message is required.
            /// </summary>
            /// <param name="context"></param>
            /// <param name="message"></param>
            /// <returns>True, if access is granted, otherwise false.</returns>
            public bool CheckAccess(RPCContext context, ref RPCMessage message)
            {
                return true;
            }
        } // private class DefaultAuthGuard : IAuthenticationGuard

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="RPCProxyHelper"/> class.
        /// </summary>
        public RPCProxyHelper()
        {
            this.AuthenticationGuard = new DefaultAuthGuard();
        }

        /// <summary>
        /// Helper to build the URI from the given number of total segments.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="segments"></param>
        /// <returns></returns>
        public string BuildUri(Uri uri, int segments)
        {
            string ret = string.Empty;
            for (int i = 0; i < segments; i++)
                ret += uri.Segments[i];
            return ret;
        }

        /// <summary>
        /// Helper to get the base path, interface type and service type for a request URI.
        /// </summary>
        /// <param name="fullRequestUri"></param>
        /// <param name="serviceEndpoints"></param>
        /// <param name="serviceTypes"></param>
        /// <param name="basePath"></param>
        /// <param name="interfaceType"></param>
        /// <param name="serviceType"></param>
        /// <param name="stripUriEnd"></param>
        public void GetUriAndTypes(Uri fullRequestUri, Dictionary<string, Type> serviceEndpoints, Dictionary<Type, Type> serviceTypes, out Uri basePath, out Type interfaceType, out Type serviceType, string stripUriEnd = null)
        {
            interfaceType = null;
            serviceType = null;

            Uri schemeAndServer = new Uri(fullRequestUri.GetComponents(UriComponents.SchemeAndServer, UriFormat.SafeUnescaped));

            string uriBasePath = string.Empty;
            for (int i = fullRequestUri.Segments.Length; i > 0; i--)
            {
                uriBasePath = BuildUri(fullRequestUri, i);

                if (stripUriEnd != null)
                {
                    if (uriBasePath.Contains(stripUriEnd))
                        uriBasePath = uriBasePath.Replace(stripUriEnd, string.Empty);
                }

                // are we treating all URIs case-insensitive?
                if (CaseInsensitiveRESTURIs)
                {
                    uriBasePath = uriBasePath.ToLowerInvariant();
                    foreach (string endpoint in serviceEndpoints.Keys)
                    {
                        if (endpoint.ToLowerInvariant() == uriBasePath.TrimEnd(new char[] { '/' }))
                        {
                            if (!serviceEndpoints.TryGetValue(endpoint, out interfaceType))
                                throw new InvalidOperationException("Service Uri endpoint invalid. No service endpoint defined for specified Uri: " + uriBasePath.Trim(new char[] { '/' }));
                        }
                    }
                }
                else
                {
                    // handle URIs directly with case sensitivity
                    if (serviceEndpoints.ContainsKey(uriBasePath.TrimEnd(new char[] { '/' })))
                        if (!serviceEndpoints.TryGetValue(uriBasePath.TrimEnd(new char[] { '/' }), out interfaceType))
                            throw new InvalidOperationException("Service Uri endpoint invalid. No service endpoint defined for specified Uri: " + uriBasePath.Trim(new char[] { '/' }));
                }

                if (interfaceType == null)
                    continue;
                else
                    break;
            }

            uriBasePath = uriBasePath.TrimEnd(new char[] { '/' });
            if (interfaceType == null)
                throw new InvalidOperationException("Service Uri endpoint invalid. No service interface for specified Uri: " + uriBasePath);

            basePath = new Uri(schemeAndServer, uriBasePath);

            if (serviceEndpoints.ContainsKey(basePath.AbsolutePath))
                if (!serviceEndpoints.TryGetValue(basePath.AbsolutePath, out interfaceType))
                    throw new InvalidOperationException("Service Uri endpoint invalid. No service endpoint defined for specified Uri: " + basePath.AbsolutePath);

            if (serviceTypes.ContainsKey(interfaceType))
                if (!serviceTypes.TryGetValue(interfaceType, out serviceType))
                    throw new InvalidOperationException("serviceTypes");
            if (serviceType == null)
                throw new InvalidOperationException("serviceType");
        }

        /// <summary>
        /// Helper to get the methods of the given type and all inherited interfaces/classes.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="includeInternal"></param>
        /// <returns></returns>
        public MethodInfo[] GetInheritanceMethods(Type type, bool includeInternal = false)
        {
            List<MethodInfo> methods = null;
            if (!includeInternal)
                methods = new List<MethodInfo>(type.GetMethods(BindingFlags.Instance | BindingFlags.Public));
            else
                methods = new List<MethodInfo>(type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
            foreach (Type t in type.GetInterfaces())
            {
                if (!includeInternal)
                    methods.AddRange(t.GetMethods(BindingFlags.Instance | BindingFlags.Public));
                else
                    methods.AddRange(t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
                if (t.GetInterfaces().Length > 0)
                    methods.AddRange(GetInheritanceMethods(t));
            }
            return methods.ToArray();
        }

        /// <summary>
        /// Helper to get the given method from the given type and all inherited interfaces/classes.
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="type"></param>
        /// <param name="includeInternal"></param>
        /// <returns></returns>
        public MethodInfo GetInheritanceMethod(string methodName, Type type, bool includeInternal = false)
        {
            return GetInheritanceMethod(methodName, GetInheritanceMethods(type, includeInternal));
        }

        /// <summary>
        /// Helper to get the given method from the given type and all inherited interfaces/classes.
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="interfaceMethods"></param>
        /// <returns></returns>
        public MethodInfo GetInheritanceMethod(string methodName, MethodInfo[] interfaceMethods)
        {
            foreach (MethodInfo info in interfaceMethods)
                if (info.Name == methodName)
                    return info;
            return null;
        }

        /// <summary>
        /// Internal helper to send an RPC fault result back to the service.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="methodInfo"></param>
        /// <param name="ex"></param>
        /// <returns></returns>
        public JObject PrepareRPCFaultResponse(RPCMessage message, MethodInfo methodInfo, Exception ex)
        {
            if (message.MessageType != RPCMessageType.Json_RPC)
                throw new ArgumentException("message");

            JObject json = new JObject();

            // generate fault element
            JObject fault = new JObject();
            if (ex is TargetInvocationException && ex.InnerException != null)
                fault.Add(FAULT_RSP_PARAM_MSG, ex.InnerException.Message);
            else
                fault.Add(FAULT_RSP_PARAM_MSG, ex.Message);
            if (methodInfo != null)
                fault.Add(FAULT_RSP_PARAM_ACTOR, methodInfo.Name);

            if (ex != null)
            {
                // use the detail tag to contain our exception serialization data
                BinaryFormatter serializer = new BinaryFormatter();
                using (MemoryStream memStream = new MemoryStream())
                {
                    if (ex is TargetInvocationException && ex.InnerException != null)
                        serializer.Serialize(memStream, ex.InnerException);
                    else
                        serializer.Serialize(memStream, ex);
                    memStream.Position = 0;
                    fault.Add(FAULT_RSP_PARAM_DETAIL, Convert.ToBase64String(memStream.ToArray()));
                }
            }

            json.Add(RPC_MSG_FAULT, fault);

            return json;
        }

        /// <summary>
        /// Internal helper to send an REST fault result back to the service.
        /// </summary>
        /// <param name="methodInfo"></param>
        /// <param name="ex"></param>
        /// <returns></returns>
        public JObject PrepareRESTFaultResponse(MethodInfo methodInfo, Exception ex)
        {
            RestResult<string> result = new RestResult<string>();
            result.Success = false;

            JObject json = (JObject)JsonObject.JTokenFromObject(null, result, typeof(RestResult<string>), false);
            if (json["data"] != null)
                json.Remove("data");

            JObject dataJson = new JObject();
            if (ex is TargetInvocationException && ex.InnerException != null)
                dataJson.Add(FAULT_RSP_PARAM_MSG, ex.InnerException.Message);
            else
                dataJson.Add(FAULT_RSP_PARAM_MSG, ex.Message);
            if (methodInfo != null)
                dataJson.Add(FAULT_RSP_PARAM_ACTOR, methodInfo.Name);

            if (ex != null)
            {
                string trace = string.Empty;
                if (ex.StackTrace != null)
                {
                    foreach (string str in ex.StackTrace.Split(new string[] { Environment.NewLine }, StringSplitOptions.None))
                        trace += str + "\n";
                }

                if (ex.InnerException != null)
                {
                    if (ex.InnerException.StackTrace != null)
                    {
                        foreach (string str in ex.InnerException.StackTrace.Split(new string[] { Environment.NewLine }, StringSplitOptions.None))
                            trace += "\ninner trace: " + str;
                    }
                }
                dataJson.Add(FAULT_RSP_PARAM_DETAIL, trace);
            }

            json.Add("data", dataJson);

            return json;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="json"></param>
        /// <param name="faultProperty"></param>
        /// <param name="throwEx"></param>
        /// <param name="ex"></param>
        /// <returns></returns>
        public bool ProcessFaultResponse(JObject json, out JProperty faultProperty, bool throwEx, out Exception ex)
        {
            ex = null;

            // test if the first element is a fault
            faultProperty = json.First as JProperty;
            if (faultProperty != null)
            {
                if (faultProperty.Name == RPC_MSG_FAULT)
                {
                    JObject fault = faultProperty.Value as JObject;

                    // get serialized exception (if any)
                    try
                    {
                        if (fault[FAULT_RSP_PARAM_DETAIL] != null)
                        {
                            byte[] data = Convert.FromBase64String(fault["detail"].Value<string>());

                            BinaryFormatter serializer = new BinaryFormatter();
                            using (MemoryStream memStream = new MemoryStream(data))
                            {
                                // deserialize the fault and rethrow
                                ex = (Exception)serializer.Deserialize(memStream);
                            }
                        }
                    }
                    catch (Exception faultEx) { ex = faultEx; }

                    string message = "Unknown exception has occurred.";
                    if (fault[FAULT_RSP_PARAM_MSG] != null)
                        message = fault[FAULT_RSP_PARAM_MSG].Value<string>();

                    string actor = string.Empty;
                    if (fault[FAULT_RSP_PARAM_ACTOR] != null)
                        actor = fault[FAULT_RSP_PARAM_ACTOR].Value<string>();

                    // just send this fault back to the client (we don't process them)
                    if (throwEx)
                        throw new RPCException(string.Format("RPC exception has occurred. {0} {1}", message, actor), ex);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="paramType"></param>
        /// <param name="originalType"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        private bool HandleFromDictionary(ref Type paramType, out Type originalType, ref object obj)
        {
            originalType = paramType;

            if (paramType.GetInterface(typeof(IDictionary).Name) != null)
            {
                if (paramType.IsConstructedGenericType)
                {
                    // do we have more then 2 generics? (should not happen)
                    if (paramType.GenericTypeArguments.Length > 2)
                        throw new InvalidOperationException("Shouldn't have more then 2 generic type arguments .. but we do");

                    Type key = paramType.GenericTypeArguments[0];
                    Type value = paramType.GenericTypeArguments[1];
                    Type dictType = typeof(DictionarySerializable<,>).MakeGenericType(new Type[] { key, value });

                    var instance = Activator.CreateInstance(dictType);
                    MethodInfo fromMethod = instance.GetType().GetMethod("FromDictionary");
                    if (obj != null) // ?? -- why? this will cause a return of non-null where a null was returned, infact it will return an empty length XmlDictionarySerializable<,>
                        instance = fromMethod.Invoke(instance, new object[] { obj });

                    obj = null;
                    obj = instance;

                    paramType = dictType;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="paramType"></param>
        /// <param name="originalType"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        private bool HandleToDictionary(ref Type paramType, out Type originalType, ref object obj)
        {
            originalType = paramType;

            // we have to do some mangling of the system dictionary type because its unsupported
            if (paramType.GetInterface(typeof(IDictionary).Name) != null)
            {
                if (paramType.IsConstructedGenericType)
                {
                    // do we have more then 2 generics? (should not happen)
                    if (paramType.GenericTypeArguments.Length > 2)
                        throw new InvalidOperationException("Shouldn't have more then 2 generic type arguments .. but we do");

                    Type key = paramType.GenericTypeArguments[0];
                    Type value = paramType.GenericTypeArguments[1];
                    Type dictType = typeof(Dictionary<,>).MakeGenericType(new Type[] { key, value });

                    var instance = Activator.CreateInstance(dictType);
                    MethodInfo toMethod = obj.GetType().GetMethod("ToDictionary");
                    if (obj != null) // ?? -- why? this will cause a return of non-null where a null was returned, infact it will return an empty length Dictionary<,>
                        instance = toMethod.Invoke(instance, new object[] { obj });

                    obj = null;
                    obj = instance;

                    paramType = dictType;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Helper to prepare a RPC request for transmission to the server.
        /// </summary>
        /// <param name="targetMethod"></param>
        /// <param name="mapper"></param>
        /// <param name="ins"></param>
        /// <param name="outs"></param>
        /// <returns></returns>
        public JObject PrepareRPCRequest(MethodInfo targetMethod, MethodMapper mapper, object[] ins, object[] outs)
        {
            if (ins.Length != mapper.InArgs.Length)
                throw new InvalidOperationException();
            if (outs.Length != mapper.OutArgs.Length)
                throw new InvalidOperationException();

            JObject json = new JObject();

            // generate method element
            JObject methodElement = new JObject();
            methodElement.Add(RPC_MSG_INARG_COUNT, mapper.InArgs.Length);
            methodElement.Add(RPC_MSG_OUTARG_COUNT, mapper.OutArgs.Length);

            JArray arguments = new JArray();

            // generate children for the methods input parameters
            for (int i = 0; i < mapper.InArgs.Length; i++)
            {
                ParameterInfo pinfo = mapper.InArgs[i];
                Type paramType = pinfo.ParameterType;
                object obj = ins[i];

                JObject paramElement = new JObject();

                // we have to do some mangling of the system dictionary type because its unsupported
                Type originalType = null;
                if (HandleFromDictionary(ref paramType, out originalType, ref obj))
                    paramElement.Add(RPC_PARAM_OAQN, originalType.AssemblyQualifiedName);

                paramElement.Add(RPC_PARAM_NAME, pinfo.Name);
                paramElement.Add(RPC_PARAM_AQN, paramType.AssemblyQualifiedName);
                paramElement.Add(RPC_PARAM_IN, true);
                paramElement.Add(JsonObject.JTokenFromObject(RPC_PARAM_VAL, obj, paramType, true));

                arguments.Add(new JObject(paramElement));
            }

            // generate children for the methods output parameters
            JArray outArguments = new JArray();
            for (int i = 0; i < mapper.OutArgs.Length; i++)
            {
                ParameterInfo pinfo = mapper.OutArgs[i];
                Type paramType = pinfo.ParameterType.GetElementType();

                JObject paramElement = new JObject();

                paramElement.Add(RPC_PARAM_NAME, pinfo.Name);
                paramElement.Add(RPC_PARAM_AQN, paramType.AssemblyQualifiedName);
                paramElement.Add(RPC_PARAM_OUT, true);

                arguments.Add(new JObject(paramElement));
            }

            methodElement.Add(RPC_MSG_ARGS, arguments);

            // append method element to JSON body
            json.Add(targetMethod.Name, methodElement);
            return json;
        }

        /// <summary>
        /// Helper to prepare a RPC result to send back to the caller.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="methodInfo"></param>
        /// <param name="ret"></param>
        /// <param name="outTypes"></param>
        /// <param name="outs"></param>
        /// <returns></returns>
        public JObject PrepareRPCResult(RPCContext context, MethodInfo methodInfo, object ret, List<Tuple<string, Type>> outTypes, object[] outs)
        {
            if (context.Message == null)
                throw new ArgumentException("context");
            if (context.Message.MessageType != RPCMessageType.Json_RPC)
                throw new ArgumentException("context");

            JObject json = new JObject();

            // generate method element
            JObject methodElement = new JObject();
            methodElement.Add(RPC_MSG_OUTARG_COUNT, outs.Length);

            JArray arguments = new JArray();

            // generate children for the methods output parameters
            for (int i = 0; i < outs.Length; i++)
            {
                string paramName = outTypes[i].Item1; // item 1 in the tuple is our parameter name
                Type paramType = outTypes[i].Item2; // item 2 in the tuple is our parameter type
                object obj = outs[i];

                JObject paramElement = new JObject();

                // we have to do some mangling of the system dictionary type because its unsupported
                Type originalType = null;
                if (HandleFromDictionary(ref paramType, out originalType, ref obj))
                    paramElement.Add(RPC_PARAM_OAQN, originalType.AssemblyQualifiedName);

                paramElement.Add(RPC_PARAM_NAME, paramName);
                paramElement.Add(RPC_PARAM_AQN, paramType.AssemblyQualifiedName);
                paramElement.Add(RPC_PARAM_OUT, true);
                paramElement.Add(JsonObject.JTokenFromObject(RPC_PARAM_VAL, obj, paramType, true));

                arguments.Add(new JObject(paramElement));
            }

            methodElement.Add(RPC_MSG_ARGS, arguments);

            // generate return result
            JObject returnElement = new JObject();

            Type returnType = methodInfo.ReturnType;
            object retObj = ret;

            // we have to do some mangling of the system dictionary type because its unsupported
            if (returnType.ReflectedType != typeof(void))
            {
                Type originalType = null;
                if (HandleFromDictionary(ref returnType, out originalType, ref retObj))
                    returnElement.Add(RPC_PARAM_OAQN, originalType.AssemblyQualifiedName);
            }

            returnElement.Add(RPC_PARAM_AQN, returnType.AssemblyQualifiedName);
            if (methodInfo.ReturnType != typeof(void))
                returnElement.Add(JsonObject.JTokenFromObject(RPC_PARAM_VAL, retObj, returnType, true));

            methodElement.Add(RPC_MSG_RET, returnElement);

            // append method element to JSON body
            json.Add(methodInfo.Name + RPC_MSG_RSP_SUFFIX, methodElement);
            return json;
        }

        /// <summary>
        /// Helper to prepare a REST result to send back to the caller.
        /// </summary>
        /// <param name="methodInfo"></param>
        /// <param name="ret"></param>
        /// <param name="outTypes"></param>
        /// <param name="outs"></param>
        /// <returns></returns>
        public string PrepareRESTResult(MethodInfo methodInfo, object ret, List<Tuple<string, Type>> outTypes, object[] outs)
        {
            bool wrappedType = false;
            RestResult<string> result = new RestResult<string>();
            JObject json = (JObject)JsonObject.JTokenFromObject(null, result, typeof(RestResult<string>), false);
            if (json["data"] != null)
                json.Remove("data");

            JObject dataJson = new JObject();

            // process actual result
            if (methodInfo.ReturnType != typeof(void))
            {
                List<Type> intf = new List<Type>(methodInfo.ReturnType.GetInterfaces());

                // wrap the result in our result structure if its a plain unwrapped type
                if (methodInfo.ReturnType != typeof(IRestResult) && !intf.Contains(typeof(IRestResult)))
                    dataJson.Add(JsonObject.JTokenFromObject(methodInfo.Name + RPC_MSG_RSP_SUFFIX, ret, methodInfo.ReturnType, false));
                else
                {
                    wrappedType = true;
                    JObject retJson = (JObject)JsonObject.JTokenFromObject(null, ret, methodInfo.ReturnType, false);
                    foreach (JProperty prop in retJson.Properties())
                        dataJson.Add(prop.Name, prop.Value);
                }
            }

            // process out types
            for (int i = 0; i < outs.Length; i++)
            {
                string typeName = outTypes[i].Item1;
                Type paramType = outTypes[i].Item2;
                object obj = outs[i];

                dataJson.Add(JsonObject.JTokenFromObject(typeName, obj, paramType, false));
            }

            if (!wrappedType)
                json.Add("data", dataJson);
            else
                json = dataJson;

            return json.ToString();
        }

        /// <summary>
        /// Helper to process an incoming RPC request from a caller.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="interfaceType"></param>
        /// <param name="serviceType"></param>
        /// <param name="classInstance"></param>
        /// <param name="faultAction"></param>
        /// <returns></returns>
        public JObject ProcessRPCRequest(RPCContext context, Type interfaceType, Type serviceType, object classInstance = null,
            Func<RPCMessage, MethodInfo, Exception, bool> faultAction = null)
        {
            if (context.Message == null)
                throw new ArgumentException("context");
            if (context.Message.MessageType != RPCMessageType.Json_RPC)
                throw new ArgumentException("context");

            RPCContext.ctxIntfType = interfaceType;
            RPCContext.ctxSvcType = serviceType;

            if (classInstance == null)
            {
                // create an instance of this class
                ConstructorInfo ctor = serviceType.GetConstructor(new Type[] { });
                classInstance = ctor.Invoke(new object[] { });
            }

            JObject json = context.Message.MessageBody;

            // get the method base for the request
            JProperty methodProperty = json.First as JProperty;
            JObject methodElement = methodProperty.Value as JObject;
            if (methodElement[RPC_MSG_INARG_COUNT] == null)
                throw new InvalidDataException("In argument count is missing from method JSON");
            int inCount = methodElement[RPC_MSG_INARG_COUNT].Value<int>();
            if (methodElement[RPC_MSG_OUTARG_COUNT] == null)
                throw new InvalidDataException("Out argument count is missing from method JSON");
            int outCount = methodElement[RPC_MSG_OUTARG_COUNT].Value<int>();
            if (methodElement[RPC_MSG_ARGS] == null)
                throw new InvalidDataException("Arguments array is missing from method JSON");

            string methodName = methodProperty.Name;
            context.Message.IncomingMessageProperties.Add(RPC_MSG_PROP_OP_NAME, methodName);
            MethodInfo methodInfo = GetInheritanceMethod(methodName, classInstance.GetType());
            MethodBase ifaceMethodBase = GetInheritanceMethod(methodName, interfaceType);
            if (methodInfo != null && ifaceMethodBase != null)
            {
                if (!ifaceMethodBase.IsDefined(typeof(RPCMethodAttribute), false))
                    throw new InvalidOperationException("Method is not a RPC method contract");

                JArray arguments = methodElement[RPC_MSG_ARGS] as JArray;
                if (arguments == null)
                    throw new InvalidDataException("Arguments array is invalid");

                int childCount = arguments.Count;
                if (childCount != (inCount + outCount))
                    throw new InvalidOperationException("Invalid count of parameters");

                // iterate through all the child elements and parse them as arguments
                List<int> outIdx = new List<int>();
                List<Tuple<string, Type>> outTypes = new List<Tuple<string, Type>>();
                object[] args = new object[childCount];
                for (int i = 0; i < childCount; i++)
                {
                    JObject parameter = arguments[i] as JObject;

                    if (parameter[RPC_PARAM_AQN] == null)
                        throw new InvalidDataException("AQN attribute is missing from method parameter JSON");

                    Type paramType = Type.GetType(parameter[RPC_PARAM_AQN].Value<string>());
                    if (parameter[RPC_PARAM_IN] != null)
                    {
                        if (parameter[RPC_PARAM_VAL] == null)
                            throw new InvalidDataException("Object attribute is missing from method parameter JSON");

                        args[i] = JsonObject.ObjectFromJToken(parameter[RPC_PARAM_VAL], paramType, true);
                        if (parameter[RPC_PARAM_OAQN] != null)
                        {
                            Type originalType = null;
                            HandleToDictionary(ref paramType, out originalType, ref args[i]);
                            // ?? perhaps handle the boolean result?
                        }
                    }
                    else
                    {
                        args[i] = null;
                        outIdx.Add(i);
                        outTypes.Add(new Tuple<string, Type>(parameter[RPC_PARAM_NAME].Value<string>(), paramType));
                    }
                }

                // do we have a authentication guard instance?
                bool accessAllowed = true;
                if (AuthenticationGuard != null)
                {
                    try
                    {
                        RPCMessage refMsg = context.Message;
                        accessAllowed = AuthenticationGuard.CheckAccess(context, ref refMsg);
                        if (context.UseMessageAsResponse)
                        {
                            if (refMsg == null)
                                throw new InvalidOperationException("Message shouldn't be null when UseMessageAsResponse flag is set!");
                            RPCContext.ctxMessage = refMsg;
                        }
                    }
                    catch (Exception ex)
                    {
                        if (faultAction != null)
                        {
                            faultAction(context.Message, methodInfo, ex);
                            return null;
                        }
                        else
                            return PrepareRPCFaultResponse(context.Message, methodInfo, ex);
                    }
                }

                object ret = null;
                try
                {
                    // execute actual RPC and collect result
                    if (accessAllowed)
                        ret = methodInfo.Invoke(classInstance, args);
                    else
                    {
                        if (methodInfo.ReturnType.IsValueType)
                            ret = Activator.CreateInstance(methodInfo.ReturnType);
                        else
                            ret = null;
                    }
                }
                catch (Exception ex)
                {
                    if (faultAction != null)
                    {
                        faultAction(context.Message, methodInfo, ex);
                        return null;
                    }
                    else
                        return PrepareRPCFaultResponse(context.Message, methodInfo, ex);
                }

                // process "out" by reference arguments
                object[] outArgs = new object[outIdx.Count];
                if (outIdx.Count > 0)
                {
                    for (int i = 0; i < outIdx.Count; i++)
                    {
                        int idx = outIdx[i];
                        if (accessAllowed)
                            outArgs[i] = args[idx];
                        else
                        {
                            // if we've failed security invoke -- we'll create the outputs
                            // as default values
                            Type outType = outTypes[i].Item2;
                            if (outType.IsValueType)
                                outArgs[i] = Activator.CreateInstance(outType);
                            else
                                outArgs[i] = null;
                        }
                    }
                }

                JObject result = null;
                if (context.UseMessageAsResponse)
                    result = context.Message.MessageBody;
                else
                    result = PrepareRPCResult(context, methodInfo, ret, outTypes, outArgs);
                return result;
            }
            else
                throw new MethodAccessException("Requested RPC method is not valid.");
        }

        /// <summary>
        /// Helper to handle a RPC request response back to the caller.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="targetMethod"></param>
        /// <param name="outs"></param>
        public object ProcessRPCResponse(RPCContext context, MethodInfo targetMethod, object[] outs)
        {
            object ret = null;

            if (context.Message == null)
                throw new ArgumentException("context");
            if (context.Message.MessageType != RPCMessageType.Json_RPC)
                throw new ArgumentException("context");

            JObject json = context.Message.MessageBody;

            // process any execution faults
            ProcessFaultResponse(json, out _, true, out _);

            // get the method base for the request
            JProperty methodProperty = json.First as JProperty;
            JObject methodElement = methodProperty.Value as JObject;
            if (methodElement[RPC_MSG_OUTARG_COUNT] == null)
                throw new InvalidDataException("Out argument count is missing from method JSON");
            int outCount = methodElement[RPC_MSG_OUTARG_COUNT].Value<int>();
            if (methodElement[RPC_MSG_ARGS] == null)
                throw new InvalidDataException("Arguments array is missing from method JSON");

            // make sure the response we recieved is for the right method
            if (!methodProperty.Name.StartsWith(targetMethod.Name))
                throw new InvalidDataException("RPC responded with the wrong method response");

            JArray arguments = methodElement[RPC_MSG_ARGS] as JArray;
            if (arguments == null)
                throw new InvalidDataException("Arguments array is invalid");

            int childCount = arguments.Count;
            if (childCount != outCount)
                throw new InvalidOperationException("Invalid count of parameters");

            // iterate through all the child elements and parse them as ByRef outputs
            for (int i = 0; i < childCount; i++)
            {
                JObject parameter = arguments[i] as JObject;

                if (parameter[RPC_PARAM_AQN] == null)
                    throw new InvalidDataException("AQN attribute is missing from method parameter JSON");
                if (parameter[RPC_PARAM_VAL] == null)
                    throw new InvalidDataException("Object attribute is missing from method parameter JSON");

                Type paramType = Type.GetType(parameter[RPC_PARAM_AQN].Value<string>());
                if (parameter[RPC_PARAM_OUT] != null)
                {
                    outs[i] = JsonObject.ObjectFromJToken(parameter[RPC_PARAM_VAL], paramType, true);
                    if (i > outs.Length)
                        throw new InvalidOperationException("i > outs.Length; this shouldn't happen");
                    if (parameter[RPC_PARAM_OAQN] != null)
                    {
                        Type originalType = null;
                        HandleToDictionary(ref paramType, out originalType, ref outs[i]);
                        // ?? perhaps handle the boolean result?
                    }
                }
            }

            // parse return value
            if (methodElement[RPC_MSG_RET] != null)
            {
                JObject retVal = methodElement[RPC_MSG_RET] as JObject;

                if (retVal[RPC_PARAM_AQN] == null)
                    throw new InvalidDataException("AQN attribute is missing from method return JSON");

                Type retType = Type.GetType(retVal[RPC_PARAM_AQN].Value<string>());
                if (retType != typeof(void))
                {
                    if (retVal[RPC_PARAM_VAL] == null)
                        throw new InvalidDataException("Object attribute is missing from method return JSON");

                    ret = JsonObject.ObjectFromJToken(retVal[RPC_PARAM_VAL], retType, true);
                    if (retVal[RPC_PARAM_OAQN] != null)
                    {
                        Type originalType = null;
                        HandleToDictionary(ref retType, out originalType, ref ret);
                        // ?? perhaps handle the boolean result?
                    }
                }
                else
                    ret = null;
            }

            return ret;
        }

        /// <summary>
        /// Helper to handle a REST GET request query parameters.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="parameters"></param>
        /// <param name="args"></param>
        /// <param name="outIdx"></param>
        /// <param name="outTypes"></param>
        /// <returns></returns>
        private void ProcessGetParameters(RPCContext context, ParameterInfo[] parameters, ref object[] args,
            out List<int> outIdx, out List<Tuple<string, Type>> outTypes)
        {
            if (context.Message == null)
                throw new ArgumentException("context");
            if (context.Message.MessageType != RPCMessageType.Json_REST)
                throw new ArgumentException("context");

            IHttpContext httpContext = context.IncomingMessageProperties[RPC_MSG_PROP_HTTP_CONTEXT] as IHttpContext;
            if (httpContext == null)
                throw new ArgumentNullException("httpContext");

            UriTemplateMatch httpQueryTemplate = context.IncomingMessageProperties[RPC_MSG_PROP_HTTP_QUERY_TEMPLATE] as UriTemplateMatch;

            // get the counts of input and output parameters
            int inCount = 0, outCount = 0;
            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo pinfo = parameters[i];
                if (!pinfo.ParameterType.IsByRef)
                    inCount++;
                else
                    outCount++;
            }

            // process method parameters
            outIdx = new List<int>();
            outTypes = new List<Tuple<string, Type>>();
            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo pinfo = parameters[i];
                Type paramType = pinfo.ParameterType;
                if (!paramType.IsByRef)
                {
                    if (httpQueryTemplate.BoundVariables[pinfo.Name.ToUpper()] == null)
                        throw new InvalidOperationException(pinfo.Name + " was expected, but not found");

                    try
                    {
                        args[i] = Convert.ChangeType(httpQueryTemplate.BoundVariables[pinfo.Name.ToUpper()], paramType);
                    }
                    catch (Exception)
                    {
                        try
                        {
                            args[i] = JsonObject.ObjectFromJToken(new JValue(httpQueryTemplate.BoundVariables[pinfo.Name.ToUpper()]), paramType, false);
                        }
                        catch (Exception)
                        {
                            // create boxed type -- we couldn't deserialize
                            if (paramType.IsValueType)
                                args[i] = Activator.CreateInstance(paramType);
                            else
                            {
                                if (Nullable.GetUnderlyingType(paramType) != null)
                                    args[i] = null;
                                // ?? -- here be dragons -- if we can't create a boxed type via activation -- and the type is
                                // not nullable what do we do?
                            }
                        }
                    }
                }
                else
                {
                    paramType = paramType.GetElementType();

                    args[i] = null;
                    outIdx.Add(i);
                    outTypes.Add(new Tuple<string, Type>(pinfo.Name, paramType));
                }
            }
        }

        /// <summary>
        /// Helper to handle a REST POST/PUT request body parameters.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="parameters"></param>
        /// <param name="args"></param>
        /// <param name="outIdx"></param>
        /// <param name="outTypes"></param>
        /// <returns></returns>
        private void ProcessPostParameters(RPCContext context, ParameterInfo[] parameters, ref object[] args,
            out List<int> outIdx, out List<Tuple<string, Type>> outTypes)
        {
            JObject json = null;

            if (context.Message == null)
                throw new ArgumentException("context");
            if (context.Message.MessageType != RPCMessageType.Json_REST)
                throw new ArgumentException("context");

            IHttpContext httpContext = context.IncomingMessageProperties[RPC_MSG_PROP_HTTP_CONTEXT] as IHttpContext;
            if (httpContext == null)
                throw new ArgumentNullException("httpContext");

            // get the raw input data
            using (var reader = new StreamReader(httpContext.Request.Body, httpContext.Request.Encoding))
            {
                string raw = reader.ReadToEnd();
                json = JObject.Parse(raw);
            }

            // process method parameters
            outIdx = new List<int>();
            outTypes = new List<Tuple<string, Type>>();
            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo pinfo = parameters[i];
                Type paramType = pinfo.ParameterType;
                if (!paramType.IsByRef)
                {
                    if (json[pinfo.Name] == null)
                    {
                        // try to treat entire body as the the input object
                        if (parameters.Length == 1)
                        {
                            try
                            {
                                args[i] = JsonObject.ObjectFromJToken(json, paramType, false);
                            }
                            catch (Exception)
                            {
                                // create boxed type -- we couldn't deserialize
                                if (paramType.IsValueType)
                                    args[i] = Activator.CreateInstance(paramType);
                                else
                                {
                                    if (Nullable.GetUnderlyingType(paramType) != null)
                                        args[i] = null;
                                    // ?? -- here be dragons -- if we can't create a boxed type via activation -- and the type is
                                    // not nullable what do we do?
                                }
                            }
                        }
                        else
                            throw new InvalidOperationException(pinfo.Name + " was expected, but not found");
                    }

                    try
                    {
                        args[i] = Convert.ChangeType(json[pinfo.Name].ToString(), paramType);
                    }
                    catch (Exception)
                    {
                        try
                        {
                            args[i] = JsonObject.ObjectFromJToken(json[pinfo.Name], paramType, false);
                        }
                        catch (Exception)
                        {
                            // create boxed type -- we couldn't deserialize
                            if (paramType.IsValueType)
                                args[i] = Activator.CreateInstance(paramType);
                            else
                            {
                                if (Nullable.GetUnderlyingType(paramType) != null)
                                    args[i] = null;
                                // ?? -- here be dragons -- if we can't create a boxed type via activation -- and the type is
                                // not nullable what do we do?
                            }
                        }
                    }
                }
                else
                {
                    paramType = paramType.GetElementType();

                    args[i] = null;
                    outIdx.Add(i);
                    outTypes.Add(new Tuple<string, Type>(pinfo.Name, paramType));
                }
            }
        }

        /// <summary>
        /// Internal helper to handle the REST request.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="methodName"></param>
        /// <param name="serviceType"></param>
        /// <param name="interfaceType"></param>
        /// <param name="methodInfo"></param>
        /// <param name="ret"></param>
        /// <param name="outTypes"></param>
        /// <param name="outArgs"></param>
        /// <param name="faultAction"></param>
        /// <returns></returns>
        public bool ProcessRESTRequest(RPCContext context, string methodName, Type serviceType, Type interfaceType,
            out MethodInfo methodInfo, out object ret, out List<Tuple<string, Type>> outTypes, out object[] outArgs,
            Func<RPCMessage, MethodInfo, Exception, bool> faultAction = null)
        {
            if (context.Message == null)
                throw new ArgumentException("context");
            if (context.Message.MessageType != RPCMessageType.Json_REST)
                throw new ArgumentException("context");

            IHttpContext httpContext = context.IncomingMessageProperties[RPC_MSG_PROP_HTTP_CONTEXT] as IHttpContext;
            if (httpContext == null)
                throw new ArgumentNullException("httpContext");

            UriTemplateMatch httpQueryTemplate = context.IncomingMessageProperties[RPC_MSG_PROP_HTTP_QUERY_TEMPLATE] as UriTemplateMatch;

            RPCContext.ctxIntfType = interfaceType;
            RPCContext.ctxSvcType = serviceType;

            ret = null;
            outTypes = null;
            outArgs = null;

            // create an instance of this class
            ConstructorInfo ctor = serviceType.GetConstructor(new Type[] { });
            object classInstance = ctor.Invoke(new object[] { });

            context.IncomingMessageProperties.Add(RPC_MSG_PROP_OP_NAME, methodName);
            methodInfo = GetInheritanceMethod(methodName, classInstance.GetType());
            MethodBase ifaceMethodBase = GetInheritanceMethod(methodName, interfaceType);
            if (methodInfo != null && ifaceMethodBase != null)
            {
                ParameterInfo[] parameters = methodInfo.GetParameters();
                object[] args = new object[parameters.Length];
                List<int> outIdx = null;

                // is this endpoint user handled?
                bool userDefinedHandler = false;
                UserEndpointHandlerAttribute userHandlerAttr = ifaceMethodBase.GetCustomAttribute<UserEndpointHandlerAttribute>();
                if (userHandlerAttr != null)
                {
                    // ensure the method defines 2 fixed parameters
                    if (parameters.Length < 2)
                        throw new InvalidOperationException(httpContext.Request.Uri.ToString() + " is a user defined handler, method template must match (RequestWorker, IHttpContext)");

                    // does the method template have a string dictionary as the last parameter? if so we can use that for the HTTP outgoing headers
                    // NOTE: this behavior is undocumented
                    Dictionary<string, string> httpOutgoingHeaders = null;
                    if (parameters.Length > 2)
                    {
                        if (parameters[2].ParameterType == typeof(Dictionary<string, string>))
                            httpOutgoingHeaders = RPCMessage.BuildOutgoingHeaders(context.OutgoingMessageHeaders);
                        else
                            throw new InvalidOperationException(httpContext.Request.Uri.ToString() + " is a user defined handler, method parameter 2 must be type of Dictionary<string, string>");
                    }

                    if (parameters.Length > 2 && httpOutgoingHeaders == null)
                        throw new InvalidOperationException(httpContext.Request.Uri.ToString() + " is a user defined handler, method template must match (RequestWorker, IHttpContext)");

                    // ensure method return type is void
                    if (methodInfo.ReturnType != typeof(void))
                        throw new InvalidOperationException(httpContext.Request.Uri.ToString() + " is a user defined handler, method must return type of void");

                    // ensure the method defines required parameters; in the appropriate sequence
                    if (parameters[0].ParameterType != typeof(RequestWorker))
                        throw new InvalidOperationException(httpContext.Request.Uri.ToString() + " is a user defined handler, method parameter 0 must be type of RequestWorker");
                    if (parameters[1].ParameterType != typeof(IHttpContext))
                        throw new InvalidOperationException(httpContext.Request.Uri.ToString() + " is a user defined handler, method parameter 1 must be type of IHttpContext");

                    userDefinedHandler = true;

                    // override method arguments
                    if (httpOutgoingHeaders != null)
                        args = new object[3] { context.IncomingMessageProperties[RPC_MSG_PROP_HTTP_REQUEST_WORKER], context.IncomingMessageProperties[RPC_MSG_PROP_HTTP_CONTEXT],
                            httpOutgoingHeaders };
                    else
                        args = new object[2] { context.IncomingMessageProperties[RPC_MSG_PROP_HTTP_REQUEST_WORKER], context.IncomingMessageProperties[RPC_MSG_PROP_HTTP_CONTEXT] };
                }

                // if this is user-defined handler, we do not expect anything extra!
                if (!userDefinedHandler)
                {
                    try
                    {
                        // which method are we using?
                        switch (httpContext.Request.Method)
                        {
                            case Method.Head:
                            case Method.Get:
                                {
                                    // do a quick sanity check and make sure for "GET" like calls we have parameters
                                    int paramCount = parameters.Length;
                                    if ((parameters.Length > 0) && (httpQueryTemplate.QueryParameters.Count == 0 && httpQueryTemplate.BoundVariables.Count == 0))
                                        throw new InvalidOperationException(httpContext.Request.Uri.ToString() + " expected some parameters but none were supplied");

                                    ProcessGetParameters(context, parameters, ref args, out outIdx, out outTypes);
                                }
                                break;

                            case Method.Post:
                            case Method.Put:
                                ProcessPostParameters(context, parameters, ref args, out outIdx, out outTypes);
                                break;

                            case Method.Delete: // ? - techincally DELETE is NOT supposed to accept a body
                                {
                                    // do a quick sanity check and make sure for "GET" like calls we have parameters
                                    int paramCount = parameters.Length;
                                    if ((parameters.Length > 0) && (httpQueryTemplate.QueryParameters.Count > 0 && httpQueryTemplate.BoundVariables.Count > 0))
                                        ProcessGetParameters(context, parameters, ref args, out outIdx, out outTypes);
                                    else
                                        ProcessPostParameters(context, parameters, ref args, out outIdx, out outTypes);
                                }
                                break;

                            case Method.Options:
                            default:
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        if (faultAction != null)
                            return faultAction(context.Message, methodInfo, ex);
                        else
                        {
                            context.UseMessageAsResponse = true;
                            context.Message.MessageBody = PrepareRESTFaultResponse(methodInfo, ex);
                            return false;
                        }
                    }
                }

                // do we have a authentication guard instance?
                bool accessAllowed = true;
                if (AuthenticationGuard != null)
                {
                    try
                    {
                        RPCMessage refMsg = context.Message;
                        accessAllowed = AuthenticationGuard.CheckAccess(context, ref refMsg);
                        if (context.UseMessageAsResponse)
                        {
                            if (refMsg == null)
                                throw new InvalidOperationException("Message shouldn't be null when UseMessageAsResponse flag is set!");
                            RPCContext.ctxMessage = refMsg;
                        }
                    }
                    catch (Exception ex)
                    {
                        if (faultAction != null)
                            return faultAction(context.Message, methodInfo, ex);
                        else
                        {
                            context.UseMessageAsResponse = true;
                            context.Message.MessageBody = PrepareRESTFaultResponse(methodInfo, ex);
                            return false;
                        }
                    }
                }

                try
                {
                    // execute actual RPC and collect result
                    if (accessAllowed)
                    {
                        ret = methodInfo.Invoke(classInstance, args);
                    }
                    else
                    {
                        if (methodInfo.ReturnType.IsValueType)
                        {
                            if (methodInfo.ReturnType == typeof(void))
                                ret = null;
                            else
                                ret = Activator.CreateInstance(methodInfo.ReturnType);
                        }
                        else
                            ret = null;
                    }
                }
                catch (Exception ex)
                {
                    if (faultAction != null)
                        return faultAction(context.Message, methodInfo, ex);
                    else
                    {
                        context.UseMessageAsResponse = true;
                        context.Message.MessageBody = PrepareRESTFaultResponse(methodInfo, ex);
                        return false;
                    }
                }

                // process "out" by reference arguments
                if (outIdx != null)
                {
                    outArgs = new object[outIdx.Count];
                    if (outIdx.Count > 0)
                    {
                        for (int i = 0; i < outIdx.Count; i++)
                        {
                            int idx = outIdx[i];
                            if (accessAllowed)
                                outArgs[i] = args[idx];
                            else
                            {
                                // if we've failed security invoke -- we'll create the outputs
                                // as default values
                                Type outType = outTypes[i].Item2;
                                if (outType.IsValueType)
                                    outArgs[i] = Activator.CreateInstance(outType);
                                else
                                    outArgs[i] = null;
                            }
                        }
                    }
                }
            }
            else
                return false;

            return true;
        }
    } // public class RPCProxyHelper
} // namespace TridentFramework.RPC
