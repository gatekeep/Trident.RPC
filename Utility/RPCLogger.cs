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
using System.Reflection;

namespace TridentFramework.RPC.Utility
{
    /// <summary>
    /// Implements the RPC logger system.
    /// </summary>
    public class RPCLogger
    {
        /*
        ** Properties
        */

        /// <summary>
        /// Gets or sets a delegate to also write logs to.
        /// </summary>
        public static Action<string> WriteLog { get; set; } = null;

        /// <summary>
        /// Gets or sets a delegate to also write logs to.
        /// </summary>
        public static Action<string> WriteErrorCB { get; set; } = null;

        /*
        ** Methods
        */

        /// <summary>
        /// Static Initializer for the see <see cref="RPCLogger"/> class.
        /// </summary>
        static RPCLogger()
        {
            // setup a dummy logger
            WriteLog = (string msg) => { return; };
            WriteErrorCB = WriteLog;
        }

        /// <summary>
        /// Returns a HTML formatted string for the given exception.
        /// </summary>
        /// <param name="throwable"></param>
        /// <returns></returns>
        public static string HtmlStackTrace(Exception throwable)
        {
            string exMessage = string.Empty;
            Exception inner = throwable.InnerException;

            exMessage += "<code>---- TRACE SNIP ----<br />";
            exMessage += throwable.Message + (inner != null ? " (Inner: " + inner.Message + ")" : "") + "<br />";
            exMessage += throwable.GetType().ToString() + "<br />";

            exMessage += "<br />" + throwable.Source + "<br />";
            foreach (string str in throwable.StackTrace.Split(new string[] { Environment.NewLine }, StringSplitOptions.None))
                exMessage += str + "<br />";
            if (inner != null)
                foreach (string str in throwable.StackTrace.Split(new string[] { Environment.NewLine }, StringSplitOptions.None))
                    exMessage += "inner trace: " + str + "<br />";
            exMessage += "---- TRACE SNIP ----</code>";

            return exMessage;
        }

        /// <summary>
        /// Writes the exception stack trace to the console/trace log
        /// </summary>
        /// <param name="throwable">Exception to obtain information from</param>
        /// <param name="reThrow"></param>
        public static void StackTrace(Exception throwable, bool reThrow = true)
        {
            StackTrace(string.Empty, throwable, reThrow);
        }

        /// <summary>
        /// Writes the exception stack trace to the console/trace log
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="throwable">Exception to obtain information from</param>
        /// <param name="reThrow"></param>
        public static void StackTrace(string msg, Exception throwable, bool reThrow = true)
        {
            MethodBase mb = new System.Diagnostics.StackTrace().GetFrame(1).GetMethod();
            ParameterInfo[] param = mb.GetParameters();
            string funcParams = string.Empty;
            for (int i = 0; i < param.Length; i++)
                if (i < param.Length - 1)
                    funcParams += param[i].ParameterType.Name + ", ";
                else
                    funcParams += param[i].ParameterType.Name;

            Exception inner = throwable.InnerException;

            WriteError("caught an unrecoverable exception! " + msg);
            WriteErrorCB("---- TRACE SNIP ----");
            WriteErrorCB(throwable.Message + (inner != null ? " (Inner: " + inner.Message + ")" : ""));
            WriteErrorCB(throwable.GetType().ToString());

            WriteErrorCB("<" + mb.ReflectedType.Name + "::" + mb.Name + "(" + funcParams + ")>");
            WriteErrorCB(throwable.Source);
            foreach (string str in throwable.StackTrace.Split(new string[] { Environment.NewLine }, StringSplitOptions.None))
                WriteErrorCB(str);
            if (inner != null)
                foreach (string str in throwable.StackTrace.Split(new string[] { Environment.NewLine }, StringSplitOptions.None))
                    WriteErrorCB("inner trace: " + str);
            WriteErrorCB("---- TRACE SNIP ----");

            if (reThrow)
                throw throwable;
        }

        /// <summary>
        /// Writes a error trace message w/ calling function information.
        /// </summary>
        /// <param name='message'>Message to print</param>
        public static void WriteWarning(string message)
        {
            WriteErrorCB("WARN: " + message);
        }

