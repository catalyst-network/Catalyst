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
using Catalyst.Common.Enums.Messages;
using Catalyst.Common.Interfaces.Cli.Options;
using Catalyst.Common.P2P;
using Catalyst.Common.Util;
using Catalyst.Node.Core.Rpc.Messaging;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using Nethereum.RLP;

namespace Catalyst.Cli.Commands
{
    public partial class Commands
    {
        /// <inheritdoc cref="MessageVerifyCommand" />
        public bool MessageVerifyCommand(IVerifyOptions opts)
        {
            Guard.Argument(opts).NotNull().Compatible<IVerifyOptions>();

            var node = GetConnectedNode(opts.Node);
            Guard.Argument(node).NotNull("The connected node cannot be null.");

            var nodeConfig = GetNodeConfig(opts.Node);

            try
            {
                var request = new RpcMessageFactory<VerifyMessageRequest>().GetMessage(
                    new VerifyMessageRequest
                    {
                        Message =
                            RLP.EncodeElement(opts.Message.Trim('\"').ToBytesForRLPEncoding()).ToByteString(),
                        PublicKey = opts.Address.ToBytesForRLPEncoding().ToByteString(),
                        Signature = opts.Signature.ToBytesForRLPEncoding().ToByteString()
                    },
                    recipient: new PeerIdentifier(Encoding.ASCII.GetBytes(nodeConfig.PublicKey), nodeConfig.HostAddress,
                        nodeConfig.Port),
                    _peerIdentifier,
                    DtoMessageType.Ask);

                node.SendMessage(request).Wait();
            }
            catch (Exception e)
            {
                _logger.Debug(e.Message);
                throw;
            }

            return true;
        }
    }
}
