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
using System.Threading;
using Catalyst.Common.Config;
using Catalyst.Common.Interfaces.Modules.Dfs;
using Catalyst.Common.Interfaces.P2P;
using Google.Protobuf;
using Catalyst.Common.FileTransfer;
using Catalyst.Common.Interfaces.FileTransfer;
using Catalyst.Node.Core.Rpc.Messaging;
using Catalyst.Common.P2P;
using DotNetty.Transport.Channels;

namespace Catalyst.Node.Core.RPC.Handlers
{
    /// <summary>
    /// The request handler to get a file from the DFS
    /// </summary>
    /// <seealso cref="CorrelatableMessageHandlerBase{GetFileFromDfsRequest, IMessageCorrelationCache}" />
    /// <seealso cref="IRpcRequestHandler" />
    public sealed class GetFileFromDfsRequestHandler : CorrelatableMessageHandlerBase<GetFileFromDfsRequest, IMessageCorrelationCache>,
        IRpcRequestHandler
    {
        /// <summary>The RPC message factory</summary>
        private readonly RpcMessageFactory<GetFileFromDfsResponse> _rpcMessageFactory;

        /// <summary>The upload file transfer factory</summary>
        private readonly IUploadFileTransferFactory _fileTransferFactory;

        /// <summary>The peer identifier</summary>
        private readonly IPeerIdentifier _peerIdentifier;

        /// <summary>The DFS</summary>
        private readonly IDfs _dfs;

        /// <summary>Initializes a new instance of the <see cref="AddFileToDfsRequestHandler"/> class.</summary>
        /// <param name="dfs">The DFS.</param>
        /// <param name="peerIdentifier">The peer identifier.</param>
        /// <param name="fileTransferFactory">The upload file transfer factory.</param>
        /// <param name="correlationCache">The correlation cache.</param>
        /// <param name="logger">The logger.</param>
        public GetFileFromDfsRequestHandler(IDfs dfs,
            IPeerIdentifier peerIdentifier,
            IUploadFileTransferFactory fileTransferFactory,
            IMessageCorrelationCache correlationCache,
            ILogger logger) : base(correlationCache, logger)
        {
            _rpcMessageFactory = new RpcMessageFactory<GetFileFromDfsResponse>();
            _fileTransferFactory = fileTransferFactory;
            _dfs = dfs;
            _peerIdentifier = peerIdentifier;
        }

        /// <summary>Handles the specified message.</summary>
        /// <param name="message">The message.</param>
        protected override void Handler(IChanneledMessage<AnySigned> message)
        {
            var deserialised = message.Payload.FromAnySigned<GetFileFromDfsRequest>();

            Guard.Argument(deserialised).NotNull("Message cannot be null");

            var recipientPeerIdentifier = new PeerIdentifier(message.Payload.PeerId);
            var correlationGuid = message.Payload.CorrelationId.ToGuid();
            long fileLen = 0;
            FileTransferResponseCodes responseCode;
            MemoryStream ms = null;

            try
            {
                using (var stream = _dfs.ReadAsync(deserialised.DfsHash).GetAwaiter().GetResult())
                {
                    ms = new MemoryStream();
                    stream.CopyTo(ms);
                    fileLen = stream.Length;
                    
                    IUploadFileInformation fileTransferInformation = new UploadFileTransferInformation(
                        ms,
                        _peerIdentifier,
                        recipientPeerIdentifier,
                        message.Context.Channel,
                        correlationGuid,
                        new RpcMessageFactory<TransferFileBytesRequest>()
                    );
                    responseCode = _fileTransferFactory.RegisterTransfer(fileTransferInformation);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e,
                    "Failed to handle GetFileFromDfsRequestHandler after receiving message {0}", message);
                responseCode = FileTransferResponseCodes.Error;
            }

            ReturnResponse(recipientPeerIdentifier, message.Context.Channel, responseCode, correlationGuid, fileLen);

            if (responseCode == FileTransferResponseCodes.Successful)
            {
                _fileTransferFactory.FileTransferAsync(correlationGuid, CancellationToken.None);
            }
            else
            {
                ms?.Dispose();
            }
        }

        /// <summary>Returns the response.</summary>
        /// <param name="recipientIdentifier">The recipient identifier.</param>
        /// <param name="recipientChannel">The recipient channel.</param>
        /// <param name="responseCode">The response code.</param>
        /// <param name="correlationGuid">The correlation unique identifier.</param>
        /// <param name="fileSize">Size of the file.</param>
        private void ReturnResponse(IPeerIdentifier recipientIdentifier, IChannel recipientChannel, FileTransferResponseCodes responseCode, Guid correlationGuid, long fileSize)
        {
            Logger.Information("File upload response code: " + responseCode);
            
            // Build Response
            var response = new GetFileFromDfsResponse
            {
                ResponseCode = ByteString.CopyFrom((byte) responseCode.Id),
                FileSize = (ulong) fileSize
            };

            // Send Response
            var responseMessage = _rpcMessageFactory.GetMessage(
                message: response,
                recipient: recipientIdentifier,
                sender: _peerIdentifier,
                messageType: MessageTypes.Tell,
                correlationGuid
            );

            recipientChannel.WriteAndFlushAsync(responseMessage).GetAwaiter().GetResult();
        }
    }
}
