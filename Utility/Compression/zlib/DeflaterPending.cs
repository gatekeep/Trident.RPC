/*
 * Copyright (c) 2008-2020 Bryan Biedenkapp., All Rights Reserved.
 * MIT Open Source. Use is subject to license terms.
 * DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
 */
//
// Based on code from the SharpZipLib project. (https://github.com/icsharpcode/SharpZipLib.git)
// Copyright © 2000-2018 SharpZipLib Contributors
// Licensed under the MIT License (http://www.opensource.org/licenses/MIT)
//

namespace TridentFramework.Compression.zlib
{
	/// <summary>
	/// This class stores the pending output of the Deflater.
	/// </summary>
	public class DeflaterPending : PendingBuffer
	{
		/// <summary>
		/// Construct instance with default buffer size
		/// </summary>
		public DeflaterPending() : base(DeflaterConstants.PENDING_BUF_SIZE)
		{
		}
    } // public class DeflaterPending : PendingBuffer
} // namespace TridentFramework.Compression.zlib
