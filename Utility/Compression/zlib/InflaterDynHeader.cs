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
//
// Based on code from the SharpZipLib project. (https://github.com/icsharpcode/SharpZipLib.git)
// Copyright � 2000-2018 SharpZipLib Contributors
// Licensed under the MIT License (http://www.opensource.org/licenses/MIT)
//

using System;
using System.Collections.Generic;

namespace TridentFramework.Compression.zlib
{
    /// <summary>
    /// 
    /// </summary>
    internal class InflaterDynHeader
    {
        // maximum data code lengths to read
        private const int CODELEN_MAX = LITLEN_MAX + DIST_MAX;

        // maximum number of distance codes
        private const int DIST_MAX = 30;

        // maximum number of literal/length codes
        private const int LITLEN_MAX = 286;
        // maximum meta code length codes to read
        private const int META_MAX = 19;

        private static readonly int[] MetaCodeLengthIndex =
            { 16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15 };

        private readonly StreamManipulator input;

        private readonly IEnumerator<bool> state;

        private readonly IEnumerable<bool> stateMachine;

        private byte[] codeLengths = new byte[CODELEN_MAX];

        private InflaterHuffmanTree distTree;

        private int litLenCodeCount, distanceCodeCount, metaCodeCount;

        private InflaterHuffmanTree litLenTree;

        /// <summary>
        /// Get distance huffman tree, must not be used before <see cref="AttemptRead"/> has returned true
        /// </summary>
        /// <exception cref="Exception">If hader has not been successfully read by the state machine</exception>
        public InflaterHuffmanTree DistanceTree
            => distTree ?? throw new Exception("Header properties were accessed before header had been successfully read");

        /// <summary>
        /// Get literal/length huffman tree, must not be used before <see cref="AttemptRead"/> has returned true
        /// </summary>
        /// <exception cref="Exception">If hader has not been successfully read by the state machine</exception>
        public InflaterHuffmanTree LiteralLengthTree
            => litLenTree ?? throw new Exception("Header properties were accessed before header had been successfully read");

        /*
        ** Methods
        */

        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        public InflaterDynHeader(StreamManipulator input)
        {
            this.input = input;
            stateMachine = CreateStateMachine();
            state = stateMachine.GetEnumerator();
        }

        /// <summary>
        /// Continue decoding header from <see cref="input"/> until more bits are needed or decoding has been completed
        /// </summary>
        /// <returns>Returns whether decoding could be completed</returns>
        public bool AttemptRead() => !state.MoveNext() || state.Current;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private IEnumerable<bool> CreateStateMachine()
        {
            // Read initial code length counts from header
            while (!input.TryGetBits(5, ref litLenCodeCount, 257))
                yield return false;
            while (!input.TryGetBits(5, ref distanceCodeCount, 1))
                yield return false;
            while (!input.TryGetBits(4, ref metaCodeCount, 4))
                yield return false;
            var dataCodeCount = litLenCodeCount + distanceCodeCount;

            if (litLenCodeCount > LITLEN_MAX)
                throw new Exception(nameof(litLenCodeCount));
            if (distanceCodeCount > DIST_MAX)
                throw new Exception(nameof(distanceCodeCount));
            if (metaCodeCount > META_MAX)
                throw new Exception(nameof(metaCodeCount));

            // Load code lengths for the meta tree from the header bits
            for (int i = 0; i < metaCodeCount; i++)
            {
                while (!input.TryGetBits(3, ref codeLengths, MetaCodeLengthIndex[i]))
                    yield return false;
            }

            var metaCodeTree = new InflaterHuffmanTree(codeLengths);

            // Decompress the meta tree symbols into the data table code lengths
            int index = 0;
            while (index < dataCodeCount)
            {
                byte codeLength;
                int symbol;

                while ((symbol = metaCodeTree.GetSymbol(input)) < 0)
                    yield return false;

                if (symbol < 16)
                {
                    // append literal code length
                    codeLengths[index++] = (byte)symbol;
                }
                else
                {
                    int repeatCount = 0;

                    if (symbol == 16) // Repeat last code length 3..6 times
                    {
                        if (index == 0)
                            throw new Exception("Cannot repeat previous code length when no other code length has been read");

                        codeLength = codeLengths[index - 1];

                        // 2 bits + 3, [3..6]
                        while (!input.TryGetBits(2, ref repeatCount, 3))
                            yield return false;
                    }
                    else if (symbol == 17) // Repeat zero 3..10 times
                    {
                        codeLength = 0;

                        // 3 bits + 3, [3..10]
                        while (!input.TryGetBits(3, ref repeatCount, 3))
                            yield return false;
                    }
                    else // (symbol == 18), Repeat zero 11..138 times
                    {
                        codeLength = 0;

                        // 7 bits + 11, [11..138]
                        while (!input.TryGetBits(7, ref repeatCount, 11))
                            yield return false;
                    }

                    if (index + repeatCount > dataCodeCount)
                        throw new Exception("Cannot repeat code lengths past total number of data code lengths");

                    while (repeatCount-- > 0)
                        codeLengths[index++] = codeLength;
                }
            }

            if (codeLengths[256] == 0)
                throw new Exception("Inflater dynamic header end-of-block code missing");

            litLenTree = new InflaterHuffmanTree(new ArraySegment<byte>(codeLengths, 0, litLenCodeCount));
            distTree = new InflaterHuffmanTree(new ArraySegment<byte>(codeLengths, litLenCodeCount, distanceCodeCount));

            yield return true;
        }
    } // internal class InflaterDynHeader
} // namespace TridentFramework.Compression.zlib
