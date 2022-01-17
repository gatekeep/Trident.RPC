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
/*
 * Based on code from .NET Reference Source
 * Copyright (c) .NET Foundation and Contributors., All Rights Reserved.
 * Licensed under MIT License.
 */

using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace TridentFramework.RPC.Remoting.Proxies
{
    /// <summary>
    /// 
    /// </summary>
    internal static class IgnoreAccessChecksToAttributeBuilder
    {
        /*
        ** Methods
        */

        /// <summary>
        /// Generate the declaration for the IgnoresAccessChecksToAttribute type.
        /// This attribute will be both defined and used in the dynamic assembly.
        /// Each usage identifies the name of the assembly containing non-public
        /// types the dynamic assembly needs to access.  Normally those types
        /// would be inaccessible, but this attribute allows them to be visible.
        /// It works like a reverse InternalsVisibleToAttribute.
        /// This method returns the ConstructorInfo of the generated attribute.
        /// </summary>
        public static ConstructorInfo AddToModule(ModuleBuilder mb)
        {
            TypeBuilder attributeTypeBuilder = mb.DefineType("System.Runtime.CompilerServices.IgnoresAccessChecksToAttribute", 
                TypeAttributes.Public | TypeAttributes.Class, typeof(Attribute));

            // create backing field as:
            // private string assemblyName;
            FieldBuilder assemblyNameField = attributeTypeBuilder.DefineField("assemblyName", typeof(string), FieldAttributes.Private);

            // create ctor as:
            // public IgnoresAccessChecksToAttribute(string)
            ConstructorBuilder constructorBuilder = attributeTypeBuilder.DefineConstructor(MethodAttributes.Public,
                CallingConventions.HasThis, new Type[] { assemblyNameField.FieldType });

            ILGenerator il = constructorBuilder.GetILGenerator();

            // create ctor body as:
            // this.assemblyName = {ctor parameter 0}
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg, 1);
            il.Emit(OpCodes.Stfld, assemblyNameField);

            // return
            il.Emit(OpCodes.Ret);

            // define property as:
            // public string AssemblyName {get { return this.assemblyName; } }
            PropertyBuilder propertyBuilder = attributeTypeBuilder.DefineProperty("AssemblyName", PropertyAttributes.None,
                CallingConventions.HasThis, returnType: typeof(string), parameterTypes: null);

            MethodBuilder getterMethodBuilder = attributeTypeBuilder.DefineMethod("get_AssemblyName", MethodAttributes.Public, 
                CallingConventions.HasThis, returnType: typeof(string), parameterTypes: null);
            propertyBuilder.SetGetMethod(getterMethodBuilder);

            // generate body:
            // return this.assemblyName;
            il = getterMethodBuilder.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, assemblyNameField);
            il.Emit(OpCodes.Ret);

            // generate the AttributeUsage attribute for this attribute type:
            // [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
            TypeInfo attributeUsageTypeInfo = typeof(AttributeUsageAttribute).GetTypeInfo();

            // find the ctor that takes only AttributeTargets
            ConstructorInfo attributeUsageConstructorInfo = attributeUsageTypeInfo.DeclaredConstructors.
                Single(c => c.GetParameters().Length == 1 && c.GetParameters()[0].ParameterType == typeof(AttributeTargets));

            // find the property to set AllowMultiple
            PropertyInfo allowMultipleProperty = attributeUsageTypeInfo.DeclaredProperties.
                Single(f => string.Equals(f.Name, "AllowMultiple"));

            // create a builder to construct the instance via the ctor and property
            CustomAttributeBuilder customAttributeBuilder = new CustomAttributeBuilder(attributeUsageConstructorInfo, 
                new object[] { AttributeTargets.Assembly }, new PropertyInfo[] { allowMultipleProperty }, new object[] { true });

            // attach this attribute instance to the newly defined attribute type
            attributeTypeBuilder.SetCustomAttribute(customAttributeBuilder);

            // make the TypeInfo real so the constructor can be used.
            return attributeTypeBuilder.CreateTypeInfo().DeclaredConstructors.Single();
        }
    } // internal static class IgnoreAccessChecksToAttributeBuilder
} // namespace TridentFramework.RPC.Remoting.Proxies
