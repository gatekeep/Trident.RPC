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
 * Copyright (C) Microsoft Corporation., All Rights Reserved.
 * Licensed under MIT License.
 */

using System;
using System.Collections.Specialized;
using System.Text;
using System.Web;

namespace TridentFramework.RPC
{
    /// <summary>
    /// 
    /// </summary>
    internal abstract class UriTemplateQueryValue
    {
        private readonly UriTemplatePartType nature;
        private static UriTemplateQueryValue empty = new EmptyUriTemplateQueryValue();

        /*
        ** Classes
        */

        /// <summary>
        /// 
        /// </summary>
        private class EmptyUriTemplateQueryValue : UriTemplateQueryValue
        {
            /*
            ** Methods
            */

            /// <summary>
            /// Initializes a new instance of the <see cref="EmptyUriTemplateQueryValue"/> class.
            /// </summary>
            public EmptyUriTemplateQueryValue()
                : base(UriTemplatePartType.Literal)
            {
                /* stub */
            }
            
            /// <inheritdoc />
            public override void Bind(string keyName, string[] values, ref int valueIndex, StringBuilder query)
            {
                query.AppendFormat("&{0}", HttpUtility.UrlEncode(keyName, Encoding.UTF8));
            }

            /// <inheritdoc />
            public override bool IsEquivalentTo(UriTemplateQueryValue other)
            {
                return (other == UriTemplateQueryValue.Empty);
            }

            /// <inheritdoc />
            public override void Lookup(string value, NameValueCollection boundParameters)
            {
                /* stub */
            }
        } // private class EmptyUriTemplateQueryValue : UriTemplateQueryValue

        /*
        ** Properties
        */

        /// <summary>
        /// 
        /// </summary>
        public static UriTemplateQueryValue Empty
        {
            get { return UriTemplateQueryValue.empty; }
        }

        /// <summary>
        /// 
        /// </summary>
        public UriTemplatePartType Nature
        {
            get { return this.nature; }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="UriTemplateQueryValue"/> class.
        /// </summary>
        /// <param name="nature"></param>
        protected UriTemplateQueryValue(UriTemplatePartType nature)
        {
            this.nature = nature;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="template"></param>
        /// <returns></returns>
        public static UriTemplateQueryValue CreateFromUriTemplate(string value, UriTemplate template)
        {
            // checking for empty value
            if (value == null)
                return UriTemplateQueryValue.Empty;

            // identifying the type of value - Literal|Compound|Variable
            switch (UriTemplateHelpers.IdentifyPartType(value))
            {
                case UriTemplatePartType.Literal:
                    return UriTemplateLiteralQueryValue.CreateFromUriTemplate(value);

                case UriTemplatePartType.Compound:
                    throw new InvalidOperationException(string.Format("Query cannot have a compound value; {0}", template.originalTemplate));

                case UriTemplatePartType.Variable:
                    return new UriTemplateVariableQueryValue(template.AddQueryVariable(value.Substring(1, value.Length - 2)));

                default:
                    return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="utqv"></param>
        /// <returns></returns>
        public static bool IsNullOrEmpty(UriTemplateQueryValue utqv)
        {
            if (utqv == null)
                return true;

            if (utqv == UriTemplateQueryValue.Empty)
                return true;

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="keyName"></param>
        /// <param name="values"></param>
        /// <param name="valueIndex"></param>
        /// <param name="query"></param>
        public abstract void Bind(string keyName, string[] values, ref int valueIndex, StringBuilder query);

        /// <summary>
        /// Indicates whether a <see cref="UriTemplateQueryValue"/> is structurally equivalent to another.
        /// </summary>
        /// <param name="other">The UriTemplateQueryValue to compare.</param>
        /// <returns></returns>
        public abstract bool IsEquivalentTo(UriTemplateQueryValue other);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="boundParameters"></param>
        public abstract void Lookup(string value, NameValueCollection boundParameters);
    } // internal abstract class UriTemplateQueryValue
} // namespace TridentFramework.RPC
