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

using System.Threading;
using Catalyst.Abstractions.Cli.Commands;
using Catalyst.Abstractions.FileTransfer;
using Catalyst.Cli.CommandTypes;
using Catalyst.Cli.Options;
using Catalyst.Core.Extensions;
using Catalyst.Core.FileTransfer;
using Catalyst.Core.IO.Messaging.Dto;
using Catalyst.Protocol.Rpc.Node;

namespace Catalyst.Cli.Commands
{
    public sealed class
        GetFileCommand : BaseMessageCommand<GetFileFromDfsRequest, GetFileFromDfsResponse, GetFileOptions>
    {
        private readonly IDownloadFileTransferFactory _downloadFileTransferFactory;

        public GetFileCommand(IDownloadFileTransferFactory downloadFileTransferFactory,
            ICommandContext commandContext) : base(commandContext)
        {
            _downloadFileTransferFactory = downloadFileTransferFactory;
        }

        protected override GetFileFromDfsRequest GetMessage(GetFileOptions option)
        {
            return new GetFileFromDfsRequest
            {
                DfsHash = option.FileHash
            };
        }

        public override void SendMessage(GetFileOptions opts)
        {
            var message = GetMessage(opts);
            var protocolMessage = message.ToProtocolMessage(SenderPeerIdentifier.PeerId);
            var correlationId = protocolMessage.CorrelationId.ToCorrelationId();

            var messageDto = new MessageDto(
                protocolMessage,
                RecipientPeerIdentifier);

            var fileTransfer = new DownloadFileTransferInformation(
                SenderPeerIdentifier,
                RecipientPeerIdentifier,
                Target.Channel,
                correlationId,
                opts.FileOutput,
                0
            );

            _downloadFileTransferFactory.RegisterTransfer(fileTransfer);
            Target.SendMessage(messageDto);

            while (!fileTransfer.ChunkIndicatorsTrue() && !fileTransfer.IsExpired())
            {
                CommandContext.UserOutput.Write($"\rDownloaded: {fileTransfer.GetPercentage().ToString()}%");
                Thread.Sleep(500);
            }

            if (fileTransfer.ChunkIndicatorsTrue())
                CommandContext.UserOutput.Write($"\rDownloaded: {fileTransfer.GetPercentage().ToString()}%\n");
            else
                CommandContext.UserOutput.WriteLine("\nFile transfer expired.");
        }
    }
}
