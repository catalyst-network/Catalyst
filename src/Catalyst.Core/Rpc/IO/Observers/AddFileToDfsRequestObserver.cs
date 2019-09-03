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
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.Enumerator;
using Catalyst.Abstractions.FileTransfer;
using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Abstractions.IO.Observers;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Extensions;
using Catalyst.Core.FileTransfer;
using Catalyst.Core.IO.Messaging.Dto;
using Catalyst.Core.IO.Observers;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using Serilog;

namespace Catalyst.Core.Rpc.IO.Observers
{
    /// <summary>
    /// The request handler to add a file to the DFS
    /// </summary>
    /// <seealso cref="IRpcRequestObserver" />
    public sealed class AddFileToDfsRequestObserver 
        : RequestObserverBase<AddFileToDfsRequest, AddFileToDfsResponse>,
            IRpcRequestObserver
    {
        /// <summary>The download file transfer factory</summary>
        private readonly IDownloadFileTransferFactory _fileTransferFactory;

        /// <summary>The DFS</summary>
        private readonly IDfs _dfs;

        /// <summary>Initializes a new instance of the <see cref="AddFileToDfsRequestObserver"/> class.</summary>
        /// <param name="dfs">The DFS.</param>
        /// <param name="peerIdentifier">The peer identifier.</param>
        /// <param name="fileTransferFactory">The download file transfer factory.</param>
        /// <param name="logger">The logger.</param>
        public AddFileToDfsRequestObserver(IDfs dfs,
            IPeerIdentifier peerIdentifier,
            IDownloadFileTransferFactory fileTransferFactory,
            ILogger logger) : base(logger, peerIdentifier)
        {
            _fileTransferFactory = fileTransferFactory;
            _dfs = dfs;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="addFileToDfsRequest"></param>
        /// <param name="channelHandlerContext"></param>
        /// <param name="senderPeerIdentifier"></param>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        protected override AddFileToDfsResponse HandleRequest(AddFileToDfsRequest addFileToDfsRequest,
            IChannelHandlerContext channelHandlerContext,
            IPeerIdentifier senderPeerIdentifier,
            ICorrelationId correlationId)
        {
            Guard.Argument(addFileToDfsRequest, nameof(addFileToDfsRequest)).NotNull();
            Guard.Argument(channelHandlerContext, nameof(channelHandlerContext)).NotNull();
            Guard.Argument(senderPeerIdentifier, nameof(senderPeerIdentifier)).NotNull();
            
            var fileTransferInformation = new DownloadFileTransferInformation(PeerIdentifier,
                senderPeerIdentifier, channelHandlerContext.Channel,
                correlationId, addFileToDfsRequest.FileName, addFileToDfsRequest.FileSize);

            FileTransferResponseCodeTypes responseCodeType;
            try
            {
                responseCodeType = _fileTransferFactory.RegisterTransfer(fileTransferInformation);
            }
            catch (Exception e)
            {
                Logger.Error(e,
                    "Failed to handle AddFileToDfsRequestHandler after receiving message {0}", addFileToDfsRequest);
                responseCodeType = FileTransferResponseCodeTypes.Error;
            }

            var message = GetResponse(fileTransferInformation, responseCodeType);

            if (responseCodeType == FileTransferResponseCodeTypes.Successful)
            {
                _fileTransferFactory.FileTransferAsync(fileTransferInformation.CorrelationId, CancellationToken.None).ContinueWith(task =>
                {
                    if (fileTransferInformation.ChunkIndicatorsTrue())
                    {
                        OnSuccessAsync(fileTransferInformation).Wait();
                    }

                    fileTransferInformation.Dispose();
                });
            }

            return message;
        }

        private async Task<FileTransferResponseCodeTypes> AddFileToDfs(IFileTransferInformation fileTransferInformation)
        {
            var responseCode = FileTransferResponseCodeTypes.Finished;

            try
            {
                string fileHash;

                using (var fileStream = File.Open(fileTransferInformation.TempPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    fileHash = await _dfs.AddAsync(fileStream, fileTransferInformation.FileOutputPath).ConfigureAwait(false);
                }

                fileTransferInformation.DfsHash = fileHash;

                Logger.Information($"Added File Name {fileTransferInformation.FileOutputPath} to DFS, Hash: {fileHash}");
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to handle file download OnSuccess {0}", fileTransferInformation.CorrelationId.Id);
                responseCode = FileTransferResponseCodeTypes.Failed;
            }
            finally
            {
                fileTransferInformation.Delete();
            }

            return responseCode;
        }

        /// <summary>Called when [success] on file transfer.</summary>
        /// <param name="fileTransferInformation">The file transfer information.</param>
        private async Task OnSuccessAsync(IFileTransferInformation fileTransferInformation)
        {
            var addFileResponseCode = AddFileToDfs(fileTransferInformation).ConfigureAwait(false);

            var message = GetResponse(fileTransferInformation, await addFileResponseCode);
            var protocolMessage =
                message.ToProtocolMessage(PeerIdentifier.PeerId, fileTransferInformation.CorrelationId);

            // Send Response
            var responseMessage = new MessageDto(
                protocolMessage,
                fileTransferInformation.RecipientIdentifier
            );

            await fileTransferInformation.RecipientChannel.WriteAndFlushAsync(responseMessage).ConfigureAwait(false);
        }

        /// <param name="fileTransferInformation">The file transfer information.</param>
        /// <param name="responseCode">The response code.</param>
        private AddFileToDfsResponse GetResponse(IFileTransferInformation fileTransferInformation, Enumeration responseCode)
        {
            Logger.Information("File transfer response code: " + responseCode);
            if (responseCode == FileTransferResponseCodeTypes.Successful)
            {
                Logger.Information($"Initialised file transfer, FileName: {fileTransferInformation.FileOutputPath}, Chunks: {fileTransferInformation.MaxChunk.ToString()}");
            }

            var dfsHash = responseCode == FileTransferResponseCodeTypes.Finished ? fileTransferInformation.DfsHash : string.Empty;

            // Build Response
            var response = new AddFileToDfsResponse
            {
                ResponseCode = ByteString.CopyFrom((byte) responseCode.Id),
                DfsHash = dfsHash
            };

            return response;
        }
    }
}
