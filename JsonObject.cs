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
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using TridentFramework.RPC.Utility;

namespace TridentFramework.RPC
{
    /// <summary>
    /// Helper class to serialize/deserialize objects to and from JSON.
    /// </summary>
    public sealed class JsonObject
    {
        /*
        ** Methods
        */

        /// <summary>
        /// Helper to generate a JSON string from the given object and type.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="obj"></param>
        /// <param name="objType"></param>
        /// <param name="base64Encode"></param>
        /// <returns></returns>
        public static JToken JTokenFromObject(string propertyName, object obj, Type objType, bool base64Encode)
        {
            string serializedObj = string.Empty;
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(objType);
            using (MemoryStream strm = new MemoryStream())
            {
                serializer.WriteObject(strm, obj);

                strm.Position = 0;

                StreamReader strReader = new StreamReader(strm);
                serializedObj = strReader.ReadToEnd();
            }

            JToken token = null;
            if (base64Encode)
            {
                UTF8Encoding utf8 = new UTF8Encoding();
                serializedObj = Convert.ToBase64String(utf8.GetBytes(serializedObj));
                token = new JValue(serializedObj);
            }
            else
                token = JToken.Parse(serializedObj);

            JToken ret = null;
            if (propertyName != null)
                ret = new JProperty(propertyName, token);
            else
                ret = token;
            return ret;
        }

        /// <summary>
        /// Helper to generate a object from a <see cref="JToken"/> of the given type.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="objType"></param>
        /// <param name="base64Decode"></param>
        /// <returns></returns>
        public static object ObjectFromJToken(JToken token, Type objType, bool base64Decode)
        {
            string tokenVal = token.ToString();
            if (base64Decode)
            {
                UTF8Encoding utf8 = new UTF8Encoding();
                tokenVal = utf8.GetString(Convert.FromBase64String(tokenVal));
            }

            object deserializedObj = null;
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(objType, new DataContractJsonSerializerSettings()
            {
                IgnoreExtensionDataObject = true // this will actually also ignore unknowns (like incorrect data types)
            });
            try
            {
                using (MemoryStream strm = new MemoryStream())
                {
                    using (StreamWriter strWriter = new StreamWriter(strm))
                    {
                        strWriter.Write(tokenVal);
                        strWriter.Flush();

                        strm.Position = 0;
                        deserializedObj = serializer.ReadObject(strm);
                    }
                }
            }
            catch (Exception e)
            {
                RPCLogger.StackTrace(e, false);
                deserializedObj = token.ToObject(objType);
            }
            return deserializedObj;
        }
    } // public sealed class JsonObject
} // namespace TridentFramework.RPC
