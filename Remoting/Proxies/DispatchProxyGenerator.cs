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
/*
 * Based on code from .NET Reference Source
 * Copyright (c) .NET Foundation and Contributors., All Rights Reserved.
 * Licensed under MIT License.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace TridentFramework.RPC.Remoting.Proxies
{
    /// <summary>
    /// <para>
    /// Helper class to handle the IL EMIT for the generation of proxies.
    /// Much of this code was taken directly from the Silverlight proxy generation.
    /// </para>
    /// <para>
    /// Differences between this and the Silverlight version are:
    /// <list type="number">
    ///   <item>
    ///     This version is based on DispatchProxy from .NET Native and CoreCLR, not RealProxy in Silverlight ServiceModel.
    ///     There are several notable differences between them.
    ///   </item>
    ///   <item>
    ///     <para>
    ///     Both DispatchProxy and RealProxy permit the caller to ask for a proxy specifying a pair of types:
    ///     the interface type to implement, and a base type.  But they behave slightly differently:
    ///     <list type="bullet">
    ///         <item>RealProxy generates a proxy type that derives from Object and *implements* all the base type's
    ///         interfaces plus all the interface type's interfaces.</item>
    ///         <item>DispatchProxy generates a proxy type that *derives* from the base type and implements all
    ///         the interface type's interfaces.  This is true for both the CLR version in .NET Native and this
    ///         version for CoreCLR.</item>
    ///      </list>
    ///    </para>
    ///  </item>
    ///  <item>
    ///    <para>
    ///    DispatchProxy and RealProxy use different type hierarchies for the generated proxies:
    ///    <list type="bullet">
    ///      <item>RealProxy type hierarchy is: proxyType : proxyBaseType : object<br/>
    ///       Presumably the 'proxyBaseType' in the middle is to allow it to implement the base type's interfaces
    ///       explicitly, preventing collision for same name methods on the base and interface types.</item>
    ///       <item>DispatchProxy hierarchy is: proxyType : baseType (where baseType : DispatchProxy)<br/>
    ///       The generated DispatchProxy proxy type does not need to generate implementation methods
    ///       for the base type's interfaces, because the base type already must have implemented them.</item>
    ///    </list>
    ///    </para>
    ///  </item>
    ///  <item>
    ///    <para>
    ///    RealProxy required a proxy instance to hold a backpointer to the RealProxy instance to mirror
    ///    the .NET Remoting design that required the proxy and RealProxy to be separate instances.
    ///    </para>
    ///    <para>
    ///    But the DispatchProxy design encourages the proxy type to *be* an DispatchProxy.  Therefore,
    ///    the proxy's 'this' becomes the equivalent of RealProxy's backpointer to RealProxy, so we were
    ///    able to remove an extraneous field and ctor arg from the DispatchProxy proxies.
    ///    </para>
    ///   </item>
    /// </list>    
    /// </para>
    /// </summary>
    internal static class DispatchProxyGenerator
    {
        // Generated proxies have a private MethodInfo[] field that generated methods use to get the corresponding MethodInfo.
        // It is the first field in the class and the first ctor parameter.
        private const int MethodInfosFieldAndCtorParameterIndex = 0;

        // Proxies are requested for a pair of types: base type and interface type.
        // The generated proxy will subclass the given base type and implement the interface type.
        // We maintain a cache keyed by 'base type' containing a dictionary keyed by interface type,
        // containing the generated proxy type for that pair.   There are likely to be few (maybe only 1)
        // base type in use for many interface types.
        // Note: this differs from Silverlight's RealProxy implementation which keys strictly off the
        // interface type.  But this does not allow the same interface type to be used with more than a
        // single base type.  The implementation here permits multiple interface types to be used with
        // multiple base types, and the generated proxy types will be unique.
        // This cache of generated types grows unbounded, one element per unique T/ProxyT pair.
        // This approach is used to prevent regenerating identical proxy types for identical T/Proxy pairs,
        // which would ultimately be a more expensive leak.
        // Proxy instances are not cached.  Their lifetime is entirely owned by the caller of DispatchProxy.Create.
        private static readonly Dictionary<Type, Dictionary<Type, GeneratedTypeInfo>> baseTypeAndInterfaceToGeneratedProxyType = new Dictionary<Type, Dictionary<Type, GeneratedTypeInfo>>();
        private static readonly ProxyAssembly proxyAssembly = new ProxyAssembly();
        private static readonly MethodInfo dispatchProxyInvokeMethod = typeof(DispatchProxy).GetMethod("Invoke", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly MethodInfo getTypeFromHandleMethod = typeof(Type).GetRuntimeMethod("GetTypeFromHandle", new Type[] { typeof(RuntimeTypeHandle) });
        private static readonly MethodInfo makeGenericMethodMethod = typeof(MethodInfo).GetMethod("MakeGenericMethod", new Type[] { typeof(Type[]) });

        /*
        ** Classes
        */

        /// <summary>
        /// 
        /// </summary>
        private sealed class GeneratedTypeInfo
        {
            /*
            ** Properties
            */

            /// <summary>
            /// 
            /// </summary>
            public Type GeneratedType { get; }

            /// <summary>
            /// 
            /// </summary>
            public MethodInfo[] MethodInfos { get; }

            /*
            ** Methods
            */

            /// <summary>
            /// Initializes a new instance of the <see cref="GeneratedTypeInfo"/> class.
            /// </summary>
            /// <param name="generatedType"></param>
            /// <param name="methodInfos"></param>
            public GeneratedTypeInfo(Type generatedType, MethodInfo[] methodInfos)
            {
                GeneratedType = generatedType;
                MethodInfos = methodInfos;
            }
        } // private sealed class GeneratedTypeInfo

        /// <summary>
        /// 
        /// </summary>
        private sealed class ProxyAssembly
        {
            private readonly AssemblyBuilder ab;
            private readonly ModuleBuilder mb;
            private int typeId;

            private readonly HashSet<string> ignoresAccessAssemblyNames = new HashSet<string>();
            private ConstructorInfo ignoresAccessChecksToAttributeConstructor;

            /*
            ** Properties
            */

            /// <summary>
            /// Gets or creates the ConstructorInfo for the IgnoresAccessChecksAttribute.
            /// This attribute is both defined and referenced in the dynamic assembly to
            /// allow access to internal types in other assemblies.
            /// </summary>
            internal ConstructorInfo IgnoresAccessChecksAttributeConstructor
            {
                get
                {
                    if (ignoresAccessChecksToAttributeConstructor == null)
                        ignoresAccessChecksToAttributeConstructor = IgnoreAccessChecksToAttributeBuilder.AddToModule(mb);

                    return ignoresAccessChecksToAttributeConstructor;
                }
            }

            /*
            ** Methods
            */

            /// <summary>
            /// Initializes a new instance of the <see cref="ProxyAssembly"/> class.
            /// </summary>
            public ProxyAssembly()
            {
                ab = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("ProxyBuilder"), AssemblyBuilderAccess.Run);
                mb = ab.DefineDynamicModule("testModule");
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="name"></param>
            /// <param name="proxyBaseType"></param>
            /// <returns></returns>
            public ProxyBuilder CreateProxy(string name, Type proxyBaseType)
            {
                int nextId = Interlocked.Increment(ref typeId);
                TypeBuilder tb = mb.DefineType($"{name}`{nextId}", TypeAttributes.Public, proxyBaseType);
                return new ProxyBuilder(this, tb, proxyBaseType);
            }

            /// <summary>
            /// Generates an instance of the IgnoresAccessChecksToAttribute to identify the given assembly as 
            /// one which contains internal types the dynamic assembly will need to reference.
            /// </summary>
            /// <param name="assemblyName"></param>
            internal void GenerateInstanceOfIgnoresAccessChecksToAttribute(string assemblyName)
            {
                // Add this assembly level attribute:
                // [assembly: System.Runtime.CompilerServices.IgnoresAccessChecksToAttribute(assemblyName)]
                ConstructorInfo attributeConstructor = IgnoresAccessChecksAttributeConstructor;
                CustomAttributeBuilder customAttributeBuilder =
                    new CustomAttributeBuilder(attributeConstructor, new object[] { assemblyName });
                ab.SetCustomAttribute(customAttributeBuilder);
            }

            /// <summary>
            /// Ensures the type we will reference from the dynamic assembly is visible. Non-public types
            /// need to emit an attribute that allows access from the dynamic assembly.
            /// </summary>
            /// <param name="type"></param>
            internal void EnsureTypeIsVisible(Type type)
            {
                if (!type.IsVisible)
                {
                    string assemblyName = type.Assembly.GetName().Name;
                    if (!ignoresAccessAssemblyNames.Contains(assemblyName))
                    {
                        GenerateInstanceOfIgnoresAccessChecksToAttribute(assemblyName);
                        ignoresAccessAssemblyNames.Add(assemblyName);
                    }
                }
            }
        } // private sealed class ProxyAssembly

        /// <summary>
        /// 
        /// </summary>
        private sealed class ProxyBuilder
        {
            private readonly ProxyAssembly assembly;
            private readonly TypeBuilder tb;
            private readonly Type proxyBaseType;
            private readonly List<FieldBuilder> fields;
            private readonly List<MethodInfo> methodInfos;

            private static readonly OpCode[] convOpCodes = new OpCode[] {
                OpCodes.Nop,            // Empty    = 0,
                OpCodes.Nop,            // Object   = 1,
                OpCodes.Nop,            // DBNull   = 2,
                OpCodes.Conv_I1,        // Boolean  = 3,
                OpCodes.Conv_I2,        // Char     = 4,
                OpCodes.Conv_I1,        // SByte    = 5,
                OpCodes.Conv_U1,        // Byte     = 6,
                OpCodes.Conv_I2,        // Int16    = 7,
                OpCodes.Conv_U2,        // UInt16   = 8,
                OpCodes.Conv_I4,        // Int32    = 9,
                OpCodes.Conv_U4,        // UInt32   = 10,
                OpCodes.Conv_I8,        // Int64    = 11,
                OpCodes.Conv_U8,        // UInt64   = 12,
                OpCodes.Conv_R4,        // Single   = 13,
                OpCodes.Conv_R8,        // Double   = 14,
                OpCodes.Nop,            // Decimal  = 15,
                OpCodes.Nop,            // DateTime = 16,
                OpCodes.Nop,            //            17
                OpCodes.Nop,            // String   = 18,
            };

            private static readonly OpCode[] ldindOpCodes = new OpCode[] {
                OpCodes.Nop,            // Empty    = 0,
                OpCodes.Nop,            // Object   = 1,
                OpCodes.Nop,            // DBNull   = 2,
                OpCodes.Ldind_I1,       // Boolean  = 3,
                OpCodes.Ldind_I2,       // Char     = 4,
                OpCodes.Ldind_I1,       // SByte    = 5,
                OpCodes.Ldind_U1,       // Byte     = 6,
                OpCodes.Ldind_I2,       // Int16    = 7,
                OpCodes.Ldind_U2,       // UInt16   = 8,
                OpCodes.Ldind_I4,       // Int32    = 9,
                OpCodes.Ldind_U4,       // UInt32   = 10,
                OpCodes.Ldind_I8,       // Int64    = 11,
                OpCodes.Ldind_I8,       // UInt64   = 12,
                OpCodes.Ldind_R4,       // Single   = 13,
                OpCodes.Ldind_R8,       // Double   = 14,
                OpCodes.Nop,            // Decimal  = 15,
                OpCodes.Nop,            // DateTime = 16,
                OpCodes.Nop,            //            17
                OpCodes.Ldind_Ref,      // String   = 18,
            };

            private static readonly OpCode[] stindOpCodes = new OpCode[] {
                OpCodes.Nop,            // Empty    = 0,
                OpCodes.Nop,            // Object   = 1,
                OpCodes.Nop,            // DBNull   = 2,
                OpCodes.Stind_I1,       // Boolean  = 3,
                OpCodes.Stind_I2,       // Char     = 4,
                OpCodes.Stind_I1,       // SByte    = 5,
                OpCodes.Stind_I1,       // Byte     = 6,
                OpCodes.Stind_I2,       // Int16    = 7,
                OpCodes.Stind_I2,       // UInt16   = 8,
                OpCodes.Stind_I4,       // Int32    = 9,
                OpCodes.Stind_I4,       // UInt32   = 10,
                OpCodes.Stind_I8,       // Int64    = 11,
                OpCodes.Stind_I8,       // UInt64   = 12,
                OpCodes.Stind_R4,       // Single   = 13,
                OpCodes.Stind_R8,       // Double   = 14,
                OpCodes.Nop,            // Decimal  = 15,
                OpCodes.Nop,            // DateTime = 16,
                OpCodes.Nop,            //            17
                OpCodes.Stind_Ref,      // String   = 18,
            };

            /*
            ** Classes
            */

            /// <summary>
            /// 
            /// </summary>
            private sealed class ParametersArray
            {
                private readonly ILGenerator il;
                private readonly Type[] paramTypes;

                /*
                ** Methods
                */

                /// <summary>
                /// Initializes a new instance of the <see cref="ParametersArray"/> class.
                /// </summary>
                /// <param name="il"></param>
                /// <param name="paramTypes"></param>
                internal ParametersArray(ILGenerator il, Type[] paramTypes)
                {
                    this.il = il;
                    this.paramTypes = paramTypes;
                }

                /// <summary>
                /// 
                /// </summary>
                /// <param name="i"></param>
                internal void Get(int i) => il.Emit(OpCodes.Ldarg, i + 1);

                /// <summary>
                /// 
                /// </summary>
                /// <param name="i"></param>
                internal void BeginSet(int i) => il.Emit(OpCodes.Ldarg, i + 1);

                /// <summary>
                /// 
                /// </summary>
                /// <param name="i"></param>
                /// <param name="stackType"></param>
                internal void EndSet(int i, Type stackType)
                {
                    Debug.Assert(paramTypes[i].IsByRef);
                    Type argType = paramTypes[i].GetElementType();
                    Convert(il, stackType, argType, false);
                    Stind(il, argType);
                }
            } // private sealed class ParametersArray

            /// <summary>
            /// 
            /// </summary>
            /// <typeparam name="T"></typeparam>
            private sealed class GenericArray<T>
            {
                private readonly ILGenerator il;
                private readonly LocalBuilder lb;

                /*
                ** Methods
                */

                /// <summary>
                /// Initializes a new instance of the <see cref="GenericArray{T}"/> class.
                /// </summary>
                /// <param name="il"></param>
                /// <param name="len"></param>
                internal GenericArray(ILGenerator il, int len)
                {
                    this.il = il;
                    lb = il.DeclareLocal(typeof(T[]));

                    il.Emit(OpCodes.Ldc_I4, len);
                    il.Emit(OpCodes.Newarr, typeof(T));
                    il.Emit(OpCodes.Stloc, lb);
                }

                /// <summary>
                /// 
                /// </summary>
                internal void Load() => il.Emit(OpCodes.Ldloc, lb);

                /// <summary>
                /// 
                /// </summary>
                /// <param name="i"></param>
                internal void Get(int i)
                {
                    il.Emit(OpCodes.Ldloc, lb);
                    il.Emit(OpCodes.Ldc_I4, i);
                    il.Emit(OpCodes.Ldelem_Ref);
                }

                /// <summary>
                /// 
                /// </summary>
                /// <param name="i"></param>
                internal void BeginSet(int i)
                {
                    il.Emit(OpCodes.Ldloc, lb);
                    il.Emit(OpCodes.Ldc_I4, i);
                }

                /// <summary>
                /// 
                /// </summary>
                /// <param name="stackType"></param>
                internal void EndSet(Type stackType)
                {
                    Convert(il, stackType, typeof(T), false);
                    il.Emit(OpCodes.Stelem_Ref);
                }
            } // private sealed class GenericArray<T>

            /// <summary>
            /// 
            /// </summary>
            private sealed class PropertyAccessorInfo
            {
                /*
                ** Properties
                */

                /// <summary>
                /// 
                /// </summary>
                public MethodInfo InterfaceGetMethod { get; }

                /// <summary>
                /// 
                /// </summary>
                public MethodInfo InterfaceSetMethod { get; }

                /// <summary>
                /// 
                /// </summary>
                public MethodBuilder GetMethodBuilder { get; set; }

                /// <summary>
                /// 
                /// </summary>
                public MethodBuilder SetMethodBuilder { get; set; }

                /*
                ** Methods
                */

                /// <summary>
                /// Initializes a new instance of the <see cref="PropertyAccessorInfo"/> class.
                /// </summary>
                /// <param name="interfaceGetMethod"></param>
                /// <param name="interfaceSetMethod"></param>
                public PropertyAccessorInfo(MethodInfo interfaceGetMethod, MethodInfo interfaceSetMethod)
                {
                    InterfaceGetMethod = interfaceGetMethod;
                    InterfaceSetMethod = interfaceSetMethod;
                }
            } // private sealed class PropertyAccessorInfo

            /// <summary>
            /// 
            /// </summary>
            private sealed class EventAccessorInfo
            {
                /*
                ** Properties
                */

                /// <summary>
                /// 
                /// </summary>
                public MethodInfo InterfaceAddMethod { get; }

                /// <summary>
                /// 
                /// </summary>
                public MethodInfo InterfaceRemoveMethod { get; }

                /// <summary>
                /// 
                /// </summary>
                public MethodInfo InterfaceRaiseMethod { get; }

                /// <summary>
                /// 
                /// </summary>
                public MethodBuilder AddMethodBuilder { get; set; }

                /// <summary>
                /// 
                /// </summary>
                public MethodBuilder RemoveMethodBuilder { get; set; }

                /// <summary>
                /// 
                /// </summary>
                public MethodBuilder RaiseMethodBuilder { get; set; }

                /*
                ** Methods
                */

                /// <summary>
                /// Initializes a new instance of the <see cref="EventAccessorInfo"/> class.
                /// </summary>
                /// <param name="interfaceAddMethod"></param>
                /// <param name="interfaceRemoveMethod"></param>
                /// <param name="interfaceRaiseMethod"></param>
                public EventAccessorInfo(MethodInfo interfaceAddMethod, MethodInfo interfaceRemoveMethod, MethodInfo interfaceRaiseMethod)
                {
                    InterfaceAddMethod = interfaceAddMethod;
                    InterfaceRemoveMethod = interfaceRemoveMethod;
                    InterfaceRaiseMethod = interfaceRaiseMethod;
                }
            } // private sealed class EventAccessorInfo

            /// <summary>
            /// 
            /// </summary>
            private sealed class MethodInfoEqualityComparer : EqualityComparer<MethodInfo>
            {
                public static readonly MethodInfoEqualityComparer Instance = new MethodInfoEqualityComparer();

                /*
                ** Methods
                */

                /// <summary>
                /// Initializes a new instance of the <see cref="MethodInfoEqualityComparer"/> class.
                /// </summary>
                private MethodInfoEqualityComparer() { }

                /// <inheritdoc />
                public sealed override bool Equals(MethodInfo left, MethodInfo right)
                {
                    if (ReferenceEquals(left, right))
                        return true;

                    if (left == null)
                        return right == null;
                    else if (right == null)
                        return false;

                    // TODO: switch to use MemberInfo.MetadataToken here.
                    // It compares honestly referring ECMA-335 I.8.6.1.6 Signature Matching.
                    if (!Equals(left.DeclaringType, right.DeclaringType))
                        return false;

                    if (!Equals(left.ReturnType, right.ReturnType))
                        return false;

                    if (left.CallingConvention != right.CallingConvention)
                        return false;

                    if (left.IsStatic != right.IsStatic)
                        return false;

                    if (left.Name != right.Name)
                        return false;

                    Type[] leftGenericParameters = left.GetGenericArguments();
                    Type[] rightGenericParameters = right.GetGenericArguments();
                    if (leftGenericParameters.Length != rightGenericParameters.Length)
                        return false;

                    for (int i = 0; i < leftGenericParameters.Length; i++)
                        if (!Equals(leftGenericParameters[i], rightGenericParameters[i]))
                            return false;

                    ParameterInfo[] leftParameters = left.GetParameters();
                    ParameterInfo[] rightParameters = right.GetParameters();
                    if (leftParameters.Length != rightParameters.Length)
                        return false;

                    for (int i = 0; i < leftParameters.Length; i++)
                        if (!Equals(leftParameters[i].ParameterType, rightParameters[i].ParameterType))
                            return false;

                    return true;
                }

                /// <inheritdoc />
                public sealed override int GetHashCode(MethodInfo obj)
                {
                    if (obj == null)
                        return 0;

                    Debug.Assert(obj.DeclaringType != null);
                    int hashCode = obj.DeclaringType.GetHashCode();
                    hashCode ^= obj.Name.GetHashCode();
                    foreach (ParameterInfo parameter in obj.GetParameters())
                        hashCode ^= parameter.ParameterType.GetHashCode();

                    return hashCode;
                }
            } // private sealed class MethodInfoEqualityComparer : EqualityComparer<MethodInfo>

            /*
            ** Methods
            */

            /// <summary>
            /// Initializes a new instance of the <see cref="ProxyBuilder"/> class.
            /// </summary>
            /// <param name="assembly"></param>
            /// <param name="tb"></param>
            /// <param name="proxyBaseType"></param>
            internal ProxyBuilder(ProxyAssembly assembly, TypeBuilder tb, Type proxyBaseType)
            {
                this.assembly = assembly;
                this.tb = tb;
                this.proxyBaseType = proxyBaseType;

                fields = new List<FieldBuilder>();
                fields.Add(tb.DefineField("_methodInfos", typeof(MethodInfo[]), FieldAttributes.Private));

                methodInfos = new List<MethodInfo>();

                this.assembly.EnsureTypeIsVisible(proxyBaseType);
            }

            /// <summary>
            /// 
            /// </summary>
            private void Complete()
            {
                Type[] args = new Type[fields.Count];
                for (int i = 0; i < args.Length; i++)
                {
                    args[i] = fields[i].FieldType;
                }

                ConstructorBuilder cb = tb.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, args);
                ILGenerator il = cb.GetILGenerator();

                // chained ctor call
                ConstructorInfo baseCtor = proxyBaseType.GetConstructor(Type.EmptyTypes);
                Debug.Assert(baseCtor != null);

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, baseCtor);

                // store all the fields
                for (int i = 0; i < args.Length; i++)
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldarg, i + 1);
                    il.Emit(OpCodes.Stfld, fields[i]);
                }

                il.Emit(OpCodes.Ret);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            internal GeneratedTypeInfo CreateType()
            {
                this.Complete();
                return new GeneratedTypeInfo(tb.CreateType(), methodInfos.ToArray());
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="iface"></param>
            internal void AddInterfaceImpl(Type iface)
            {
                // If necessary, generate an attribute to permit visibility
                // to internal types.
                assembly.EnsureTypeIsVisible(iface);

                tb.AddInterfaceImplementation(iface);

                // AccessorMethods -> Metadata mappings.
                Dictionary<MethodInfo, PropertyAccessorInfo> propertyMap = new Dictionary<MethodInfo, PropertyAccessorInfo>(MethodInfoEqualityComparer.Instance);
                foreach (PropertyInfo pi in iface.GetRuntimeProperties())
                {
                    var ai = new PropertyAccessorInfo(pi.GetMethod, pi.SetMethod);
                    if (pi.GetMethod != null)
                        propertyMap[pi.GetMethod] = ai;
                    if (pi.SetMethod != null)
                        propertyMap[pi.SetMethod] = ai;
                }

                Dictionary<MethodInfo, EventAccessorInfo> eventMap = new Dictionary<MethodInfo, EventAccessorInfo>(MethodInfoEqualityComparer.Instance);
                foreach (EventInfo ei in iface.GetRuntimeEvents())
                {
                    var ai = new EventAccessorInfo(ei.AddMethod, ei.RemoveMethod, ei.RaiseMethod);
                    if (ei.AddMethod != null)
                        eventMap[ei.AddMethod] = ai;
                    if (ei.RemoveMethod != null)
                        eventMap[ei.RemoveMethod] = ai;
                    if (ei.RaiseMethod != null)
                        eventMap[ei.RaiseMethod] = ai;
                }

                foreach (MethodInfo mi in iface.GetRuntimeMethods())
                {
                    // Skip regular/non-virtual instance methods, static methods, and methods that cannot be overriden
                    // ("methods that cannot be overriden" includes default implementation of other interface methods).
                    if (!mi.IsVirtual || mi.IsFinal)
                        continue;

                    int methodInfoIndex = methodInfos.Count;
                    methodInfos.Add(mi);
                    MethodBuilder mdb = AddMethodImpl(mi, methodInfoIndex);
                    if (propertyMap.TryGetValue(mi, out PropertyAccessorInfo associatedProperty))
                    {
                        if (MethodInfoEqualityComparer.Instance.Equals(associatedProperty.InterfaceGetMethod, mi))
                            associatedProperty.GetMethodBuilder = mdb;
                        else
                            associatedProperty.SetMethodBuilder = mdb;
                    }

                    if (eventMap.TryGetValue(mi, out EventAccessorInfo associatedEvent))
                    {
                        if (MethodInfoEqualityComparer.Instance.Equals(associatedEvent.InterfaceAddMethod, mi))
                            associatedEvent.AddMethodBuilder = mdb;
                        else if (MethodInfoEqualityComparer.Instance.Equals(associatedEvent.InterfaceRemoveMethod, mi))
                            associatedEvent.RemoveMethodBuilder = mdb;
                        else
                            associatedEvent.RaiseMethodBuilder = mdb;
                    }
                }

                foreach (PropertyInfo pi in iface.GetRuntimeProperties())
                {
                    PropertyAccessorInfo ai = propertyMap[pi.GetMethod ?? pi.SetMethod];

                    // If we didn't make an overriden accessor above, this was a static property, non-virtual property,
                    // or a default implementation of a property of a different interface. In any case, we don't need
                    // to redeclare it.
                    if (ai.GetMethodBuilder == null && ai.SetMethodBuilder == null)
                        continue;

                    PropertyBuilder pb = tb.DefineProperty(pi.Name, pi.Attributes, pi.PropertyType, pi.GetIndexParameters().Select(p => p.ParameterType).ToArray());
                    if (ai.GetMethodBuilder != null)
                        pb.SetGetMethod(ai.GetMethodBuilder);
                    if (ai.SetMethodBuilder != null)
                        pb.SetSetMethod(ai.SetMethodBuilder);
                }

                foreach (EventInfo ei in iface.GetRuntimeEvents())
                {
                    EventAccessorInfo ai = eventMap[ei.AddMethod ?? ei.RemoveMethod];

                    // If we didn't make an overriden accessor above, this was a static event, non-virtual event,
                    // or a default implementation of an event of a different interface. In any case, we don't
                    // need to redeclare it.
                    if (ai.AddMethodBuilder == null && ai.RemoveMethodBuilder == null && ai.RaiseMethodBuilder == null)
                        continue;

                    Debug.Assert(ei.EventHandlerType != null);
                    EventBuilder eb = tb.DefineEvent(ei.Name, ei.Attributes, ei.EventHandlerType);
                    if (ai.AddMethodBuilder != null)
                        eb.SetAddOnMethod(ai.AddMethodBuilder);
                    if (ai.RemoveMethodBuilder != null)
                        eb.SetRemoveOnMethod(ai.RemoveMethodBuilder);
                    if (ai.RaiseMethodBuilder != null)
                        eb.SetRaiseMethod(ai.RaiseMethodBuilder);
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="mi"></param>
            /// <param name="methodInfoIndex"></param>
            /// <returns></returns>
            private MethodBuilder AddMethodImpl(MethodInfo mi, int methodInfoIndex)
            {
                ParameterInfo[] parameters = mi.GetParameters();
                Type[] paramTypes = new Type[parameters.Length];
                Type[][] paramReqMods = new Type[paramTypes.Length][];

                for (int i = 0; i < parameters.Length; i++)
                {
                    paramTypes[i] = parameters[i].ParameterType;
                    paramReqMods[i] = parameters[i].GetRequiredCustomModifiers();
                }

                MethodBuilder mdb = tb.DefineMethod(mi.Name, MethodAttributes.Public | MethodAttributes.Virtual, CallingConventions.Standard,
                    mi.ReturnType, null, null, paramTypes, paramReqMods, null);

                if (mi.ContainsGenericParameters)
                {
                    Type[] ts = mi.GetGenericArguments();
                    string[] ss = new string[ts.Length];
                    for (int i = 0; i < ts.Length; i++)
                        ss[i] = ts[i].Name;

                    GenericTypeParameterBuilder[] genericParameters = mdb.DefineGenericParameters(ss);
                    for (int i = 0; i < genericParameters.Length; i++)
                        genericParameters[i].SetGenericParameterAttributes(ts[i].GenericParameterAttributes);
                }

                ILGenerator il = mdb.GetILGenerator();
                ParametersArray args = new ParametersArray(il, paramTypes);

                // object[] args = new object[paramCount];
                il.Emit(OpCodes.Nop);
                GenericArray<object> argsArr = new GenericArray<object>(il, parameters.Length);

                for (int i = 0; i < parameters.Length; i++)
                {
                    // args[i] = argi;
                    bool isOutRef = parameters[i].IsOut && parameters[i].ParameterType.IsByRef && !parameters[i].IsIn;

                    if (!isOutRef)
                    {
                        argsArr.BeginSet(i);
                        args.Get(i);
                        argsArr.EndSet(parameters[i].ParameterType);
                    }
                }

                // MethodInfo methodInfo = _methodInfos[methodInfoIndex];
                LocalBuilder methodInfoLocal = il.DeclareLocal(typeof(MethodInfo));
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, fields[MethodInfosFieldAndCtorParameterIndex]); // MethodInfo[] _methodInfos
                il.Emit(OpCodes.Ldc_I4, methodInfoIndex);
                il.Emit(OpCodes.Ldelem_Ref);
                il.Emit(OpCodes.Stloc, methodInfoLocal);

                if (mi.ContainsGenericParameters)
                {
                    // methodInfo = methodInfo.MakeGenericMethod(mi.GetGenericArguments());
                    il.Emit(OpCodes.Ldloc, methodInfoLocal);

                    Type[] genericTypes = mi.GetGenericArguments();
                    GenericArray<Type> typeArr = new GenericArray<Type>(il, genericTypes.Length);
                    for (int i = 0; i < genericTypes.Length; ++i)
                    {
                        typeArr.BeginSet(i);
                        il.Emit(OpCodes.Ldtoken, genericTypes[i]);
                        il.Emit(OpCodes.Call, getTypeFromHandleMethod);
                        typeArr.EndSet(typeof(Type));
                    }
                    typeArr.Load();

                    il.Emit(OpCodes.Callvirt, makeGenericMethodMethod);
                    il.Emit(OpCodes.Stloc, methodInfoLocal);
                }

                // object result = this.Invoke(methodInfo, args);
                LocalBuilder resultLocal = mi.ReturnType != typeof(void) ? il.DeclareLocal(typeof(object)) : null;
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldloc, methodInfoLocal);
                argsArr.Load();
                il.Emit(OpCodes.Callvirt, dispatchProxyInvokeMethod);

                if (resultLocal != null)
                    il.Emit(OpCodes.Stloc, resultLocal);
                else
                {
                    // drop the result for void methods
                    il.Emit(OpCodes.Pop);
                }

                for (int i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i].ParameterType.IsByRef)
                    {
                        args.BeginSet(i);
                        argsArr.Get(i);
                        args.EndSet(i, typeof(object));
                    }
                }

                if (resultLocal != null)
                {
                    // return (mi.ReturnType)result;
                    il.Emit(OpCodes.Ldloc, resultLocal);
                    Convert(il, typeof(object), mi.ReturnType, false);
                }

                il.Emit(OpCodes.Ret);

                tb.DefineMethodOverride(mdb, mi);
                return mdb;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="type"></param>
            /// <returns></returns>
            private static int GetTypeCode(Type type)
            {
                if (type == null)
                    return 0;   // TypeCode.Empty;

                if (type == typeof(bool))
                    return 3;   // TypeCode.Boolean;

                if (type == typeof(char))
                    return 4;   // TypeCode.Char;

                if (type == typeof(sbyte))
                    return 5;   // TypeCode.SByte;

                if (type == typeof(byte))
                    return 6;   // TypeCode.Byte;

                if (type == typeof(short))
                    return 7;   // TypeCode.Int16;

                if (type == typeof(ushort))
                    return 8;   // TypeCode.UInt16;

                if (type == typeof(int))
                    return 9;   // TypeCode.Int32;

                if (type == typeof(uint))
                    return 10;  // TypeCode.UInt32;

                if (type == typeof(long))
                    return 11;  // TypeCode.Int64;

                if (type == typeof(ulong))
                    return 12;  // TypeCode.UInt64;

                if (type == typeof(float))
                    return 13;  // TypeCode.Single;

                if (type == typeof(double))
                    return 14;  // TypeCode.Double;

                if (type == typeof(decimal))
                    return 15;  // TypeCode.Decimal;

                if (type == typeof(DateTime))
                    return 16;  // TypeCode.DateTime;

                if (type == typeof(string))
                    return 18;  // TypeCode.String;

                if (type.IsEnum)
                    return GetTypeCode(Enum.GetUnderlyingType(type));

                return 1;   // TypeCode.Object;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="il"></param>
            /// <param name="source"></param>
            /// <param name="target"></param>
            /// <param name="isAddress"></param>
            private static void Convert(ILGenerator il, Type source, Type target, bool isAddress)
            {
                Debug.Assert(!target.IsByRef);
                if (target == source)
                    return;

                if (source.IsByRef)
                {
                    Debug.Assert(!isAddress);
                    Type argType = source.GetElementType();
                    Ldind(il, argType);
                    Convert(il, argType, target, isAddress);
                    return;
                }

                if (target.IsValueType)
                {
                    if (source.IsValueType)
                    {
                        OpCode opCode = convOpCodes[GetTypeCode(target)];
                        Debug.Assert(!opCode.Equals(OpCodes.Nop));
                        il.Emit(opCode);
                    }
                    else
                    {
                        Debug.Assert(source.IsAssignableFrom(target));
                        il.Emit(OpCodes.Unbox, target);
                        if (!isAddress)
                            Ldind(il, target);
                    }
                }
                else if (target.IsAssignableFrom(source))
                {
                    if (source.IsValueType || source.IsGenericParameter)
                    {
                        if (isAddress)
                            Ldind(il, source);
                        il.Emit(OpCodes.Box, source);
                    }
                }
                else
                {
                    Debug.Assert(source.IsAssignableFrom(target) || target.IsInterface || source.IsInterface);
                    if (target.IsGenericParameter)
                        il.Emit(OpCodes.Unbox_Any, target);
                    else
                        il.Emit(OpCodes.Castclass, target);
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="il"></param>
            /// <param name="type"></param>
            private static void Ldind(ILGenerator il, Type type)
            {
                OpCode opCode = ldindOpCodes[GetTypeCode(type)];
                if (!opCode.Equals(OpCodes.Nop))
                    il.Emit(opCode);
                else
                    il.Emit(OpCodes.Ldobj, type);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="il"></param>
            /// <param name="type"></param>
            private static void Stind(ILGenerator il, Type type)
            {
                OpCode opCode = stindOpCodes[GetTypeCode(type)];
                if (!opCode.Equals(OpCodes.Nop))
                    il.Emit(opCode);
                else
                    il.Emit(OpCodes.Stobj, type);
            }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Returns a new instance of a proxy the derives from 'baseType' and implements 'interfaceType'.
        /// </summary>
        /// <param name="baseType"></param>
        /// <param name="interfaceType"></param>
        /// <returns></returns>
        internal static object CreateProxyInstance(Type baseType, Type interfaceType)
        {
            Debug.Assert(baseType != null);
            Debug.Assert(interfaceType != null);

            GeneratedTypeInfo proxiedType = GetProxyType(baseType, interfaceType);
            return Activator.CreateInstance(proxiedType.GeneratedType, new object[] { proxiedType.MethodInfos });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="baseType"></param>
        /// <param name="interfaceType"></param>
        /// <returns></returns>
        private static GeneratedTypeInfo GetProxyType(Type baseType, Type interfaceType)
        {
            lock (baseTypeAndInterfaceToGeneratedProxyType)
            {
                if (!baseTypeAndInterfaceToGeneratedProxyType.TryGetValue(baseType, out Dictionary<Type, GeneratedTypeInfo> interfaceToProxy))
                {
                    interfaceToProxy = new Dictionary<Type, GeneratedTypeInfo>();
                    baseTypeAndInterfaceToGeneratedProxyType[baseType] = interfaceToProxy;
                }

                if (!interfaceToProxy.TryGetValue(interfaceType, out GeneratedTypeInfo generatedProxy))
                {
                    generatedProxy = GenerateProxyType(baseType, interfaceType);
                    interfaceToProxy[interfaceType] = generatedProxy;
                }

                return generatedProxy;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="baseType"></param>
        /// <param name="interfaceType"></param>
        /// <returns></returns>
        private static GeneratedTypeInfo GenerateProxyType(Type baseType, Type interfaceType)
        {
            // Parameter validation is deferred until the point we need to create the proxy.
            // This prevents unnecessary overhead revalidating cached proxy types.

            // The interface type must be an interface, not a class
            if (!interfaceType.IsInterface)
            {
                // "T" is the generic parameter seen via the public contract
                throw new ArgumentException(string.Format("The type '{0}' must be an interface, not a class.", interfaceType.FullName), "T");
            }

            // The base type cannot be sealed because the proxy needs to subclass it.
            if (baseType.IsSealed)
            {
                // "TProxy" is the generic parameter seen via the public contract
                throw new ArgumentException(string.Format("The base type '{0}' cannot be sealed.", baseType.FullName), "TProxy");
            }

            // The base type cannot be abstract
            if (baseType.IsAbstract)
            {
                throw new ArgumentException(string.Format("The base type '{0}' cannot be abstract.", baseType.FullName), "TProxy");
            }

            // The base type must have a public default ctor
            if (baseType.GetConstructor(Type.EmptyTypes) == null)
            {
                throw new ArgumentException(string.Format("The base type '{0}' must have a public parameterless constructor.", baseType.FullName), "TProxy");
            }

            // Create a type that derives from 'baseType' provided by caller
            ProxyBuilder pb = proxyAssembly.CreateProxy($"{interfaceType.Name}Proxy", baseType);

            foreach (Type t in interfaceType.GetInterfaces())
                pb.AddInterfaceImpl(t);

            pb.AddInterfaceImpl(interfaceType);

            GeneratedTypeInfo generatedProxyType = pb.CreateType();
            return generatedProxyType;
        }
    } // internal static class DispatchProxyGenerator
} // namespace TridentFramework.RPC.Remoting.Proxies
