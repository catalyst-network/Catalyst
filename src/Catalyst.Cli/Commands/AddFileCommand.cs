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

using Catalyst.Cli.Options;
using Catalyst.Common.FileTransfer;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.Interfaces.FileTransfer;
using Catalyst.Protocol.Rpc.Node;
using System.IO;
using Catalyst.Cli.CommandTypes;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.Cli.Commands;

namespace Catalyst.Cli.Commands
{
    public sealed class AddFileCommand : BaseMessageCommand<AddFileToDfsRequest, AddFileToDfsResponse, AddFileOptions>
    {
        public static string ErrorNoResponseCodes => "No response codes were found.";
        private readonly IUploadFileTransferFactory _uploadFileTransferFactory;

        public AddFileCommand(IUploadFileTransferFactory uploadFileTransferFactory,
            ICommandContext commandContext) : base(commandContext)
        {
            _uploadFileTransferFactory = uploadFileTransferFactory;
        }

        protected override AddFileToDfsRequest GetMessage(AddFileOptions option)
        {
            return new AddFileToDfsRequest
            {
                FileName = Path.GetFileName(option.File)
            };
        }

        protected override bool ExecuteCommand(AddFileOptions options)
        {
            return File.Exists(options.File);
        }

        public override void SendMessage(AddFileOptions options)
        {
            var request = GetMessage(options);

            using (var fileStream = File.Open(options.File, FileMode.Open))
            {
                request.FileSize = (ulong) fileStream.Length;
            }

            var protocolMessage = request.ToProtocolMessage(SenderPeerIdentifier.PeerId);
            var requestMessage = CommandContext.DtoFactory.GetDto(
                protocolMessage,
                SenderPeerIdentifier,
                RecipientPeerIdentifier,
                protocolMessage.CorrelationId.ToCorrelationId()
            );

            IUploadFileInformation fileTransfer = new UploadFileTransferInformation(
                File.Open(options.File, FileMode.Open),
                SenderPeerIdentifier,
                RecipientPeerIdentifier,
                Target.Channel,
                requestMessage.CorrelationId,
                CommandContext.DtoFactory);

            _uploadFileTransferFactory.RegisterTransfer(fileTransfer);

            Target.SendMessage(requestMessage);

            while (!fileTransfer.ChunkIndicatorsTrue() && !fileTransfer.IsExpired())
            {
                CommandContext.UserOutput.Write($"\rUploaded: {fileTransfer.GetPercentage().ToString()}%");
                System.Threading.Thread.Sleep(500);
            }

            if (fileTransfer.ChunkIndicatorsTrue())
            {
                CommandContext.UserOutput.Write($"\rUploaded: {fileTransfer.GetPercentage().ToString()}%\n");
            }
            else
            {
                CommandContext.UserOutput.WriteLine("\nFile transfer expired.");
            }
        }

        protected override void ResponseMessage(AddFileToDfsResponse response)
        {
            if (response.ResponseCode.IsEmpty)
            {
                CommandContext.UserOutput.WriteLine(ErrorNoResponseCodes);
                return;
            }

            var responseCode = (FileTransferResponseCodes) response.ResponseCode[0];
            if (responseCode == FileTransferResponseCodes.Failed || responseCode == FileTransferResponseCodes.Finished)
            {
                CommandContext.UserOutput.WriteLine("File transfer completed, Response: " + responseCode.Name + " Dfs Hash: " + response.DfsHash);
            }
        }
    }
}
