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
using Catalyst.Common.FileTransfer;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.Interfaces.Cli.Commands;
using Catalyst.Common.Interfaces.Cli.Options;
using Catalyst.Common.Interfaces.FileTransfer;
using Catalyst.Common.Interfaces.Rpc;
using Catalyst.Common.P2P;
using Catalyst.Protocol.Rpc.Node;
using Dawn;

namespace Catalyst.Cli.Commands
{
    public sealed class AddFileCommand : CommandBase<AddFileToDfsRequest, IAddFileOnDfsOptions>
    {
        private readonly IUserOutput _userOutput;
        private readonly IUploadFileTransferFactory _uploadFileTransferFactory;

        public AddFileCommand(IUploadFileTransferFactory uploadFileTransferFactory,
            IUserOutput userOutput,
            IOptionsBase optionBase,
            ICommandContext commandContext) : base(optionBase, commandContext)
        {
            _uploadFileTransferFactory = uploadFileTransferFactory;
            _userOutput = userOutput;
        }

        public override AddFileToDfsRequest GetMessage(IAddFileOnDfsOptions option)
        {
            return new AddFileToDfsRequest
            {
                FileName = Path.GetFileName(option.File)
            };
        }

        public override bool SendMessage(IAddFileOnDfsOptions options)
        {
            if (!File.Exists(options.File))
            {
                _userOutput.WriteLine("File does not exist.");
                return false;
            }

            var request = GetMessage(options);

            using (var fileStream = File.Open(options.File, FileMode.Open))
            {
                request.FileSize = (ulong) fileStream.Length;
            }

            var requestMessage = CommandContext.DtoFactory.GetDto(
                request,
                SenderPeerIdentifier,
                RecipientPeerIdentifier
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

            return true;
        }
    }
}
