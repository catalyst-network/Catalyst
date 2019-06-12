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
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.FileTransfer;
using Catalyst.Common.Interfaces.FileTransfer;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.IO.Observables;
using Catalyst.Common.Interfaces.Modules.Dfs;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Common.IO.Observables;
using Catalyst.Common.P2P;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using Google.Protobuf;
using Serilog;

namespace Catalyst.Node.Core.RPC.Observables
{
    /// <summary>
    /// The request handler to add a file to the DFS
    /// </summary>
    /// <seealso cref="IRpcRequestObserver" />
    public sealed class AddFileToDfsRequestObserver : ObserverBase,
        IRpcRequestObserver
    {
        /// <summary>The RPC message factory</summary>
        private readonly IProtocolMessageFactory _protocolMessageFactory;

        /// <summary>The download file transfer factory</summary>
        private readonly IDownloadFileTransferFactory _fileTransferFactory;

        /// <summary>The peer identifier</summary>
        private readonly IPeerIdentifier _peerIdentifier;

        /// <summary>The DFS</summary>
        private readonly IDfs _dfs;

        /// <summary>Initializes a new instance of the <see cref="AddFileToDfsRequestObserver"/> class.</summary>
        /// <param name="dfs">The DFS.</param>
        /// <param name="peerIdentifier">The peer identifier.</param>
        /// <param name="fileTransferFactory">The download file transfer factory.</param>
        /// <param name="protocolMessageFactory"></param>
        /// <param name="logger">The logger.</param>
        public AddFileToDfsRequestObserver(IDfs dfs,
            IPeerIdentifier peerIdentifier,
            IDownloadFileTransferFactory fileTransferFactory,
            IProtocolMessageFactory protocolMessageFactory,
            ILogger logger) : base(logger)
        {
            _protocolMessageFactory = protocolMessageFactory;
            _fileTransferFactory = fileTransferFactory;
            _dfs = dfs;
            _peerIdentifier = peerIdentifier;
        }

        /// <summary>Handles the specified message.</summary>
        /// <param name="messageDto">The message.</param>
        protected override void Handler(IProtocolMessageDto<ProtocolMessage> messageDto)
        {
            var deserialised = messageDto.Payload.FromProtocolMessage<AddFileToDfsRequest>();

            Guard.Argument(deserialised).NotNull("Message cannot be null");
            
            IDownloadFileInformation fileTransferInformation = new DownloadFileTransferInformation(
                _peerIdentifier,
                new PeerIdentifier(messageDto.Payload.PeerId),
                messageDto.Context.Channel,
                messageDto.Payload.CorrelationId.ToGuid(),
                deserialised.FileName,
                deserialised.FileSize);
            
            FileTransferResponseCodes responseCode;
            try
            {
                responseCode = _fileTransferFactory.RegisterTransfer(fileTransferInformation);
            }
            catch (Exception e)
            {
                Logger.Error(e,
                    "Failed to handle AddFileToDfsRequestHandler after receiving message {0}", messageDto);
                responseCode = FileTransferResponseCodes.Error;
            }

            ReturnResponse(fileTransferInformation, responseCode, fileTransferInformation.CorrelationGuid);

            if (responseCode == FileTransferResponseCodes.Successful)
            {
                _fileTransferFactory.FileTransferAsync(fileTransferInformation.CorrelationGuid, CancellationToken.None).ContinueWith(task =>
                {
                    if (fileTransferInformation.ChunkIndicatorsTrue())
                    {
                        OnSuccess(fileTransferInformation);
                    }
                });
            }
        }

        /// <summary>Called when [success] on file transfer.</summary>
        /// <param name="fileTransferInformation">The file transfer information.</param>
        private void OnSuccess(IFileTransferInformation fileTransferInformation)
        {
            var addFileResponseCode = Task.Run(() =>
            {
                var responseCode = FileTransferResponseCodes.Finished;

                try
                {
                    string fileHash;
                    using (var fileStream = File.Open(fileTransferInformation.TempPath, FileMode.Open,
                        FileAccess.Read, FileShare.ReadWrite))
                    {
                        fileHash = _dfs.AddAsync(fileStream, fileTransferInformation.FileOutputPath).ConfigureAwait(false).GetAwaiter().GetResult();
                    }

                    fileTransferInformation.DfsHash = fileHash;

                    Logger.Information(
                        $"Added File Name {fileTransferInformation.FileOutputPath} to DFS, Hash: {fileHash}");
                }
                catch (Exception e)
                {
                    Logger.Error(e,
                        "Failed to handle file download OnSuccess {0}", fileTransferInformation.CorrelationGuid);
                    responseCode = FileTransferResponseCodes.Failed;
                }
                finally
                {
                    fileTransferInformation.Delete();
                }

                return responseCode;
            }).ConfigureAwait(false).GetAwaiter().GetResult();

            ReturnResponse(fileTransferInformation, addFileResponseCode, fileTransferInformation.CorrelationGuid);
        }

        /// <param name="fileTransferInformation">The file transfer information.</param>
        /// <param name="responseCode">The response code.</param>
        /// <param name="correlationGuid"></param>
        private void ReturnResponse(IFileTransferInformation fileTransferInformation, FileTransferResponseCodes responseCode, Guid correlationGuid)
        {
            Logger.Information("File transfer response code: " + responseCode);
            if (responseCode == FileTransferResponseCodes.Successful)
            {
                Logger.Information($"Initialised file transfer, FileName: {fileTransferInformation.FileOutputPath}, Chunks: {fileTransferInformation.MaxChunk.ToString()}");
            }

            var dfsHash = responseCode == FileTransferResponseCodes.Finished ? fileTransferInformation.DfsHash : string.Empty;

            // Build Response
            var response = new AddFileToDfsResponse
            {
                ResponseCode = ByteString.CopyFrom((byte) responseCode.Id),
                DfsHash = dfsHash
            };

            // Send Response
            var responseMessage = _protocolMessageFactory.GetMessage(new MessageDto(
                    response,
                    MessageTypes.Response,
                    fileTransferInformation.RecipientIdentifier,
                    _peerIdentifier
                ),
                correlationGuid
            );

            fileTransferInformation.RecipientChannel.WriteAndFlushAsync(responseMessage);
        }
    }
}
