/*
 * Copyright (c) 2008-2020 Bryan Biedenkapp., All Rights Reserved.
 * MIT Open Source. Use is subject to license terms.
 * DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
 */

using System;
using System.IO;

namespace TridentFramework.Compression.LZMA
{
    /// <summary>
    /// </summary>
    public static class Lzma
    {
        private static readonly int dictionary = 1 << 23;
        private static readonly bool eos = false;

        private static readonly CoderPropID[] propIDs =
        {
            CoderPropID.DictionarySize, CoderPropID.PosStateBits, CoderPropID.LitContextBits, CoderPropID.LitPosBits,
            CoderPropID.Algorithm, CoderPropID.NumFastBytes, CoderPropID.MatchFinder, CoderPropID.EndMarker
        };

        // these are the default properties, keeping it simple for now:
        private static readonly object[] properties =
        {
            dictionary, // Dictionary Size
            2, // Pos State Bits
            3, // Lit Context Bits
            0, // Lit Pos Bits
            2, // Algorithm
            128, // Num Fast Bytes
            "bt4", // Match Finder Signature
            eos // End Marker
        };

        /*
        ** Methods
        */

        /// <summary>
        /// </summary>
        /// <param name="inputBytes"></param>
        /// <returns></returns>
        public static byte[] Compress(byte[] inputBytes)
        {
            MemoryStream inStream = new MemoryStream(inputBytes);
            MemoryStream outStream = new MemoryStream();

            LzmaEncoder encoder = new LzmaEncoder();
            encoder.SetCoderProperties(propIDs, properties);
            encoder.WriteCoderProperties(outStream);

            long fileSize = inStream.Length;
            for (int i = 0; i < 8; i++)
                outStream.WriteByte((byte)(fileSize >> (8 * i)));

            encoder.Code(inStream, outStream, -1, -1, null);
            return outStream.ToArray();
        }

        /// <summary>
        /// </summary>
        /// <param name="inputBytes"></param>
        /// <returns></returns>
        public static byte[] Decompress(byte[] inputBytes)
        {
            MemoryStream newInStream = new MemoryStream(inputBytes);

            LzmaDecoder decoder = new LzmaDecoder();

            newInStream.Seek(0, 0);
            MemoryStream newOutStream = new MemoryStream();

            byte[] properties2 = new byte[5];
            if (newInStream.Read(properties2, 0, 5) != 5)
                throw new Exception("input .lzma is too short");

            long outSize = 0;
            for (int i = 0; i < 8; i++)
            {
                int v = newInStream.ReadByte();
                if (v < 0)
                    throw new Exception("Can't Read 1");
                outSize |= (long)(byte)v << (8 * i);
            }

            decoder.SetDecoderProperties(properties2);

            long compressedSize = newInStream.Length - newInStream.Position;
            decoder.Code(newInStream, newOutStream, compressedSize, outSize, null);

            byte[] b = newOutStream.ToArray();
            return b;
        }
    } // public static class Lzma
} // namespace TridentFramework.Compression.LZMA
