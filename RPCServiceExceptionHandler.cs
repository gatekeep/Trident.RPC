/*
 * Copyright (c) 2008-2020 Bryan Biedenkapp., All Rights Reserved.
 * MIT Open Source. Use is subject to license terms.
 * DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
 */

using System;

using Newtonsoft.Json.Linq;

using TridentFramework.RPC.Utility;

namespace TridentFramework.RPC
{
    /// <summary>
    /// This class implements our custom message inspector that is used for handling authorization.
    /// </summary>
    public class RPCServiceExceptionHandler : IRPCExceptionHandler
    {
        /*
        ** Methods
        */

        /// <inheritdoc />
        public bool HandleError(Exception ex)
        {
            return false;
        }

        /// <inheritdoc />
        public void ProvideFault(Exception ex, ref JObject fault)
        {
            RPCLogger.StackTrace(ex, false);
            fault = null; // returns default fault
        }
    } // public class RPCServiceExceptionHandler : IRPCExceptionHandler
} // namespace TridentFramework.RPC
