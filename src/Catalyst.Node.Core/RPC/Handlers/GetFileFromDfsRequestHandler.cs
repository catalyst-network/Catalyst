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
using Catalyst.Common.Config;
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
    public sealed class GetFileFromDfsRequestHandler : CorrelatableMessageHandlerBase<GetFileFromDfsRequest, IMessageCorrelationCache>,
        IRpcRequestHandler
    {
        /// <summary>The RPC message factory</summary>
        private readonly RpcMessageFactory<GetFileFromDfsResponse> _rpcMessageFactory;

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
        public GetFileFromDfsRequestHandler(IDfs dfs,
            IPeerIdentifier peerIdentifier,
            IFileTransfer fileTransfer,
            IMessageCorrelationCache correlationCache,
            ILogger logger) : base(correlationCache, logger)
        {
            _rpcMessageFactory = new RpcMessageFactory<GetFileFromDfsResponse>();
            _fileTransfer = fileTransfer;
            _dfs = dfs;
            _peerIdentifier = peerIdentifier;
        }

        /// <summary>Handles the specified message.</summary>
        /// <param name="message">The message.</param>
        protected override void Handler(IChanneledMessage<AnySigned> message)
        {
            var deserialised = message.Payload.FromAnySigned<GetFileFromDfsRequest>();

            var stream = _dfs.ReadAsync(deserialised.DfsHash).Result;
            long fileLen = stream?.Length ?? 0;

            Guard.Argument(deserialised).NotNull("Message cannot be null");
            
            IFileTransferInformation fileTransferInformation = FileTransferInformation.BuildUpload(
                stream,
                _peerIdentifier,
                new PeerIdentifier(message.Payload.PeerId),
                message.Context.Channel,
                message.Payload.CorrelationId.ToGuid(),
                new RpcMessageFactory<TransferFileBytesRequest>()
            );
            
            FileTransferResponseCodes responseCode;
            try
            {
                responseCode = _fileTransfer.InitializeTransfer(fileTransferInformation);
            }
            catch (Exception e)
            {
                Logger.Error(e.ToString());
                responseCode = FileTransferResponseCodes.Error;
            }

            ReturnResponse(fileTransferInformation, responseCode, fileTransferInformation.CorrelationGuid, fileLen);

            fileTransferInformation.Upload();
        }

        /// <param name="fileTransferInformation">The file transfer information.</param>
        /// <param name="responseCode">The response code.</param>
        private void ReturnResponse(IFileTransferInformation fileTransferInformation, FileTransferResponseCodes responseCode, Guid correlationGuid, long fileSize)
        {
            Logger.Information("File upload response code: " + responseCode);
            if (responseCode == FileTransferResponseCodes.Successful)
            {
                Logger.Information($"Initialised file upload, FileName: {fileTransferInformation.FileOutputPath}, Chunks: {fileTransferInformation.MaxChunk}");
            }

            // Build Response
            var response = new GetFileFromDfsResponse()
            {
                ResponseCode = ByteString.CopyFrom((byte) responseCode.Id),
                FileSize = (ulong) fileSize
            };

            // Send Response
            var responseMessage = _rpcMessageFactory.GetMessage(
                message: response,
                recipient: fileTransferInformation.RecipientIdentifier,
                sender: _peerIdentifier,
                messageType: MessageTypes.Tell,
                correlationGuid
            );

            fileTransferInformation.RecipientChannel.WriteAndFlushAsync(responseMessage).GetAwaiter().GetResult();
        }
    }
}
