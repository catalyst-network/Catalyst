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
using Catalyst.Abstractions.Config;
using Catalyst.Abstractions.Enumerator;
using Google.Protobuf;

namespace Catalyst.Abstractions.Types
{
    public sealed class PeerMessageTypes
        : Enumeration,
            IEnumerableMessageType
    {
        /// <summary>The message map</summary>
        private static readonly Dictionary<string, PeerMessageTypes> MessageMap;

        /// <summary>The message namespace</summary>
        private const string MessageNamespace = "Catalyst.Protocol.IPPN";

        /// <summary>Initializes the <see cref="PeerMessageTypes"/> class.</summary>
        static PeerMessageTypes()
        {
            MessageMap = new Dictionary<string, PeerMessageTypes>();
            var types = AppDomain.CurrentDomain.GetAssemblies()
               .SelectMany(t => t.GetTypes())
               .Where(t => t.IsClass && t.Namespace == MessageNamespace
                 && typeof(IMessage).IsAssignableFrom(t));

            var id = 0;
            foreach (var type in types)
            {
                MessageMap.Add(type.Name, new PeerMessageTypes(id, type.Name));
                id += 1;
            }
        }

        /// <summary>Gets the messages.</summary>
        /// <value>The messages.</value>
        public IEnumerable<PeerMessageTypes> Messages => MessageMap.Values.AsEnumerable();

        /// <summary>Initializes a new instance of the <see cref="PeerMessageTypes"/> class.</summary>
        /// <param name="id">The identifier.</param>
        /// <param name="name">The name.</param>
        private PeerMessageTypes(int id, string name) : base(id, name) { }
    }
}
