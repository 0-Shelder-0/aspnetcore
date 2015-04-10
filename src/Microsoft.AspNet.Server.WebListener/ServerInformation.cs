// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System.Reflection;
using Microsoft.AspNet.Hosting.Server;

namespace Microsoft.AspNet.Server.WebListener
{
    public class ServerInformation : IServerInformation
    {
        private MessagePump _messagePump;

        internal ServerInformation(MessagePump messagePump)
        {
            _messagePump = messagePump;
        }

        internal MessagePump MessagePump
        {
            get { return _messagePump; }
        }

        // Microsoft.AspNet.Server.WebListener
        public string Name
        {
            get { return GetType().GetTypeInfo().Assembly.GetName().Name; }
        }

        public Microsoft.Net.Http.Server.WebListener Listener
        {
            get { return _messagePump.Listener; }
        }

        public int MaxAccepts
        {
            get { return _messagePump.MaxAccepts; }
            set { _messagePump.MaxAccepts = value; }
        }
    }
}
