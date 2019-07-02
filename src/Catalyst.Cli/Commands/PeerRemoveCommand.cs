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
using System.Net;
using System.Text;
using Catalyst.Common.Interfaces.Cli.Options;
using Catalyst.Common.Interfaces.Rpc;
using Catalyst.Common.Network;
using Catalyst.Common.P2P;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using Google.Protobuf;
using Nethereum.RLP;

namespace Catalyst.Cli.Commands
{
    internal partial class Commands
    {
        /// <inheritdoc cref="PeerRemoveCommand" />
        public bool PeerRemoveCommand(IRemovePeerOptions opts)
        {
            Guard.Argument(opts).NotNull().Compatible<IRemovePeerOptions>();

            INodeRpcClient node;
            try
            {
                node = GetConnectedNode(opts.Node);
            }
            catch (Exception e)
            {
                _logger.Error(e.Message);
                return false;
            }
            
            var nodeConfig = GetNodeConfig(opts.Node);
            Guard.Argument(nodeConfig, nameof(nodeConfig)).NotNull("The node configuration cannot be null");

            var requestMessage = _dtoFactory.GetDto(new RemovePeerRequest
                {
                    PeerIp = ByteString.CopyFrom(IPAddress.Parse(opts.Ip).To16Bytes()),
                    PublicKey = string.IsNullOrEmpty(opts.PublicKey)
                        ? ByteString.Empty
                        : ByteString.CopyFrom(opts.PublicKey.ToBytesForRLPEncoding())
                },
                _peerIdentifier,
                PeerIdentifier.BuildPeerIdFromConfig(nodeConfig, _peerIdClientId));

            node.SendMessage(requestMessage);

            return true;
        }
    }
}
