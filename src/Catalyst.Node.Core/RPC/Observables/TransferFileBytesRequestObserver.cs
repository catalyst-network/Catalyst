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
using Catalyst.Common.Interfaces.FileTransfer;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.IO.Observables;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Observables;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using Google.Protobuf;
using Serilog;

namespace Catalyst.Node.Core.RPC.Observables
{
    public sealed class TransferFileBytesRequestObserver
        : RequestMessageObserverBase<TransferFileBytesRequest>,
            IRpcRequestMessageObserver
    {
        /// <summary>The download file transfer factory</summary>
        private readonly IDownloadFileTransferFactory _fileTransferFactory;

        /// <summary>Initializes a new instance of the <see cref="TransferFileBytesRequestObserver"/> class.</summary>
        /// <param name="fileTransferFactory">The download transfer factory.</param>
        /// <param name="peerIdentifier">The peer identifier.</param>
        /// <param name="logger">The logger.</param>
        public TransferFileBytesRequestObserver(IDownloadFileTransferFactory fileTransferFactory,
            IPeerIdentifier peerIdentifier,
            ILogger logger)
            : base(logger, peerIdentifier)
        {
            _fileTransferFactory = fileTransferFactory;
        }

        /// <summary>Handles the specified message.</summary>
        /// <param name="messageDto">The message.</param>
        public override IMessage HandleRequest(IProtocolMessageDto<ProtocolMessage> messageDto)
        {
            Logger.Debug("received message of type TransferFileBytesRequest");

            Guard.Argument(messageDto, nameof(messageDto)).NotNull();
            
            var deserialised = messageDto.Payload.FromProtocolMessage<TransferFileBytesRequest>();
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
                    "Failed to handle TransferFileBytesRequestHandler after receiving message {0}", messageDto);
                responseCode = FileTransferResponseCodes.Error;
            }
            
            return new TransferFileBytesResponse
            {
                ResponseCode = ByteString.CopyFrom((byte) responseCode.Id)
            };
        }
    }
}
