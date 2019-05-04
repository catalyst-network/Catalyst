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

using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.IO.Messaging.Handlers;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using Serilog;
using Catalyst.Common.Extensions;
using System;
using System.IO;
using System.Threading.Tasks;
using Catalyst.Common.Enums.FileTransfer;
using Catalyst.Common.Enums.Messages;
using Catalyst.Common.Interfaces.Modules.Dfs;
using Catalyst.Common.Interfaces.P2P;
using Google.Protobuf;
using Catalyst.Common.FileTransfer;
using Catalyst.Common.Interfaces.FileTransfer;
using Catalyst.Node.Core.Rpc.Messaging;
using Catalyst.Common.P2P;

namespace Catalyst.Node.Core.RPC.Handlers
{
    /// <summary>
    /// The request handler to add a file to the DFS
    /// </summary>
    /// <seealso cref="CorrelatableMessageHandlerBase{AddFileToDfsRequest, IMessageCorrelationCache}" />
    /// <seealso cref="IRpcRequestHandler" />
    public sealed class AddFileToDfsRequestHandler : CorrelatableMessageHandlerBase<AddFileToDfsRequest, IMessageCorrelationCache>,
        IRpcRequestHandler
    {
        /// <summary>The RPC message factory</summary>
        private readonly RpcMessageFactory<AddFileToDfsResponse> _rpcMessageFactory;

        /// <summary>The file transfer</summary>
        private readonly IFileTransfer _fileTransfer;

        /// <summary>The peer identifier</summary>
        private readonly IPeerIdentifier _peerIdentifier;

        /// <summary>The DFS</summary>
        private readonly IDfs _dfs;

        /// <summary>Initializes a new instance of the <see cref="AddFileToDfsRequestHandler"/> class.</summary>
        /// <param name="dfs">The DFS.</param>
        /// <param name="peerIdentifier">The peer identifier.</param>
        /// <param name="fileTransfer">The file transfer.</param>
        /// <param name="correlationCache">The correlation cache.</param>
        /// <param name="logger">The logger.</param>
        public AddFileToDfsRequestHandler(IDfs dfs,
            IPeerIdentifier peerIdentifier,
            IFileTransfer fileTransfer,
            IMessageCorrelationCache correlationCache,
            ILogger logger) : base(correlationCache, logger)
        {
            _rpcMessageFactory = new RpcMessageFactory<AddFileToDfsResponse>();
            _fileTransfer = fileTransfer;
            _dfs = dfs;
            _peerIdentifier = peerIdentifier;
        }

        /// <summary>Handles the specified message.</summary>
        /// <param name="message">The message.</param>
        protected override void Handler(IChanneledMessage<AnySigned> message)
        {
            var deserialised = message.Payload.FromAnySigned<AddFileToDfsRequest>();

            Guard.Argument(deserialised).NotNull("Message cannot be null");

            var chunkSize = (uint) Math.Max(1, (int) Math.Ceiling((double) deserialised.FileSize / FileTransferConstants.ChunkSize));

            IFileTransferInformation fileTransferInformation = new FileTransferInformation(
                new PeerIdentifier(message.Payload.PeerId),
                message.Context.Channel,
                message.Payload.CorrelationId.ToGuid().ToString(),
                deserialised.FileName, chunkSize);
            fileTransferInformation.AddSuccessCallback(OnSuccess);

            AddFileToDfsResponseCode responseCode;
            try
            {
                responseCode = _fileTransfer.InitializeTransfer(fileTransferInformation);
            }
            catch (Exception e)
            {
                Logger.Error(e.ToString());
                responseCode = AddFileToDfsResponseCode.Error;
            }

            ReturnResponse(fileTransferInformation, responseCode);
        }

        /// <summary>Called when [success] on file transfer.</summary>
        /// <param name="fileTransferInformation">The file transfer information.</param>
        private void OnSuccess(IFileTransferInformation fileTransferInformation)
        {
            var addFileResponseCode = Task.Run(() =>
            {
                var responseCode = AddFileToDfsResponseCode.Finished;

                try
                {
                    string fileHash;
                    using (var fileStream = File.Open(fileTransferInformation.TempPath, FileMode.Open))
                    {
                        fileHash = _dfs.AddAsync(fileStream, fileTransferInformation.FileName).Result;
                    }

                    fileTransferInformation.DfsHash = fileHash;

                    Logger.Information($"Added File Name {fileTransferInformation.FileName} to DFS, Hash: {fileHash}");
                }
                catch (Exception e)
                {
                    Logger.Error(e.ToString());
                    responseCode = AddFileToDfsResponseCode.Failed;
                }

                return responseCode;
            }).GetAwaiter().GetResult();

            ReturnResponse(fileTransferInformation, addFileResponseCode);
        }

        /// <param name="fileTransferInformation">The file transfer information.</param>
        /// <param name="responseCode">The response code.</param>
        private void ReturnResponse(IFileTransferInformation fileTransferInformation, AddFileToDfsResponseCode responseCode)
        {
            Logger.Information("File transfer response code: " + responseCode);
            if (responseCode == AddFileToDfsResponseCode.Successful)
            {
                Logger.Information($"Initialised file transfer, FileName: {fileTransferInformation.FileName}, Chunks: {fileTransferInformation.MaxChunk}");
            }

            var dfsHash = responseCode == AddFileToDfsResponseCode.Finished ? fileTransferInformation.DfsHash : string.Empty;

            // Build Response
            var response = new AddFileToDfsResponse
            {
                ResponseCode = ByteString.CopyFrom((byte) responseCode),
                DfsHash = dfsHash
            };

            // Send Response
            var responseMessage = _rpcMessageFactory.GetMessage(
                message: response,
                recipient: fileTransferInformation.RecipientIdentifier,
                sender: _peerIdentifier,
                messageType: DtoMessageType.Tell,
                Guid.NewGuid()
            );

            fileTransferInformation.RecipientChannel.WriteAndFlushAsync(responseMessage);
        }
    }
}
