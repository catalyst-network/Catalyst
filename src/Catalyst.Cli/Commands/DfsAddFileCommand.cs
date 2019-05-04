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

using System.IO;
using System.Text;
using Catalyst.Cli.Options;
using Catalyst.Common.Enums.Messages;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.Cli.Options;
using Catalyst.Common.P2P;
using Catalyst.Node.Core.Rpc.Messaging;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using Serilog.Events;

namespace Catalyst.Cli.Commands
{
    public partial class Commands
    {
        /// <inheritdoc cref="DfsAddFile" />
        public bool DfsAddFile(IAddFileOnDfsOptions opts)
        {
            Guard.Argument(opts).NotNull().Compatible<IAddFileOnDfsOptions>();

            var addFileOnDfsOptions = (AddFileOnDfsOptions) opts;
            var node = GetConnectedNode(addFileOnDfsOptions.Node);
            var nodeConfig = GetNodeConfig(addFileOnDfsOptions.Node);
            var nodePeerIdentifier = new PeerIdentifier(Encoding.ASCII.GetBytes(nodeConfig.PublicKey),
                nodeConfig.HostAddress, nodeConfig.Port);

            Guard.Argument(node).NotNull();

            if (!File.Exists(addFileOnDfsOptions.File))
            {
                _userOutput.WriteLine("File does not exist.");
                return false;
            }

            var request = new AddFileToDfsRequest
            {
                FileName = Path.GetFileName(addFileOnDfsOptions.File)
            };

            using (var fileStream = File.Open(addFileOnDfsOptions.File, FileMode.Open))
            {
                request.FileSize = (ulong) fileStream.Length;
            }

            var rpcMessageFactory = new RpcMessageFactory<AddFileToDfsRequest>();

            var requestMessage = rpcMessageFactory.GetMessage(
                message: request,
                recipient: nodePeerIdentifier,
                sender: _peerIdentifier,
                messageType: DtoMessageType.Ask
            );

            node.SendMessage(requestMessage);

            var responseReceived = _cliFileTransfer.Wait();

            if (!responseReceived)
            {
                _userOutput.WriteLine("Timeout - No response received from node");
                return false;
            }

            if (!_cliFileTransfer.InitialiseSuccess())
            {
                return true;
            }

            var minLevel = Program.LogLevelSwitch.MinimumLevel;
            Program.LogLevelSwitch.MinimumLevel = LogEventLevel.Error;
            _cliFileTransfer.TransferFile(addFileOnDfsOptions.File, requestMessage.CorrelationId.ToGuid(), node,
                nodePeerIdentifier, _peerIdentifier);
            Program.LogLevelSwitch.MinimumLevel = minLevel;
            _cliFileTransfer.WaitForDfsHash();

            return true;
        }
    }
}
