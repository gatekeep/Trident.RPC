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
//
// Based on code from the DiscUtils project. (http://discutils.codeplex.com/)
// Copyright (c) 2008-2011, Kenneth Bell
// Licensed under the MIT License (http://www.opensource.org/licenses/MIT)
//

using System;
using System.IO;

namespace TridentFramework.RPC.Utility
{
    /// <summary>
    /// This class implements a local file locator.
    /// </summary>
    public sealed class LocalFileLocator : IFileLocator
    {
        private string dir;

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalFileLocator"/> class.
        /// </summary>
        /// <param name="dir"></param>
        public LocalFileLocator(string dir)
        {
            this.dir = dir;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalFileLocator"/> class.
        /// </summary>
        /// <param name="basePath"></param>
        /// <param name="dir"></param>
        public LocalFileLocator(IFileLocator basePath, string dir)
        {
            this.dir = basePath.GetFullPath(dir);
        }

        /// <inheritdoc />
        public override bool Exists()
        {
            return Directory.Exists(dir);
        }

        /// <inheritdoc />
        public override bool Exists(string fileName)
        {
            return File.Exists(Path.Combine(dir, fileName));
        }

        /// <inheritdoc />
        public override void Move(IFileLocator locator, string fileName)
        {
            if (Exists(fileName) && locator.Exists())
                File.Move(Path.Combine(dir, fileName), Path.Combine(locator.GetFullPath(), fileName));
        }

        /// <inheritdoc />
        public override void Delete(string fileName)
        {
            if (Exists(fileName))
                File.Delete(Path.Combine(dir, fileName));
        }

        /// <inheritdoc />
        public override void Copy(string srcFileName, string destFileName)
        {
            if (Exists(srcFileName) && !Exists(destFileName))
                File.Copy(Path.Combine(dir, srcFileName), Path.Combine(dir, destFileName));
        }

        /// <inheritdoc />
        public override void Copy(IFileLocator locator, string fileName)
        {
            if (Exists(fileName) && locator.Exists())
                File.Copy(Path.Combine(dir, fileName), Path.Combine(locator.GetFullPath(), fileName));
        }

        /// <inheritdoc />
        public override bool PathExists(string directoryName)
        {
            return Directory.Exists(Path.Combine(dir, directoryName));
        }

        /// <inheritdoc />
        public override Stream Open(string fileName, FileMode mode, FileAccess access, FileShare share)
        {
            return new FileStream(Path.Combine(dir, fileName), mode, access, share);
        }

        /// <inheritdoc />
        public override IFileLocator GetRelativeLocator(string path)
        {
            return new LocalFileLocator(Path.Combine(dir, path));
        }

        /// <inheritdoc />
        public override string GetFullPath()
        {
            return Path.GetFullPath(dir);
        }

        /// <inheritdoc />
        public override string GetFullPath(string path)
        {
            string combinedPath = Path.Combine(dir, path);
            if (string.IsNullOrEmpty(combinedPath))
                return Environment.CurrentDirectory;
            else
                return Path.GetFullPath(combinedPath);
        }

        /// <inheritdoc />
        public override string GetDirectoryFromPath(string path)
        {
            return Path.GetDirectoryName(path);
        }

        /// <inheritdoc />
        public override string GetFileFromPath(string path)
        {
            return Path.GetFileName(path);
        }

        /// <inheritdoc />
        public override DateTime GetLastWriteTimeUtc(string path)
        {
            return File.GetLastWriteTimeUtc(Path.Combine(dir, path));
        }

        /// <inheritdoc />
        public override bool HasCommonRoot(IFileLocator other)
        {
            LocalFileLocator otherLocal = other as LocalFileLocator;
            if (otherLocal == null)
                return false;

            // if the paths have drive specifiers, then common root depends on them having a common
            // drive letter.
            string otherDir = otherLocal.dir;
            if (otherDir.Length >= 2 && dir.Length >= 2)
                if (otherDir[1] == ':' && dir[1] == ':')
                    return Char.ToUpperInvariant(otherDir[0]) == Char.ToUpperInvariant(dir[0]);

            return true;
        }

        /// <inheritdoc />
        public override string ResolveRelativePath(string path)
        {
            return _ResolveRelativePath(dir, path);
        }
    } // public sealed class LocalFileLocator : IFileLocator
} // namespace TridentFramework.RPC.Utility
