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
using System.Reflection;
using System.Security;

using TridentFramework.RPC.Remoting.Proxies;

namespace TridentFramework.RPC.Remoting
{
    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="TObject"></typeparam>
    [SecurityCritical]
    public class ChannelProxy<TObject> : DispatchProxy, IRemotingTypeInfo where TObject : class
    {
        private Type interfaceType;
        private IRPCProxySend channel;

        /*
        ** Properties
        */

        /// <summary>
        /// Gets or sets the fully qualified type name of the server object.
        /// </summary>
        string IRemotingTypeInfo.TypeName
        {
            get { return interfaceType.FullName; }
            set { }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelProxy{TObject}"/> class.
        /// </summary>
        public ChannelProxy()
        {
            /* stub */
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelProxy{TObject}"/> class.
        /// </summary>
        /// <param name="interfaceType"></param>
        /// <param name="channel"></param>
        public ChannelProxy(Type interfaceType, IRPCProxySend channel)
        {
            this.interfaceType = interfaceType;
            this.channel = channel;
        }

        /// <summary>
        /// Returns the proxy for the current instance of <see cref="ChannelProxy{TObject}"/>.
        /// </summary>
        /// <returns>The proxy for the current proxy instance.</returns>
        public TObject Create()
        {
            ChannelProxy<TObject> proxy = Create<TObject, ChannelProxy<TObject>>() as ChannelProxy<TObject>;
            proxy.interfaceType = interfaceType;
            proxy.channel = channel;

            return proxy as TObject;
        }

        /// <inheritdoc />
        protected override object Invoke(MethodInfo targetMethod, object[] args)
        {
            try
            {
                MethodMapper mapper = new MethodMapper(targetMethod);

                object[] outs;
                object[] ins = mapper.MapSyncInputs(args, out outs);

                object ret = this.channel.Send(targetMethod, mapper, ins, outs);

                object[] returnArgs = mapper.MapSyncOutputs(args, outs, ref ret);
                return ret;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <inheritdoc />
        bool IRemotingTypeInfo.CanCastTo(Type toType, object o)
        {
            return toType.IsAssignableFrom(interfaceType);
        }
    } // public sealed class ChannelProxy<TObject> : DispatchProxy, IRemotingTypeInfo
} // namespace TridentFramework.RPC.Remoting
