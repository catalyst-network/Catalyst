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

using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.Interfaces.Cli.Commands;
using Catalyst.Common.Shell;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Catalyst.Cli.Commands
{
    /// <inheritdoc cref="ShellBase" />
    public sealed class Commands : ShellBase
    {
        private readonly IEnumerable<ICommand> _commands;

        /// <summary>
        /// </summary>
        public Commands(IUserOutput userOutput, IEnumerable<ICommand> commands) : base(userOutput)
        {
            _commands = commands;
            userOutput.WriteLine(@"Koopa Shell Start");
        }

        /// <inheritdoc cref="ParseCommand" />
        public override bool ParseCommand(params string[] args)
        {
            var parsedCommand = _commands.FirstOrDefault(command => command.CommandName.Equals(args[0], StringComparison.InvariantCultureIgnoreCase));
            var commandExists = parsedCommand != null;
            parsedCommand?.Parse(args);
            return commandExists;
        }

        /// <summary>
        /// Connects a valid and configured node to the RPC server.
        /// </summary>
        /// <param name="nodeId">a string including the node ID.</param>
        /// <returns>Returns true unless an unhandled exception occurs.</returns>
        private bool OnConnectNode(string nodeId)
        {
            /*Guard.Argument(nodeId, nameof(nodeId)).NotEmpty();
            var rpcNodeConfigs = GetNodeConfig(nodeId);
            Guard.Argument(rpcNodeConfigs, nameof(rpcNodeConfigs)).NotNull();

            try
            {
                //Connect to the node and store it in the socket client registry
                var nodeRpcClient = _nodeRpcClientFactory.GetClient(_certificateStore.ReadOrCreateCertificateFile(rpcNodeConfigs.PfxFileName), rpcNodeConfigs);

                if (!IsSocketChannelActive(nodeRpcClient))
                {
                    return false;
                }

                var clientHashCode = _socketClientRegistry.GenerateClientHashCode(
                    EndpointBuilder.BuildNewEndPoint(rpcNodeConfigs.HostAddress, rpcNodeConfigs.Port));

                _socketClientRegistry.AddClientToRegistry(clientHashCode, nodeRpcClient);
            }
            catch (Exception e)
            {
                _logger.Debug(e.Message, e);
                return false;
            }*/

            return true;
        }

        /// <inheritdoc cref="DisconnectNode" />
        private bool DisconnectNode(string nodeId)
        {
            /*Guard.Argument(nodeId, nameof(nodeId)).Contains(typeof(string));
            var nodeConfig = GetNodeConfig(nodeId);
            Guard.Argument(nodeConfig, nameof(nodeConfig)).NotNull();

            var registryId = _socketClientRegistry.GenerateClientHashCode(
                EndpointBuilder.BuildNewEndPoint(nodeConfig.HostAddress, nodeConfig.Port));

            var node = _socketClientRegistry.GetClientFromRegistry(registryId);
            Guard.Argument(node, nameof(node)).Require(IsSocketChannelActive(node));

            node.Dispose();
            _socketClientRegistry.RemoveClientFromRegistry(registryId);*/

            return true;
        }
    }
}
