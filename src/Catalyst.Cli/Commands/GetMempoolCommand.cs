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
using Catalyst.Common.Config;
using Catalyst.Common.Interfaces.Cli.Options;
using Catalyst.Common.Interfaces.Rpc;
using Catalyst.Common.P2P;
using Catalyst.Node.Core.Rpc.Messaging;
using Catalyst.Protocol.Rpc.Node;
using Dawn;

namespace Catalyst.Cli.Commands
{
    public partial class Commands
    {
        /// <inheritdoc cref="GetMempoolCommand" />
        public bool GetMempoolCommand(IGetMempoolOptions opts)
        {
            Guard.Argument(opts, nameof(opts)).NotNull().Compatible<IGetMempoolOptions>();

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
                var request = new RpcMessageFactory<GetMempoolRequest>().GetMessage(
                    new GetMempoolRequest(),
                    new PeerIdentifier(Encoding.ASCII.GetBytes(nodeConfig.PublicKey),
                        nodeConfig.HostAddress,
                        nodeConfig.Port),
                    _peerIdentifier,
                    MessageTypes.Ask
                );
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
