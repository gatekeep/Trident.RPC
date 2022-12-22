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
/*
 * Based on code from Lidgren Network v3
 * Copyright (C) 2010 Michael Lidgren., All Rights Reserved.
 * Licensed under MIT License.
 */

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

using TridentFramework.RPC.Net.Message;
using TridentFramework.RPC.Utility;

namespace TridentFramework.RPC.Net.Encryption
{
    /// <summary>
    /// Base class that all symmetric encryption algorithms should derive.
    /// </summary>
    public abstract class CryptoProviderBase : IMessageEncryption
    {
        protected SymmetricAlgorithm algorithm;

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new insatnce of the <see cref="CryptoProviderBase"/> class.
        /// </summary>
        /// <param name="algo"></param>
        public CryptoProviderBase(SymmetricAlgorithm algo)
        {
            algorithm = algo;
            algorithm.GenerateKey();
            algorithm.GenerateIV();
        }

        /// <summary>
        /// Sets the algorithm Key and IV.
        /// </summary>
        /// <param name="str"></param>
        public virtual void SetKey(string str)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(str);
            SetKey(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// Sets the algorithm Key and IV.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public virtual void SetKey(byte[] data, int offset, int count)
        {
            int len = algorithm.Key.Length;

            byte[] key = new byte[len];
            for (int i = 0; i < len; i++)
                key[i] = data[offset + (i % count)];
            algorithm.Key = key;

            len = algorithm.IV.Length;
            key = new byte[len];
            for (int i = 0; i < len; i++)
                key[len - 1 - i] = data[offset + (i % count)];

            algorithm.IV = key;
        }

        /// <summary>
        /// Encrypt outgoing message
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public virtual bool Encrypt(OutgoingMessage msg)
        {
            try
            {
                int unEncLenBits = msg.BitLength;
                if (unEncLenBits < 0)
                    return false;

                MemoryStream ms = new MemoryStream();
                CryptoStream cs = new CryptoStream(ms, algorithm.CreateEncryptor(), CryptoStreamMode.Write);
                cs.Write(msg.Data, 0, msg.LengthBytes);
                cs.Close();

                // get results
                byte[] arr = ms.ToArray();
                ms.Close();

                msg.EnsureBufferSize((arr.Length + 4) * 8);
                msg.BitLength = 0; // reset write pointer
                msg.Write((uint)unEncLenBits);
                msg.Write(arr);
                msg.BitLength = (arr.Length + 4) * 8;

                RPCLogger.Trace("Encrypted " + msg.ToString());
                return true;
            }
            catch (Exception e)
            {
                RPCLogger.WriteError("Failed to encrypt message!");
                RPCLogger.StackTrace(e, false);
                return false;
            }
        }

        /// <summary>
        /// Decrypt incoming message
        /// </summary>
        /// <returns></returns>
        public virtual bool Decrypt(IncomingMessage msg)
        {
            try
            {
                int unEncLenBits = (int)msg.ReadUInt32();
                if (unEncLenBits < 0)
                    return false;
                if (unEncLenBits > msg.BitLength)
                {
#if DEBUG
                    RPCLogger.WriteWarning("DEBUG: Unencrypted bit length beyond message bit length!");
#endif
                    return false;
                }

                MemoryStream ms = new MemoryStream(msg.Data, 4, msg.LengthBytes - 4);
                CryptoStream cs = new CryptoStream(ms, algorithm.CreateDecryptor(), CryptoStreamMode.Read);

                int byteLen = NetUtility.BytesToHoldBits(unEncLenBits);
                if (byteLen < 0)
                    return false;

                byte[] result = new byte[byteLen];
                cs.Read(result, 0, byteLen);
                cs.Close();

                // TODO: recycle existing msg

                msg.Data = result;
                msg.bitLength = unEncLenBits;
                msg.readPosition = 0;

                RPCLogger.Trace("Decrypted " + msg.ToString());
                return true;
            }
            catch (Exception e)
            {
                RPCLogger.WriteError("Failed to decrypt message!");
                RPCLogger.StackTrace(e, false);
                return false;
            }
        }
    } // public abstract class CryptoProviderBase : IMessageEncryption
} // namespace TridentFramework.RPC.Net.Encryption
