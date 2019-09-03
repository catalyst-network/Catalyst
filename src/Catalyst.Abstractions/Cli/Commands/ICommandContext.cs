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

using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.IO.Transport;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.Rpc;

namespace Catalyst.Abstractions.Cli.Commands
{
    public interface ICommandContext
    {
        /// <summary>Gets the node RPC client factory.</summary>
        /// <value>The node RPC client factory.</value>
        INodeRpcClientFactory NodeRpcClientFactory { get; }

        /// <summary>Gets the certificate store.</summary>
        /// <value>The certificate store.</value>
        ICertificateStore CertificateStore { get; }

        /// <summary>Gets the user output.</summary>
        /// <value>The user output.</value>
        IUserOutput UserOutput { get; }

        /// <summary>Gets the socket client registry.</summary>
        /// <value>The socket client registry.</value>
        ISocketClientRegistry<INodeRpcClient> SocketClientRegistry { get; }

        /// <summary>Gets the peer identifier.</summary>
        /// <value>The peer identifier.</value>
        IPeerIdentifier PeerIdentifier { get; }

        /// <summary>Gets the connected node.</summary>
        /// <param name="nodeId">The node identifier located in configuration.</param>
        /// <returns></returns>
        INodeRpcClient GetConnectedNode(string nodeId);

        /// <summary>Gets the node configuration.</summary>
        /// <param name="nodeId">The node identifier located in configuration.</param>
        /// <returns></returns>
        IRpcNodeConfig GetNodeConfig(string nodeId);

        /// <summary>Determines whether [is socket channel active] [the specified node].</summary>
        /// <param name="node">A <see>
        ///         <cref>IRpcNode</cref>
        ///     </see>
        ///     object including node required information.</param>
        /// <returns><c>true</c> if [is socket channel active] [the specified node]; otherwise,
        /// <c>false</c> A "Channel inactive ..." message is returned to the console.</returns>
        bool IsSocketChannelActive(INodeRpcClient node);
    }
}
