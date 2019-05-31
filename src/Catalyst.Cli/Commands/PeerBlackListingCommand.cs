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
using System.Text;
using Catalyst.Common.Config;
using Catalyst.Common.Interfaces.Cli.Options;
using Catalyst.Common.Interfaces.Rpc;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.P2P;
using Catalyst.Common.Rpc;
using Catalyst.Common.Util;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using Nethereum.RLP;

namespace Catalyst.Cli.Commands
{
    internal partial class Commands
    {
        /// <inheritdoc cref="PeerBlackListingCommand" />
        public bool PeerBlackListingCommand(IPeerBlackListingOptions opts)
        {
            Guard.Argument(opts).NotNull().Compatible<IPeerBlackListingOptions>();
            try
            {
                INodeRpcClient node;
                try
                {
                    node = GetConnectedNode(opts.Node);
                }
                catch (Exception e)
                {
                    _logger.Error(e.Message, "Failed to get connected node {0} while trying to set the peer blacklist flag", opts.Node);
                    return false;
                }

                var nodeConfig = GetNodeConfig(opts.Node);
                if (nodeConfig == null)
                {
                    throw new KeyNotFoundException($"Unable to find configuration for node {opts.Node}");
                }
                
                var peerPublicKey = opts.PublicKey;
                var peerIp = opts.IpAddress;

                var blackListFlag = opts.BlackListFlag;

                var requestMessage = _rpcMessageFactory.GetMessage(new MessageDto(
                    new SetPeerBlackListRequest
                    {
                        PublicKey = peerPublicKey.ToBytesForRLPEncoding().ToByteString(),
                        Ip = peerIp.ToBytesForRLPEncoding().ToByteString(),
                        Blacklist = blackListFlag
                    },
                    MessageTypes.Ask,
                    new PeerIdentifier(Encoding.ASCII.GetBytes(nodeConfig.PublicKey), nodeConfig.HostAddress,
                        nodeConfig.Port),
                    _peerIdentifier
                ));

                node.SendMessage(requestMessage).Wait();
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
