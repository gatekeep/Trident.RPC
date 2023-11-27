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
/*
 * Based on code from .NET Reference Source
 * Copyright (C) Microsoft Corporation., All Rights Reserved.
 * Licensed under MIT License.
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security;

namespace TridentFramework.RPC.Remoting
{
    /// <summary>
    ///
    /// </summary>
    public sealed class MethodMapper
    {
        static internal readonly ParameterInfo[] NoParams = new ParameterInfo[0];
        static internal readonly object[] EmptyArray = new object[0];

        private MethodInfo syncMethod;

        private ParameterInfo[] inParams;
        private ParameterInfo[] outParams;
        private ParameterInfo returnParam;

        /*
        ** Properties
        */

        /// <summary>
        ///
        /// </summary>
        public MethodInfo Method { get { return syncMethod; } }

        /// <summary>
        ///
        /// </summary>
        public ParameterInfo[] InArgs { get { return inParams; } }

        /// <summary>
        ///
        /// </summary>
        public ParameterInfo[] OutArgs { get { return outParams; } }

        /// <summary>
        ///
        /// </summary>
        public ParameterInfo ReturnParam { get { return returnParam; } }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="MethodMapper"/> class.
        /// </summary>
        /// <param name="syncMethod"></param>
        internal MethodMapper(MethodInfo syncMethod)
        {
            this.syncMethod = syncMethod;

            this.inParams = GetInputParameters(this.syncMethod, false);
            this.outParams = GetOutputParameters(this.syncMethod, false);
            this.returnParam = this.syncMethod.ReturnParameter;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="paramInfo"></param>
        /// <returns></returns>
        internal static bool FlowsIn(ParameterInfo paramInfo)    // conceptually both "in" and "in/out" params return true
        {
            return !paramInfo.IsOut || paramInfo.IsIn;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="paramInfo"></param>
        /// <returns></returns>
        internal static bool FlowsOut(ParameterInfo paramInfo)   // conceptually both "out" and "in/out" params return true
        {
            return paramInfo.ParameterType.IsByRef;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="method"></param>
        /// <param name="asyncPattern"></param>
        /// <returns></returns>
        internal static ParameterInfo[] GetInputParameters(MethodInfo method, bool asyncPattern)
        {
            int count = 0;
            ParameterInfo[] parameters = method.GetParameters();

            // length of parameters we care about (-2 for async)
            int len = parameters.Length;
            if (asyncPattern)
                len -= 2;

            // count the ins
            for (int i = 0; i < len; i++)
                if (FlowsIn(parameters[i]))
                    count++;

            // grab the ins
            ParameterInfo[] result = new ParameterInfo[count];
            int pos = 0;
            for (int i = 0; i < len; i++)
            {
                ParameterInfo param = parameters[i];
                if (FlowsIn(param))
                    result[pos++] = param;
            }

            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="method"></param>
        /// <param name="asyncPattern"></param>
        /// <returns></returns>
        internal static ParameterInfo[] GetOutputParameters(MethodInfo method, bool asyncPattern)
        {
            int count = 0;
            ParameterInfo[] parameters = method.GetParameters();

            // length of parameters we care about (-1 for async)
            int len = parameters.Length;
            if (asyncPattern)
                len -= 1;

            // count the outs
            for (int i = 0; i < len; i++)
                if (FlowsOut(parameters[i]))
                    count++;

            // grab the outs
            ParameterInfo[] result = new ParameterInfo[count];
            int pos = 0;
            for (int i = 0; i < len; i++)
            {
                ParameterInfo param = parameters[i];
                if (FlowsOut(param))
                    result[pos++] = param;
            }

            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="parameterInfo"></param>
        /// <returns></returns>
        internal static Type GetParameterType(ParameterInfo parameterInfo)
        {
            Type parameterType = parameterInfo.ParameterType;
            if (parameterType.IsByRef)
                return parameterType.GetElementType();
            else
                return parameterType;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="parameterType"></param>
        /// <returns></returns>
        internal static object GetDefaultParameterValue(Type parameterType)
        {
            return (parameterType.IsValueType && parameterType != typeof(void)) ? Activator.CreateInstance(parameterType) : null;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="args"></param>
        /// <param name="outs"></param>
        /// <returns></returns>
        [SecurityCritical]
        internal object[] MapSyncInputs(object[] args, out object[] outs)
        {
            if (this.outParams.Length == 0)
                outs = EmptyArray;
            else
                outs = new object[this.outParams.Length];

            if (this.inParams.Length == 0)
                return EmptyArray;

            List<object> ins = new List<object>();
            for (int i = 0; i < inParams.Length; i++)
                ins.Add(args[inParams[i].Position]);

            return ins.ToArray();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="args"></param>
        /// <param name="outs"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        [SecurityCritical]
        internal object[] MapSyncOutputs(object[] args, object[] outs, ref object ret)
        {
            if (ret == null && this.returnParam != null)
                ret = GetDefaultParameterValue(GetParameterType(this.returnParam));
            if (outParams.Length == 0)
                return null;

            for (int i = 0; i < outParams.Length; i++)
            {
                if (outs[i] == null)
                {
                    // the RealProxy infrastructure requires a default value for value types
                    args[outParams[i].Position] = GetDefaultParameterValue(GetParameterType(outParams[i]));
                }
                else
                    args[outParams[i].Position] = outs[i];
            }

            return args;
        }
    } // public sealed class MethodMapper
} // namespace TridentFramework.RPC.Remoting
