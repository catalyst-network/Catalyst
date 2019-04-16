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

using Catalyst.Node.Common.Helpers.Enumerator;

namespace Catalyst.Node.Common.Helpers.Config
{
    /// <summary>
    ///     Not sure we need this
    /// </summary>
    public class IoClients : Enumeration
    {
        public static readonly IoClients NodeRpcTcpClient = new NodeRpcTcp();
        public static readonly IoClients NodePeerUdpClient = new NodePeerUdp();

        private IoClients(int id, string name) : base(id, name) { }

        private sealed class NodeRpcTcp : IoClients
        {
            public NodeRpcTcp() : base(1, "NodeRpcTcp") { }
        }

        private sealed class NodePeerUdp : IoClients
        {
            public NodePeerUdp() : base(2, "NodePeerUdp") { }
        }
    }
}
