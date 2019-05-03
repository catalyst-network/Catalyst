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
    public class P2PMessages
        : Enumeration,
            IEnumerableMessageType
    {
        /// <summary>The message map</summary>
        private static Dictionary<string, P2PMessages> _messageMap;

        /// <summary>The message namespace</summary>
        private static readonly string _messageNamespace = "Catalyst.Protocol.IPPN";

        /// <summary>Initializes the <see cref="P2PMessages"/> class.</summary>
        static P2PMessages()
        {
            _messageMap = new Dictionary<string, P2PMessages>();
            var types = AppDomain.CurrentDomain.GetAssemblies()
               .SelectMany(t => t.GetTypes())
               .Where(t => t.IsClass && t.Namespace == _messageNamespace
                 && typeof(IMessage).IsAssignableFrom(t));

            int id = 0;
            foreach (Type type in types)
            {
                _messageMap.Add(type.Name, new P2PMessages(id, type.Name));
                id += 1;
            }
        }

        /// <summary>Gets the messages.</summary>
        /// <value>The messages.</value>
        public IEnumerable<P2PMessages> Messages => _messageMap.Values.AsEnumerable();

        /// <summary>Initializes a new instance of the <see cref="P2PMessages"/> class.</summary>
        /// <param name="id">The identifier.</param>
        /// <param name="name">The name.</param>
        protected P2PMessages(int id, string name) : base(id, name) { }
    }
}
