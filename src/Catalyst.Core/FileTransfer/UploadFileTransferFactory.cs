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
using System.Collections.Generic;
using System.Threading.Tasks;
using Catalyst.Abstractions.FileTransfer;
using Catalyst.Core.Config;
using Polly;
using Polly.Retry;
using Serilog;

namespace Catalyst.Core.FileTransfer
{
    /// <summary>
    /// Handles and stores the upload file transfers
    /// </summary>
    /// <seealso cref="BaseFileTransferFactory{IUploadFileInformation}" />
    /// <seealso cref="IUploadFileTransferFactory" />
    public sealed class UploadFileTransferFactory : BaseFileTransferFactory<IUploadFileInformation>, IUploadFileTransferFactory
    {
        /// <summary>The start chunk retry key</summary>
        private static readonly string StartChunkRetryKey = "StartChunk";

        /// <summary>The retry policy</summary>
        private readonly AsyncRetryPolicy _retryPolicy;
        
        public UploadFileTransferFactory(ILogger logger) : base(logger)
        {
            _retryPolicy = Policy
               .Handle<Exception>()
               .RetryAsync(Constants.FileTransferMaxChunkRetryCount);
        }

        /// <summary>Does the transfer.</summary>
        /// <param name="fileTransferInformation">The file transfer information.</param>
        /// <returns></returns>
        protected override async Task DoTransferAsync(IUploadFileInformation fileTransferInformation)
        {
            EnsureKeyExists(fileTransferInformation.CorrelationId);
            await UploadAsync(fileTransferInformation).ConfigureAwait(false);
        }

        /// <summary>Uploads the specified file transfer information.</summary>
        /// <param name="fileTransferInformation">The file transfer information.</param>
        /// <returns></returns>
        private async Task UploadAsync(IUploadFileInformation fileTransferInformation)
        {
            EnsureKeyExists(fileTransferInformation.CorrelationId);

            Context context =
                new Context(fileTransferInformation.CorrelationId.Id.ToString(), new Dictionary<string, object>())
                {
                    {StartChunkRetryKey, (uint) 0}
                };

            await _retryPolicy.ExecuteAsync(ctx => SendChunksAsync(fileTransferInformation, ctx), context);
        }

        /// <summary>Sends the chunks.</summary>
        /// <param name="fileTransferInformation">The file transfer information.</param>
        /// <param name="retryContext">The retry context.</param>
        /// <returns></returns>
        private async Task SendChunksAsync(IUploadFileInformation fileTransferInformation, Context retryContext)
        {
            var cancellationRequested = fileTransferInformation.IsExpired();
            retryContext.TryGetValue(StartChunkRetryKey, out var value);
            uint startChunk = (uint?) value ?? 0;
            for (uint chunkId = startChunk; chunkId < fileTransferInformation.MaxChunk; chunkId++)
            {
                if (cancellationRequested)
                {
                    return;
                }

                retryContext[StartChunkRetryKey] = chunkId;
                cancellationRequested = fileTransferInformation.IsExpired();

                var transferMessage = fileTransferInformation.GetUploadMessageDto(chunkId);

                await fileTransferInformation.RecipientChannel.WriteAndFlushAsync(transferMessage);
                fileTransferInformation.UpdateChunkIndicator(chunkId, true);
            }
        }
    }
}
