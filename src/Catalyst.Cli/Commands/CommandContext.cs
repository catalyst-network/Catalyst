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
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.Interfaces.Cli.Commands;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.IO.Transport;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.Rpc;
using Catalyst.Common.IO.Transport;
using Catalyst.Common.Network;
using Catalyst.Node.Rpc.Client;
using Dawn;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Catalyst.Cli.Commands
{
    public class CommandContext : ICommandContext
    {
        private readonly IList<IRpcNodeConfig> _rpcNodeConfigs;
        private readonly ISocketClientRegistry<INodeRpcClient> _socketClientRegistry;
        private readonly ILogger _logger;
        private readonly IUserOutput _userOutput;

        /// <summary>
        /// </summary>
        public CommandContext(IConfigurationRoot config,
            ILogger logger,
            IUserOutput userOutput,
            IPeerIdClientId peerIdClientId,
            IDtoFactory dtoFactory)
        {
            _logger = logger;
            _socketClientRegistry = new SocketClientRegistry<INodeRpcClient>();
            _rpcNodeConfigs = NodeRpcConfig.BuildRpcNodeSettingList(config);
            _userOutput = userOutput;
            DtoFactory = dtoFactory;
            PeerIdClientId = peerIdClientId;
            PeerIdentifier = Common.P2P.PeerIdentifier.BuildPeerIdFromConfig(config, peerIdClientId);
        }

        public IDtoFactory DtoFactory { get; }
        public IPeerIdentifier PeerIdentifier { get; }

        public IPeerIdClientId PeerIdClientId { get; }

        /// <inheritdoc cref="GetConnectedNode" />
        public INodeRpcClient GetConnectedNode(string nodeId)
        {
            Guard.Argument(nodeId, nameof(nodeId)).NotNull().NotEmpty().Compatible<string>();
            var nodeConfig = _rpcNodeConfigs.SingleOrDefault(node => node.NodeId.Equals(nodeId));
            Guard.Argument(nodeConfig, nameof(nodeConfig)).NotNull();

            var registryId = _socketClientRegistry.GenerateClientHashCode(
                EndpointBuilder.BuildNewEndPoint(nodeConfig.HostAddress, nodeConfig.Port));

            var nodeRpcClient = _socketClientRegistry.GetClientFromRegistry(registryId);
            Guard.Argument(nodeRpcClient).Require(IsSocketChannelActive(nodeRpcClient));

            return nodeRpcClient;
        }

        /// <inheritdoc cref="GetNodeConfig" />
        public IRpcNodeConfig GetNodeConfig(string nodeId)
        {
            Guard.Argument(nodeId, nameof(nodeId)).NotNull().NotEmpty().Compatible<string>();

            var nodeConfig = _rpcNodeConfigs.SingleOrDefault(config => config.NodeId.Equals(nodeId));

            if (nodeConfig != null)
            {
                return nodeConfig;
            }

            _userOutput.WriteLine("Node not configured. Add node to config file and try again.");

            return null;
        }

        /// <summary>
        /// Checks if the socket channel opened with the RPC server in the node is still active.
        /// </summary>
        /// <param name="node">A <see cref="IRpcNode"/> object including node required information.</param>
        /// <returns>Returns True if the channel is still active and False otherwise.  A "Channel inactive ..." message is returned to the console.</returns>
        public bool IsSocketChannelActive(INodeRpcClient node)
        {
            Guard.Argument(node, nameof(node)).Compatible<INodeRpcClient>();
            try
            {
                Guard.Argument(node.Channel.Active, nameof(node.Channel.Active)).True();
                return true;
            }
            catch (Exception e)
            {
                _logger.Information("Channel inactive ...");
                _logger.Debug(e.Message);
                return false;
            }
        }
    }
}
