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
using System.Text;
using Catalyst.Common.Interfaces.Cli.Options;
using Catalyst.Common.Interfaces.Rpc;
using Catalyst.Common.P2P;
using Catalyst.Protocol.Rpc.Node;
using Dawn;

namespace Catalyst.Cli.Commands
{
    internal sealed partial class Commands
    {
        /// <inheritdoc cref="GetInfoCommand" />
        public bool GetInfoCommand(IGetInfoOptions opts)
        {
            Guard.Argument(opts, nameof(opts)).NotNull().Compatible<IGetInfoOptions>();

            INodeRpcClient node;
            try
            {
                node = GetConnectedNode(opts.NodeId);
            }
            catch (Exception e)
            {
                _logger.Error(e.Message);
                return false;
            }

            var nodeConfig = GetNodeConfig(opts.NodeId);
            Guard.Argument(nodeConfig, nameof(nodeConfig)).NotNull("The node configuration cannot be null");

            try
            {
                var message = new GetInfoRequest
                {
                    Query = true
                };

                var request = _dtoFactory.GetDto(message,
                    _peerIdentifier,
                    PeerIdentifier.BuildPeerIdFromConfig(nodeConfig, _peerIdClientId));

                node.SendMessage(request);
            }
            catch (Exception e)
            {
                _logger.Debug(e.Message);
                return false;
            }

            return true;
        }
    }
}
