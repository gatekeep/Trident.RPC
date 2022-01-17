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

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

using TridentFramework.RPC.Utility;

namespace TridentFramework.RPC.Http.Service
{
    /// <summary>
    /// Implements a simple templating system.
    /// </summary>
    public class Template
    {
        private string tplFileName;
        private string tplDirectoryName;

        private Dictionary<string, string> variables;
        private Dictionary<string, List<Dictionary<string, string>>> blockVariables;

        private string uncompiledCode;

        /**
         * Internal Methods
         */

        /// <summary>
        /// Internal function to compile a template block.
        /// </summary>
        /// <param name="blockName">Block Name</param>
        /// <param name="blockTemplate">Block Template</param>
        private string CompileBlock(string blockName, string blockTemplate)
        {
            // copy uncompiled block template
            string compiledBlock = string.Empty;

            // check if the block var dictionary contains this key
            if (blockVariables.ContainsKey(blockName))
            {
                // iterate through block variables and replace
                List<Dictionary<string, string>> blockVars = blockVariables[blockName];
                for (int i = 0; i < blockVars.Count; i++)
                {
                    string compiledTpl = blockTemplate;

                    // iterate through top-level variables and replace
                    foreach (KeyValuePair<string, string> kvp in blockVars[i])
                    {
                        compiledTpl = compiledTpl.Replace("{" + blockName + "." + kvp.Key + "}", kvp.Value);

                        // check if the key is a "guid" if so replace "sanitized" versions
                        if (kvp.Key.ToUpper().Contains("GUID"))
                            compiledTpl = compiledTpl.Replace("{" + blockName + ".SANITIZED_" + kvp.Key + "}", kvp.Value.Replace("-", string.Empty));
                    }

                    // append compiled template to compiled block
                    compiledBlock += compiledTpl;
                }

                // if the compiled block is still empty, just fill it with the template
                if (compiledBlock == string.Empty)
                    compiledBlock += blockTemplate;
            }
            return compiledBlock;
        }

        /*
        ** Methods
        */