        /// <summary>
        /// Writes a error trace message w/ calling function information.
        /// </summary>
        /// <param name='message'>Message to print</param>
        public static void WriteError(string message)
        {
            WriteErrorCB("ERROR: " + message);
        }

        /// <summary>
        /// Writes a trace message w/ calling function information.
        /// </summary>
        /// <param name="message">Message to print to debug window</param>
        /// <param name="frame"></param>
        /// <param name="dropToTrace"></param>
        /// <param name="dropToConsole"></param>
        public static void Trace(string message, int frame = 1, bool dropToTrace = false, bool dropToConsole = false)
        {
            string trace = string.Empty;

            MethodBase mb = new System.Diagnostics.StackTrace().GetFrame(frame).GetMethod();
            ParameterInfo[] param = mb.GetParameters();
            string funcParams = string.Empty;
            for (int i = 0; i < param.Length; i++)
                if (i < param.Length - 1)
                    funcParams += param[i].ParameterType.Name + ", ";
                else
                    funcParams += param[i].ParameterType.Name;

            trace += "<" + mb.ReflectedType.Name + "::" + mb.Name + "(" + funcParams + ")> ";
            trace += message;

            WriteLog(trace);
            if (dropToTrace)
                System.Diagnostics.Trace.WriteLine(trace);
            if (dropToConsole)
                System.Console.WriteLine(trace);
        }

        /// <summary>
        /// Helper to display the ASCII representation of a hex dump.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        private static string DisplayHexChars(byte[] buffer, int offset)
        {
            int bCount = 0;

            string _out = string.Empty;
            for (int i = offset; i < buffer.Length; i++)
            {
                // stop every 16 bytes...
                if (bCount == 16)
                    break;

                byte b = buffer[i];
                char c = Convert.ToChar(b);

                // make control and illegal characters spaces
                if (c >= 0x00 && c <= 0x1F)
                    c = ' ';
                if (c >= 0x7F)
                    c = ' ';

                _out += c;

                bCount++;
            }

            return _out;
        }

        /// <summary>
        /// Perform a hex dump of a buffer.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="buffer"></param>
        /// <param name="maxLength"></param>
        /// <param name="startOffset"></param>
        /// <param name="dropToTrace"></param>
        /// <param name="dropToConsole"></param>
        public static void TraceHex(string message, byte[] buffer, int maxLength = 32,
            int startOffset = 0, bool dropToTrace = false, bool dropToConsole = false)
        {
            int bCount = 0, j = 0, lenCount = 0;

            // iterate through buffer printing all the stored bytes
            string traceMsg = message + "\nDUMP " + j.ToString("X4") + ": ";
            for (int i = startOffset; i < buffer.Length; i++)
            {
                byte b = buffer[i];

                // split the message every 16 bytes...
                if (bCount == 16)
                {
                    traceMsg += "\t*" + DisplayHexChars(buffer, j) + "*";
                    Trace(traceMsg, 2, false, false);
                    if (dropToTrace)
                        System.Diagnostics.Trace.WriteLine(traceMsg);
                    if (dropToConsole)
                        System.Console.WriteLine(traceMsg);

                    bCount = 0;
                    j += 16;
                    traceMsg = "DUMP " + j.ToString("X4") + ": ";
                }
                else
                    traceMsg += (bCount > 0) ? " " : "";

                traceMsg += b.ToString("X2");

                bCount++;

                // increment the length counter, and check if we've exceeded the specified
                // maximum, then break the loop
                lenCount++;
                if (lenCount > maxLength)
                    break;
            }

            // if the byte count at this point is non-zero print the message
            if (bCount != 0)
            {
                traceMsg += "\t*" + DisplayHexChars(buffer, j) + "*";
                Trace(traceMsg, 2, false, false);
                if (dropToTrace)
                    System.Diagnostics.Trace.WriteLine(traceMsg);
                if (dropToConsole)
                    System.Console.WriteLine(traceMsg);
            }
        }
    } // public class Messages
} // namespace TridentFramework.RPC.Utility
