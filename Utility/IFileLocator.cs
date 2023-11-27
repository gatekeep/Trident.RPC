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
// Based on code from the DiscUtils project. (http://discutils.codeplex.com/)
// Copyright (c) 2008-2011, Kenneth Bell
// Licensed under the MIT License (http://www.opensource.org/licenses/MIT)
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TridentFramework.RPC.Utility
{
    /// <summary>
    /// Interface defines a class structure that can locate files.
    /// </summary>
    public abstract class IFileLocator
    {
        /**
         * Path Manipulation
         */

        /// <summary>
        /// Combines two paths.
        /// </summary>
        /// <param name="a">The first part of the path.</param>
        /// <param name="b">The second part of the path.</param>
        /// <returns>The combined path.</returns>
        public static string CombinePaths(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || (b.Length > 0 && b[0] == Path.DirectorySeparatorChar))
                return b;
            else if (string.IsNullOrEmpty(b))
                return a;
            else
                return a.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar +
                    b.TrimStart(Path.DirectorySeparatorChar);
        }

        /// <summary>
        /// Resolves a relative path into an absolute one.
        /// </summary>
        /// <param name="basePath">The base path to resolve from.</param>
        /// <param name="relativePath">The relative path.</param>
        /// <returns>The absolute path, so far as it can be resolved.  If the
        /// <paramref name="relativePath"/> contains more '..' characters than the
        /// base path contains levels of directory, the resultant string will be relative.
        /// For example: (TEMP\Foo.txt, ..\..\Bar.txt) gives (..\Bar.txt).</returns>
        protected static string _ResolveRelativePath(string basePath, string relativePath)
        {
            List<string> pathElements = new List<string>(basePath.Split(new char[] { Path.DirectorySeparatorChar },
                StringSplitOptions.RemoveEmptyEntries));
            if (!basePath.EndsWith(string.Empty + Path.DirectorySeparatorChar, StringComparison.Ordinal) && pathElements.Count > 0)
                pathElements.RemoveAt(pathElements.Count - 1);

            pathElements.AddRange(relativePath.Split(new char[] { Path.DirectorySeparatorChar },
                StringSplitOptions.RemoveEmptyEntries));

            int pos = 1;
            while (pos < pathElements.Count)
            {
                if (pathElements[pos] == ".")
                {
                    pathElements.RemoveAt(pos);
                }
                else if (pathElements[pos] == ".." && pos > 0 && pathElements[pos - 1][0] != '.')
                {
                    pathElements.RemoveAt(pos);
                    pathElements.RemoveAt(pos - 1);
                    pos--;
                }
                else
                {
                    pos++;
                }
            }

            string merged = string.Join(string.Empty + Path.DirectorySeparatorChar, pathElements.ToArray());
            if (relativePath.EndsWith(string.Empty + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                merged += string.Empty + Path.DirectorySeparatorChar;

            if (basePath.StartsWith(string.Empty + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                merged = string.Empty + Path.DirectorySeparatorChar + merged;
            else if (basePath.StartsWith(string.Empty + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                merged = string.Empty + Path.DirectorySeparatorChar + merged;

            return merged;
        }

        /// <summary>
        /// Resolve the given path.
        /// </summary>
        /// <param name="basePath"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        protected static string _ResolvePath(string basePath, string path)
        {
            if (!path.StartsWith(string.Empty + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                return _ResolveRelativePath(basePath, path);
            else
                return path;
        }

        /// <summary>
        /// Make a relative path to the given base path.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="basePath"></param>
        /// <returns></returns>
        protected static string _MakeRelativePath(string path, string basePath)
        {
            List<string> pathElements = new List<string>(path.Split(new char[] { Path.DirectorySeparatorChar },
                StringSplitOptions.RemoveEmptyEntries));
            List<string> basePathElements = new List<string>(basePath.Split(new char[] { Path.DirectorySeparatorChar },
                StringSplitOptions.RemoveEmptyEntries));

            if (!basePath.EndsWith(string.Empty + Path.DirectorySeparatorChar, StringComparison.Ordinal) && basePathElements.Count > 0)
                basePathElements.RemoveAt(basePathElements.Count - 1);

            // find first part of paths that don't match
            int i = 0;
            while (i < Math.Min(pathElements.Count - 1, basePathElements.Count))
            {
                if (pathElements[i].ToUpperInvariant() != basePathElements[i].ToUpperInvariant())
                    break;

                ++i;
            }

            // for each remaining part of the base path, insert '..'
            StringBuilder result = new StringBuilder();
            if (i == basePathElements.Count)
                result.Append(@".\");
            else if (i < basePathElements.Count)
            {
                for (int j = 0; j < basePathElements.Count - i; ++j)
                    result.Append(@"..\");
            }

            // for each remaining part of the path, add the path element
            for (int j = i; j < pathElements.Count - 1; ++j)
            {
                result.Append(pathElements[j]);
                result.Append(string.Empty + Path.DirectorySeparatorChar);
            }

            result.Append(pathElements[pathElements.Count - 1]);

            // if the target was a directory, put the terminator back
            if (path.EndsWith(string.Empty + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                result.Append(string.Empty + Path.DirectorySeparatorChar);

            return result.ToString();
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Checks if the base path exists.
        /// </summary>
        /// <returns></returns>
        public abstract bool Exists();

        /// <summary>
        /// Checks if the given file exists.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public abstract bool Exists(string fileName);

        /// <summary>
        /// Moves a specified file to a new location.
        /// </summary>
        /// <param name="locator"></param>
        /// <param name="fileName"></param>
        public abstract void Move(IFileLocator locator, string fileName);

        /// <summary>
        /// Deletes the specified file.
        /// </summary>
        /// <param name="fileName"></param>
        public abstract void Delete(string fileName);

        /// <summary>
        /// Copies an existing file to a new file. Overwriting a file of the same name is not allowed.
        /// </summary>
        /// <param name="srcFileName"></param>
        /// <param name="destFileName"></param>
        public abstract void Copy(string srcFileName, string destFileName);

        /// <summary>
        /// Copies an existing file to a new file. Overwriting a file of the same name is not allowed.
        /// </summary>
        /// <param name="locator"></param>
        /// <param name="fileName"></param>
        public abstract void Copy(IFileLocator locator, string fileName);

        /// <summary>
        /// Checks if the given path exists.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public abstract bool PathExists(string directoryName);

        /// <summary>
        /// Opens the given file.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="mode"></param>
        /// <param name="access"></param>
        /// <param name="share"></param>
        /// <returns></returns>
        public abstract Stream Open(string fileName, FileMode mode, FileAccess access, FileShare share);

        /// <summary>
        /// Returns the relative file locator.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public abstract IFileLocator GetRelativeLocator(string path);

        /// <summary>
        /// Returns the full path.
        /// </summary>
        /// <returns></returns>
        public abstract string GetFullPath();

        /// <summary>
        /// Returns the full path (including file component).
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public abstract string GetFullPath(string path);

        /// <summary>
        /// Gets the directory portion of the given path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public abstract string GetDirectoryFromPath(string path);

        /// <summary>
        /// Gets the file portion of the given path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public abstract string GetFileFromPath(string path);

        /// <summary>
        /// Returns the last modified/write time of the given file path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public abstract DateTime GetLastWriteTimeUtc(string path);

        /// <summary>
        /// Checks if this locator has a common root with another locator.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public abstract bool HasCommonRoot(IFileLocator other);

        /// <summary>
        /// Resolves a relative path to a full path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public abstract string ResolveRelativePath(string path);

        /// <summary>
        ///
        /// </summary>
        /// <param name="fileLocator"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public string MakeRelativePath(IFileLocator fileLocator, string path)
        {
            if (!HasCommonRoot(fileLocator))
                return null;

            string ourFullPath = GetFullPath(string.Empty) + @"\";
            string otherFullPath = fileLocator.GetFullPath(path);

            return _MakeRelativePath(otherFullPath, ourFullPath);
        }
    } // public abstract class FileLocator
} // namespace TridentFramework.RPC.Utility
