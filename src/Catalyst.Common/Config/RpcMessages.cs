#region LICENSE

/**
* Copyright (c) 2019 Catalyst Network
*
* This file is part of Catalyst.Node <https://github.com/catalyst-network/Catalyst.Node>
*
* Catalyst.Node is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 2 of the License, or
* (at your option) any later version.
*
* Catalyst.Node is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with Catalyst.Node. If not, see <https://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using Catalyst.Common.Enumerator;
using Catalyst.Common.Interfaces.IO.Messaging;
using Google.Protobuf;

namespace Catalyst.Common.Config
{
    public class RpcMessages
        : Enumeration,
            IEnumerableMessageType
    {
        /// <summary>The message map</summary>
        private static Dictionary<string, RpcMessages> _messageMap;

        /// <summary>The RPC message namespace</summary>
        private static readonly string _messageNamespace = "Catalyst.Protocol.Rpc.Node";

        /// <summary>Initializes the <see cref="RpcMessages"/> class.</summary>
        static RpcMessages()
        {
            _messageMap = new Dictionary<string, RpcMessages>();
            var types = AppDomain.CurrentDomain.GetAssemblies()
               .SelectMany(t => t.GetTypes())
               .Where(t => t.IsClass && t.Namespace == _messageNamespace 
                 && typeof(IMessage).IsAssignableFrom(t));

            int id = 0;
            foreach (Type type in types)
            {
                _messageMap.Add(type.Name, new RpcMessages(id, type.Name));
                id += 1;
            }
        }

        public static IEnumerable<RpcMessages> Messages => _messageMap.Values.AsEnumerable();

        /// <summary>Initializes a new instance of the <see cref="RpcMessages"/> class.</summary>
        /// <param name="id">The identifier.</param>
        /// <param name="name">The name.</param>
        protected RpcMessages(int id, string name) : base(id, name) { }
    }
}
