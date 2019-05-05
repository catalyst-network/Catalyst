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
using System.IO;
using System.Text;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.Cli.Options;
using Catalyst.Common.Interfaces.Rpc;
using Catalyst.Common.P2P;
using Catalyst.Node.Core.Rpc.Messaging;
using Catalyst.Protocol.Rpc.Node;
using Dawn;

namespace Catalyst.Cli.Commands
{
    public sealed partial class Commands
    {
        /// <inheritdoc cref="DfsAddFile" />
        public bool DfsAddFile(IAddFileOnDfsOptions opts)
        {
            Guard.Argument(opts, nameof(opts)).NotNull().Compatible<IAddFileOnDfsOptions>();
            
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
            Guard.Argument(nodeConfig, nameof(nodeConfig)).NotNull();

            var nodePeerIdentifier = new PeerIdentifier(Encoding.ASCII.GetBytes(nodeConfig.PublicKey),
                nodeConfig.HostAddress, nodeConfig.Port);

            if (!File.Exists(opts.File))
            {
                UserOutput.WriteLine("File does not exist.");
                return false;
            }

            var request = new AddFileToDfsRequest
            {
                FileName = Path.GetFileName(opts.File)
            };

            using (var fileStream = File.Open(opts.File, FileMode.Open))
            {
                request.FileSize = (ulong) fileStream.Length;
            }

            var requestMessage = new RpcMessageFactory<AddFileToDfsRequest>().GetMessage(
                message: request,
                recipient: nodePeerIdentifier,
                sender: _peerIdentifier,
                messageType: MessageTypes.Ask
            );

            node.SendMessage(requestMessage);

            var responseReceived = _rpcFileTransfer.Wait();

            if (!responseReceived)
            {
                UserOutput.WriteLine("Timeout - No response received from node");
                return false;
            }

            if (!_rpcFileTransfer.InitialiseSuccess())
            {
                return false;
            }
            
            _rpcFileTransfer.TransferFile(opts.File, requestMessage.CorrelationId.ToGuid(), node,
                nodePeerIdentifier, _peerIdentifier);

            _rpcFileTransfer.WaitForDfsHash();

            return true;
        }
    }
}
