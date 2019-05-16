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
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.FileTransfer;
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Messaging.Handlers;
using Catalyst.Node.Core.Rpc.Messaging;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using Google.Protobuf;
using Serilog;
using Catalyst.Common.Interfaces.FileTransfer;
using Catalyst.Common.P2P;
using Microsoft.Extensions.Configuration;

namespace Catalyst.Cli.Handlers
{
    public sealed class TransferFileBytesRequestHandler
        : CorrelatableMessageHandlerBase<TransferFileBytesRequest, IMessageCorrelationCache>,
            IRpcResponseHandler
    {
        /// <summary>The download file transfer factory</summary>
        private readonly IDownloadFileTransferFactory _fileTransferFactory;

        /// <summary>The RPC message factory</summary>
        private readonly RpcMessageFactory<TransferFileBytesResponse> _rpcMessageFactory;

        /// <summary>The peer identifier</summary>
        private readonly IPeerIdentifier _peerIdentifier;

        /// <summary>Initializes a new instance of the <see cref="TransferFileBytesRequestHandler"/> class.</summary>
        /// <param name="fileTransferFactory">The download file transfer factory.</param>
        /// <param name="config">The configuration.</param>
        /// <param name="correlationCache">The correlation cache.</param>
        /// <param name="logger">The logger.</param>
        public TransferFileBytesRequestHandler(IDownloadFileTransferFactory fileTransferFactory,
            IConfigurationRoot config,
            IMessageCorrelationCache correlationCache,
            ILogger logger)
            : base(correlationCache, logger)
        {
            _fileTransferFactory = fileTransferFactory;
            _rpcMessageFactory = new RpcMessageFactory<TransferFileBytesResponse>(correlationCache);
            _peerIdentifier = Commands.Commands.BuildCliPeerId(config);
        }

        /// <summary>Initializes a new instance of the <see cref="TransferFileBytesRequestHandler"/> class.</summary>
        /// <param name="fileTransferFactory">The download file transfer factory.</param>
        /// <param name="peerIdentifier">The peer identifier.</param>
        /// <param name="correlationCache">The correlation cache.</param>
        /// <param name="logger">The logger.</param>
        public TransferFileBytesRequestHandler(IDownloadFileTransferFactory fileTransferFactory,
            IPeerIdentifier peerIdentifier,
            IMessageCorrelationCache correlationCache,
            ILogger logger)
            : base(correlationCache, logger)
        {
            _fileTransferFactory = fileTransferFactory;
            _rpcMessageFactory = new RpcMessageFactory<TransferFileBytesResponse>(_correlationCache);
            _peerIdentifier = peerIdentifier;
        }

        /// <summary>Handles the specified message.</summary>
        /// <param name="message">The message.</param>
        protected override void Handler(IChanneledMessage<AnySigned> message)
        {
            var deserialised = message.Payload.FromAnySigned<TransferFileBytesRequest>();
            FileTransferResponseCodes responseCode;

            try
            {
                Guard.Argument(deserialised).NotNull("Message cannot be null");

                var correlationId = new Guid(deserialised.CorrelationFileName.ToByteArray());
                responseCode = _fileTransferFactory.DownloadChunk(correlationId, deserialised.ChunkId, deserialised.ChunkBytes.ToByteArray());
            }
            catch (Exception e)
            {
                Logger.Error(e,
                    "Failed to handle TransferFileBytesRequestHandler after receiving message {0}", message);
                responseCode = FileTransferResponseCodes.Error;
            }

            var responseMessage = new TransferFileBytesResponse
            {
                ResponseCode = ByteString.CopyFrom((byte) responseCode.Id)
            };

            var responseDto = _rpcMessageFactory.GetMessage(
                message: responseMessage,
                recipient: new PeerIdentifier(message.Payload.PeerId),
                sender: _peerIdentifier,
                messageType: MessageTypes.Tell,
                message.Payload.CorrelationId.ToGuid()
            );

            message.Context.Channel.WriteAndFlushAsync(responseDto);
        }
    }
}
