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

using System.Net;
using System.Text;
using Catalyst.Common.Enums.Messages;
using Catalyst.Common.Interfaces.Cli.Options;
using Catalyst.Common.Network;
using Catalyst.Common.P2P;
using Catalyst.Node.Core.Rpc.Messaging;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using Google.Protobuf;
using Nethereum.RLP;

namespace Catalyst.Cli.Commands
{
    public partial class Commands
    {
        /// <inheritdoc cref="PeerRemoveCommand" />
        public bool PeerRemoveCommand(IRemovePeerOptions opts)
        {
            Guard.Argument(opts).NotNull().Compatible<IRemovePeerOptions>();

            var node = GetConnectedNode(opts.Node);
            var nodeConfig = GetNodeConfig(opts.Node);

            Guard.Argument(node).NotNull();

            var rpcMessageFactory = new RpcMessageFactory<RemovePeerRequest>();

            var ip = IPAddress.Parse(opts.Ip);

            var request = new RemovePeerRequest
            {
                PeerIp = ByteString.CopyFrom(ip.To16Bytes()),
                PublicKey = string.IsNullOrEmpty(opts.PublicKey)
                    ? ByteString.Empty
                    : ByteString.CopyFrom(opts.PublicKey.ToBytesForRLPEncoding())
            };

            var requestMessage = rpcMessageFactory.GetMessage(
                message: request,
                recipient: new PeerIdentifier(Encoding.ASCII.GetBytes(nodeConfig.PublicKey), nodeConfig.HostAddress,
                    nodeConfig.Port),
                sender: _peerIdentifier,
                messageType: DtoMessageType.Ask
            );

            node.SendMessage(requestMessage).Wait();

            return true;
        }
    }
}
