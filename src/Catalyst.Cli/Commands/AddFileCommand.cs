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
using Catalyst.Common.FileTransfer;
using Catalyst.Common.Interfaces.Cli.Options;
using Catalyst.Common.Interfaces.FileTransfer;
using Catalyst.Common.Interfaces.Rpc;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.P2P;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using Serilog.Events;

namespace Catalyst.Cli.Commands
{
    internal sealed partial class Commands
    {
        /// <inheritdoc cref="AddFile" />
        public bool AddFile(IAddFileOnDfsOptions opts)
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
            
            var requestMessage = _messageFactory.GetMessage(new MessageDto(
                message: request,
                messageTypes: MessageTypes.Ask,
                recipient: nodePeerIdentifier,
                sender: _peerIdentifier
            ));

            IUploadFileInformation fileTransfer = new UploadFileTransferInformation(
                File.Open(opts.File, FileMode.Open),
                _peerIdentifier,
                nodePeerIdentifier,
                node,
                requestMessage.CorrelationId.ToGuid(),
                _messageFactory);

            _uploadFileTransferFactory.RegisterTransfer(fileTransfer);

            node.SendMessage(requestMessage);

            var originalLogLevel = Program.LogLevelSwitch.MinimumLevel;
            
            Program.LogLevelSwitch.MinimumLevel = LogEventLevel.Error;

            while (!fileTransfer.ChunkIndicatorsTrue() && !fileTransfer.IsExpired())
            {
                _userOutput.Write($"\rUploaded: {fileTransfer.GetPercentage().ToString()}%");
                System.Threading.Thread.Sleep(500);
            }

            if (fileTransfer.ChunkIndicatorsTrue())
            {
                _userOutput.Write($"\rUploaded: {fileTransfer.GetPercentage().ToString()}%\n");
            }
            else
            {
                _userOutput.WriteLine("\nFile transfer expired.");
            }

            Program.LogLevelSwitch.MinimumLevel = originalLogLevel;
            return true;
        }
    }
}
