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
using System.Net;
using Catalyst.Abstractions.Cli;
using Catalyst.Abstractions.Cli.Commands;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.IO.Transport;
using Catalyst.Abstractions.Rpc;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.Network;
using Catalyst.Core.Lib.Util;
using Catalyst.Core.Modules.Rpc.Client;
using Catalyst.Protocol.Peer;
using Dawn;
using Microsoft.Extensions.Configuration;
using MultiFormats;
using Serilog;

namespace Catalyst.Cli.Commands
{
    public class CommandContext : ICommandContext
    {
        private readonly IList<IRpcClientConfig> _rpcNodeConfigs;
        private readonly ILogger _logger;

        /// <summary>
        /// </summary>
        public CommandContext(IConfigurationRoot config,
            ILogger logger,
            IUserOutput userOutput,
            IRpcClientFactory rpcClientFactory,
            ICertificateStore certificateStore,
            ISocketClientRegistry<IRpcClient> socketClientRegistry)
        {
            _logger = logger;
            _rpcNodeConfigs = RpcClientSettings.BuildRpcNodeSettingList(config);

            SocketClientRegistry = socketClientRegistry;
            PeerId = GetPeerIdentifierFromCliConfig(config);
            RpcClientFactory = rpcClientFactory;
            CertificateStore = certificateStore;
            UserOutput = userOutput;
        }

        public MultiAddress PeerId { get; }

        public IRpcClientFactory RpcClientFactory { get; }

        public ICertificateStore CertificateStore { get; }

        public IUserOutput UserOutput { get; }

        public ISocketClientRegistry<IRpcClient> SocketClientRegistry { get; }

        /// <inheritdoc cref="GetConnectedNode" />
        public IRpcClient GetConnectedNode(string nodeId)
        {
            Guard.Argument(nodeId, nameof(nodeId)).NotNull().NotEmpty().Compatible<string>();
            var nodeConfig = _rpcNodeConfigs.SingleOrDefault(node => node.NodeId.Equals(nodeId));
            Guard.Argument(nodeConfig, nameof(nodeConfig)).NotNull();

            var registryId = SocketClientRegistry.GenerateClientHashCode(nodeConfig.PeerId.GetIPEndPoint());

            var nodeRpcClient = SocketClientRegistry.GetClientFromRegistry(registryId);
            Guard.Argument(nodeRpcClient).Require(IsSocketChannelActive(nodeRpcClient));

            return nodeRpcClient;
        }

        /// <inheritdoc cref="GetNodeConfig" />
        public IRpcClientConfig GetNodeConfig(string nodeId)
        {
            Guard.Argument(nodeId, nameof(nodeId)).NotNull().NotEmpty().Compatible<string>();

            var nodeConfig = _rpcNodeConfigs.SingleOrDefault(config => config.NodeId.Equals(nodeId));

            if (nodeConfig != null) return nodeConfig;

            UserOutput.WriteLine("Node not configured. Add node to config file and try again.");

            return null;
        }

        public bool IsSocketChannelActive(IRpcClient node)
        {
            Guard.Argument(node, nameof(node)).Compatible<IRpcClient>();
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

        private MultiAddress GetPeerIdentifierFromCliConfig(IConfigurationRoot configRoot)
        {
            var cliSettings = configRoot.GetSection("CatalystCliConfig");
            var address = cliSettings.GetSection("Address").Value;
            return new MultiAddress(address);
        }
    }
}
