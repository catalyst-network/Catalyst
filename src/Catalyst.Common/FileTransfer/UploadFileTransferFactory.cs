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
using System.Threading.Tasks;
using Catalyst.Common.Interfaces.FileTransfer;

namespace Catalyst.Common.FileTransfer
{
    /// <summary>
    /// Handles and stores the upload file transfers
    /// </summary>
    /// <seealso cref="BaseFileTransferFactory{IUploadFileInformation}" />
    /// <seealso cref="IUploadFileTransferFactory" />
    public sealed class UploadFileTransferFactory : BaseFileTransferFactory<IUploadFileInformation>, IUploadFileTransferFactory
    {
        /// <summary>Does the transfer.</summary>
        /// <param name="fileTransferInformation">The file transfer information.</param>
        /// <returns></returns>
        protected override async Task DoTransfer(IUploadFileInformation fileTransferInformation)
        {
            EnsureKeyExists(fileTransferInformation.CorrelationGuid);
            await Upload(fileTransferInformation).ConfigureAwait(false);
        }

        /// <summary>Uploads the specified file transfer information.</summary>
        /// <param name="fileTransferInformation">The file transfer information.</param>
        /// <returns></returns>
        private async Task Upload(IUploadFileInformation fileTransferInformation)
        {
            EnsureKeyExists(fileTransferInformation.CorrelationGuid);
            var cancellationRequested =
                fileTransferInformation.IsExpired();

            for (uint i = 0; i < fileTransferInformation.MaxChunk; i++)
            {
                if (cancellationRequested)
                {
                    return;
                }

                cancellationRequested = fileTransferInformation.IsExpired();

                var transferMessage = fileTransferInformation.GetUploadMessageDto(i);
                try
                {
                    await fileTransferInformation.RecipientChannel.WriteAndFlushAsync(transferMessage);
                    fileTransferInformation.UpdateChunkIndicator(i, true);
                }
                catch (Exception)
                {
                    var canRetry = fileTransferInformation.CanRetry();
                    
                    if (canRetry)
                    {
                        fileTransferInformation.RetryCount += 1;
                        i--;
                    }
                    else
                    {
                        return;
                    }
                }
            }
        }
    }
}
