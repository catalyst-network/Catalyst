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
using Catalyst.Common.FileTransfer;
using Catalyst.Common.Interfaces.Cli.Options;
using Catalyst.Common.Interfaces.FileTransfer;
using Catalyst.Common.Interfaces.Rpc;
using Catalyst.Common.P2P;
using Catalyst.Protocol.Rpc.Node;
using Dawn;

namespace Catalyst.Cli.Commands
{
    internal sealed partial class Commands
    {
        /// <summary>Called when [get file options].</summary>
        /// <param name="opts">The opts.</param>
        /// <returns></returns>
        private bool GetFileOptions(IGetFileOptions opts)
        {
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

            var message = new GetFileFromDfsRequest
            {
                DfsHash = opts.FileHash
            };

            var recipient = PeerIdentifier.BuildPeerIdFromConfig(nodeConfig, _peerIdClientId);
            
            var messageDto = _dtoFactory.GetDto(message, 
                recipient,
                _peerIdentifier);

            IDownloadFileInformation fileTransfer = new DownloadFileTransferInformation(
                _peerIdentifier,
                messageDto.Sender,
                node.Channel,
                messageDto.CorrelationId,
                opts.FileOutput,
                0
            );

            _downloadFileTransferFactory.RegisterTransfer(fileTransfer);

            node.SendMessage(messageDto);

            while (!fileTransfer.ChunkIndicatorsTrue() && !fileTransfer.IsExpired())
            {
                _userOutput.Write($"\rDownloaded: {fileTransfer.GetPercentage().ToString()}%");
                System.Threading.Thread.Sleep(500);
            }

            if (fileTransfer.ChunkIndicatorsTrue())
            {
                _userOutput.Write($"\rDownloaded: {fileTransfer.GetPercentage().ToString()}%\n");
            }
            else
            {
                _userOutput.WriteLine("\nFile transfer expired.");
            }

            return true;
        }
    }
}
