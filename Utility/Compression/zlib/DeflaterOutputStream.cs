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
// Copyright © 2000-2018 SharpZipLib Contributors
// Licensed under the MIT License (http://www.opensource.org/licenses/MIT)
//

using System;
using System.IO;

namespace TridentFramework.Compression.zlib
{
    /// <summary>
    /// A special stream deflating or compressing the bytes that are
    /// written to it.  It uses a Deflater to perform actual deflating.
    /// </summary>
    public class DeflaterOutputStream : Stream
    {
        /// <summary>
        /// Base stream the deflater depends on.
        /// </summary>
        protected Stream baseOutputStream;

        /// <summary>
        /// The deflater which is used to deflate the stream.
        /// </summary>
        protected Deflater deflater;

        /// <summary>
        /// This buffer is used temporarily to retrieve the bytes from the
        /// deflater and write them to the underlying output stream.
        /// </summary>
        private byte[] buffer;

        private bool isClosed;

        /*
        ** Properties
        */

        ///	<summary>
        /// Allows client to determine if an entry can be patched after its added
        /// </summary>
        public bool CanPatchEntries
        {
            get { return baseOutputStream.CanSeek; }
        }

        /// <inheritdoc />
        public override bool CanRead
        {
            get { return false; }
        }

        /// <inheritdoc />
        public override bool CanSeek
        {
            get { return false; }
        }

        /// <inheritdoc />
        public override bool CanWrite
        {
            get { return baseOutputStream.CanWrite; }
        }

        /// <summary>
        /// Gets or sets a flag indicating ownership of underlying stream.
        /// When the flag is true <see cref="Stream.Dispose()" /> will close the underlying stream also.
        /// </summary>
        /// <remarks>The default value is true.</remarks>
        public bool IsStreamOwner { get; set; } = true;

        /// <inheritdoc />
        public override long Length
        {
            get { return baseOutputStream.Length; }
        }

        /// <inheritdoc />
        public override long Position
        {
            get { return baseOutputStream.Position; }
            set { throw new NotSupportedException("Position property not supported"); }
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Creates a new DeflaterOutputStream with a default Deflater and default buffer size.
        /// </summary>
        /// <param name="baseOutputStream">
        /// the output stream where deflated output should be written.
        /// </param>
        public DeflaterOutputStream(Stream baseOutputStream)
            : this(baseOutputStream, new Deflater(), 512)
        {
            /* stub */
        }

        /// <summary>
        /// Creates a new DeflaterOutputStream with the given Deflater and
        /// default buffer size.
        /// </summary>
        /// <param name="baseOutputStream">
        /// the output stream where deflated output should be written.
        /// </param>
        /// <param name="deflater">
        /// the underlying deflater.
        /// </param>
        public DeflaterOutputStream(Stream baseOutputStream, Deflater deflater)
            : this(baseOutputStream, deflater, 512)
        {
            /* stub */
        }

        /// <summary>
        /// Creates a new DeflaterOutputStream with the given Deflater and
        /// buffer size.
        /// </summary>
        /// <param name="baseOutputStream">
        /// The output stream where deflated output is written.
        /// </param>
        /// <param name="deflater">
        /// The underlying deflater to use
        /// </param>
        /// <param name="bufferSize">
        /// The buffer size in bytes to use when deflating (minimum value 512)
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// bufsize is less than or equal to zero.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// baseOutputStream does not support writing
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// deflater instance is null
        /// </exception>
        public DeflaterOutputStream(Stream baseOutputStream, Deflater deflater, int bufferSize)
        {
            if (baseOutputStream == null)
                throw new ArgumentNullException(nameof(baseOutputStream));
            if (baseOutputStream.CanWrite == false)
                throw new ArgumentException("Must support writing", nameof(baseOutputStream));
            if (bufferSize < 512)
                throw new ArgumentOutOfRangeException(nameof(bufferSize));

            this.baseOutputStream = baseOutputStream;
            buffer = new byte[bufferSize];
            this.deflater = deflater ?? throw new ArgumentNullException(nameof(deflater));
        }

        /// <summary>
        /// Finishes the stream by calling finish() on the deflater.
        /// </summary>
        /// <exception cref="Exception">
        /// Not all input is deflated
        /// </exception>
        public virtual void Finish()
        {
            deflater.Finish();
            while (!deflater.IsFinished)
            {
                int len = deflater.Deflate(buffer, 0, buffer.Length);
                if (len <= 0)
                    break;

                baseOutputStream.Write(buffer, 0, len);
            }

            if (!deflater.IsFinished)
                throw new Exception("Can't deflate all input?");

            baseOutputStream.Flush();
        }

        /// <inheritdoc />
        public override void Flush()
        {
            deflater.Flush();
            Deflate(true);
            baseOutputStream.Flush();
        }

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("DeflaterOutputStream Read not supported");
        }

        /// <inheritdoc />
        public override int ReadByte()
        {
            throw new NotSupportedException("DeflaterOutputStream ReadByte not supported");
        }

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException("DeflaterOutputStream Seek not supported");
        }

        /// <inheritdoc />
        public override void SetLength(long value)
        {
            throw new NotSupportedException("DeflaterOutputStream SetLength not supported");
        }

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count)
        {
            deflater.SetInput(buffer, offset, count);
            Deflate();
        }

        /// <inheritdoc />
        public override void WriteByte(byte value)
        {
            byte[] b = new byte[1];
            b[0] = value;
            Write(b, 0, 1);
        }

        /// <summary>
        /// Deflates everything in the input buffers.  This will call
        /// <code>def.deflate()</code> until all bytes from the input buffers
        /// are processed.
        /// </summary>
        protected void Deflate()
        {
            Deflate(false);
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (!isClosed)
            {
                isClosed = true;

                try
                {
                    Finish();
                }
                finally
                {
                    if (IsStreamOwner)
                        baseOutputStream.Dispose();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="flushing"></param>
        private void Deflate(bool flushing)
        {
            while (flushing || !deflater.IsNeedingInput)
            {
                int deflateCount = deflater.Deflate(buffer, 0, buffer.Length);

                if (deflateCount <= 0)
                    break;

                baseOutputStream.Write(buffer, 0, deflateCount);
            }

            if (!deflater.IsNeedingInput)
                throw new Exception("DeflaterOutputStream can't deflate all input?");
        }
    } // public class DeflaterOutputStream : Stream
} // namespace TridentFramework.Compression.zlib
