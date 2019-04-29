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

using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Messaging.Handlers;
using Catalyst.Common.Rpc;
using Catalyst.Node.Core.P2P.Messaging;
using Catalyst.Node.Core.Rpc.Messaging;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using Google.Protobuf;
using Serilog;
using System;
using System.Net;
using Catalyst.Node.Core.Modules.FileTransfer;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.P2P;

namespace Catalyst.Node.Core.RPC.Handlers
{
    public class TransferFileBytesRequestHandler : CorrelatableMessageHandlerBase<TransferFileBytesRequest, IMessageCorrelationCache>,
            IRpcRequestHandler
    {
        /// <summary>The file transfer</summary>
        private IFileTransfer _fileTransfer;

        /// <summary>The RPC message factory</summary>
        private RpcMessageFactory<TransferFileBytesResponse, RpcMessages> _rpcMessageFactory;

        /// <summary>The peer identifier</summary>
        private IPeerIdentifier _peerIdentifier;

        /// <summary>Initializes a new instance of the <see cref="TransferFileBytesRequestHandler"/> class.</summary>
        /// <param name="fileTransfer">The file transfer.</param>
        /// <param name="peerIdentifier">The peer identifier.</param>
        /// <param name="correlationCache">The correlation cache.</param>
        /// <param name="logger">The logger.</param>
        public TransferFileBytesRequestHandler(IFileTransfer fileTransfer, IPeerIdentifier peerIdentifier, IMessageCorrelationCache correlationCache, ILogger logger) : base(correlationCache, logger)
        {
            _fileTransfer = fileTransfer;
            _rpcMessageFactory = new RpcMessageFactory<TransferFileBytesResponse, RpcMessages>();
            _peerIdentifier = peerIdentifier;
        }

        /// <summary>Handles the specified message.</summary>
        /// <param name="message">The message.</param>
        protected override void Handler(IChanneledMessage<AnySigned> message)
        {
            var deserialised = message.Payload.FromAnySigned<TransferFileBytesRequest>();
            FileTransferInformation fileTransferInformation = null;
            AddFileToDfsResponseCode responseCode;

            try
            {
                Guard.Argument(deserialised).NotNull("Message cannot be null");

                Guid correlationId = new Guid(deserialised.CorrelationFileName.ToByteArray());
                responseCode = _fileTransfer.WriteChunk(correlationId.ToString(), deserialised.ChunkId, deserialised.ChunkBytes.ToByteArray(), out fileTransferInformation);
            } catch(Exception e)
            {
                Logger.Error(e.ToString());
                responseCode = AddFileToDfsResponseCode.Error;
            }

            TransferFileBytesResponse responseMessage = new TransferFileBytesResponse();
            responseMessage.ResponseCode = ByteString.CopyFrom((byte)responseCode);

            var responseDto = _rpcMessageFactory.GetMessage(new MessageDto<TransferFileBytesResponse, RpcMessages>(
                type: RpcMessages.TransferFileBytesResponse,
                message: responseMessage,
                recipient: new PeerIdentifier(message.Payload.PeerId),
                sender: _peerIdentifier
            ));
            message.Context.Channel.WriteAndFlushAsync(responseDto);

            if (fileTransferInformation != null && fileTransferInformation.IsComplete())
            {
                fileTransferInformation.Dispose();
                fileTransferInformation.OnSuccess?.Invoke(fileTransferInformation);
                fileTransferInformation.Delete();
            }
        }
    }
}