        /// <summary>
        /// Initializes a new instance of the <see cref="Template"/> class.
        /// </summary>
        /// <param name="tplFileName"></param>
        /// <param name="variables"></param>
        /// <param name="blockVariables"></param>
        private Template(string tplFileName, Dictionary<string, string> variables,
            Dictionary<string, List<Dictionary<string, string>>> blockVariables)
        {
            this.tplFileName = Path.GetFileName(tplFileName);
            this.tplDirectoryName = Path.GetDirectoryName(tplFileName);

            this.variables = variables;
            this.blockVariables = blockVariables;

            this.uncompiledCode = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Template"/> class.
        /// </summary>
        /// <param name="tplFileName"></param>
        public Template(string tplFileName)
            : this(tplFileName, new Dictionary<string, string>(), new Dictionary<string, List<Dictionary<string, string>>>())
        {
            /* stub */
        }

        /// <summary>
        /// Assign the entire variables dictionary.
        /// </summary>
        /// <param name="variables"></param>
        public void AssignVars(Dictionary<string, string> variables)
        {
            // iterate through input dictionary and add variables where possible
            foreach (KeyValuePair<string, string> kvp in variables)
            {
                if (this.variables.ContainsKey(kvp.Key))
                    this.variables[kvp.Key] = kvp.Value;
                else
                    this.variables.Add(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// Assign the entire variables dictionary for the given block.
        /// </summary>
        /// <param name="block">Block to assign variables</param>
        /// <param name="variables"></param>
        public void AssignBlockVars(string block, Dictionary<string, string> variables)
        {
            if (!blockVariables.ContainsKey(block))
            {
                // create a new list for the block vars
                List<Dictionary<string, string>> varList = new List<Dictionary<string, string>>();
                varList.Add(variables);

                // add list to the block var dictionary
                blockVariables.Add(block, varList);
            }
            else
                blockVariables[block].Add(variables);
        }

        /// <summary>
        /// Assign a new variable name.
        /// </summary>
        /// <param name="varName">Variable Name</param>
        /// <param name="varValue">Variable Value</param>
        public void AssignVar(string varName, string varValue)
        {
            if (!variables.ContainsKey(varName))
                variables.Add(varName, varValue);
            else
                variables[varName] = varValue;
        }

        /// <summary>
        /// Determines if the specified variable is set or not.
        /// </summary>
        /// <param name="varName">Variable Name</param>
        /// <returns></returns>
        public bool IsVarSet(string varName)
        {
            if (variables.ContainsKey(varName))
                return true;
            return false;
        }

        /// <summary>
        /// Swap template for another and recompile.
        /// </summary>
        /// <param name="tplFileName">Template to swap to</param>
        /// <returns></returns>
        public string SwapAndRecompile(RequestWorker worker, string tplFileName)
        {
            this.tplFileName = tplFileName;
            return Compile(worker);
        }

        /// <summary>
        /// Compile template and return as string.
        /// </summary>
        /// <param name="worker"></param>
        public string Compile(RequestWorker worker)
        {
            const string tplKeywordFormat = "<!-- (.*?) (.*?)?[ ]?-->";
            const string beginFormat = "<!-- BEGIN {0} -->";
            const string endFormat = "<!-- END {0} -->";
            const string includeFormat = "<!-- INCLUDE {0} -->";

            // load template file into uncompiled code string
            try
            {
                if (worker.embeddedMode)
                {
                    // build the embedded assembly path and test if resource exists
                    string assemblyPath = worker.webroot + ".";
                    if (tplDirectoryName != string.Empty)
                    {
                        string tempPath = tplDirectoryName.Replace('/', '.');
                        tempPath = tempPath.Replace("\\", ".");
                        assemblyPath += tempPath + ".";
                    }

                    assemblyPath += tplFileName;
                    List<string> embedTest = new List<string>(worker.executingAssembly.GetManifestResourceNames());
                    if (!embedTest.Contains(assemblyPath))
                    {
                        RPCLogger.WriteWarning("unable to locate resource [" + tplFileName + "]");
                        return null;
                    }

                    // resource should exist ... read it from the assembly
                    Stream assemblyStream = worker.executingAssembly.GetManifestResourceStream(assemblyPath);
                    if (assemblyPath != null)
                    {
                        StreamReader reader = new StreamReader(assemblyStream);
                        uncompiledCode = reader.ReadToEnd();
                        reader.Close();
                    }
                    else
                        return null;
                }
                else
                {
                    LocalFileLocator locator = new LocalFileLocator(worker.webroot + Path.DirectorySeparatorChar + tplDirectoryName);
                    if (!locator.Exists(tplFileName))
                    {
                        RPCLogger.WriteWarning("unable to locate resource [" + tplFileName + "]");
                        return null;
                    }

                    // resource should exist
                    Stream fileStream = locator.Open(tplFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                    if (fileStream != null)
                    {
                        StreamReader reader = new StreamReader(fileStream);
                        uncompiledCode = reader.ReadToEnd();
                        reader.Close();
                    }
                    else
                        return null;
                }

                //Messages.Trace("compiling template [" + tplFileName + "]", LogFilter.TEMPLATE_TRACE);

                // copy uncompiled code to compiled
                string compiledCode = uncompiledCode;

                // iterate through top-level variables and replace
                foreach (KeyValuePair<string, string> kvp in variables)
                {
                    //Messages.Trace("var [{" + kvp.Key + "}] -> [" + kvp.Value + "]", LogFilter.TEMPLATE_TRACE);
                    compiledCode = compiledCode.Replace("{" + kvp.Key + "}", kvp.Value);

                    // check if the key is a "guid" if so replace "sanitized" versions
                    if (kvp.Key.ToUpper().Contains("GUID"))
                        compiledCode = compiledCode.Replace("{SANITIZED_" + kvp.Key + "}", kvp.Value.Replace("-", string.Empty));
                }

                // see if we have any blocks
                MatchCollection matches = Regex.Matches(uncompiledCode, tplKeywordFormat);
                foreach (Match m in matches)
                {
                    // we must have at least 2 groups!
                    if (m.Groups.Count <= 1)
                        continue;

                    string keyword = m.Groups[1].Value.ToUpper();
                    string value = m.Groups[2].Value;

                    // is this a begin tag?
                    if (keyword == "BEGIN")
                    {
                        string[] arr = compiledCode.Split(new string[] { String.Format(beginFormat, value),
                            String.Format(endFormat, value) }, StringSplitOptions.RemoveEmptyEntries);
                        int arrEnd = arr.Length - 1;
                        //Messages.Trace("block [" + value + "] arrLen [" + arr.Length + "]", LogFilter.TEMPLATE_TRACE);

                        // make sure there is some length, otherwise bail
                        if (arr.Length < 1)
                            continue;

                        // iterate through array
                        for (int i = 1; i < arrEnd; i++)
                        {
                            string blockTplCode = arr[i];
                            //Messages.Trace("idx [" + i + "] block [" + value + "] TplCode [" + blockTplCode + "]", LogFilter.TEMPLATE_TRACE);

                            // compile block template
                            string compiledBlock = CompileBlock(value, blockTplCode);
                            //Messages.Trace("idx [" + i + "] block [" + value + "] compiledBlock [" + compiledBlock + "]", LogFilter.TEMPLATE_TRACE);

                            // replace original block
                            compiledCode = compiledCode.Replace(blockTplCode, compiledBlock);

                            // dump log warning if original block remains
                            if (compiledCode.Contains(blockTplCode))
                                RPCLogger.WriteWarning("failed to replace template block! page will be inconsistent!");
                        }
                    }
                    else if (keyword == "INCLUDE")
                    {
                        //Messages.Trace("include [" + value + "]", LogFilter.TEMPLATE_TRACE);

                        Template includeTemplate = new Template(value, variables, blockVariables);
                        string compiledInclude = includeTemplate.Compile(worker);

                        compiledCode = compiledCode.Replace(String.Format(includeFormat, value), compiledInclude);

                        // dump log warning if original block remains
                        if (compiledCode.Contains(String.Format(includeFormat, value)))
                            RPCLogger.WriteWarning("failed to replace template include! page will be inconsistent!");
                    }
                }

                return compiledCode;
            }
            catch (Exception e)
            {
                RPCLogger.StackTrace(e, false);
            }

            return null;
        }
    } // public class Template
} // namespace TridentFramework.RPC.Http
