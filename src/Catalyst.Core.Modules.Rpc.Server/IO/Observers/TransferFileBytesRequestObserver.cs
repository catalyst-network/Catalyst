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

using Catalyst.Abstractions.FileTransfer;
using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Abstractions.IO.Observers;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Lib.IO.Observers;
using Catalyst.Protocol.Peer;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using Lib.P2P.Protocols;
using MultiFormats;
using Serilog;

namespace Catalyst.Core.Modules.Rpc.Server.IO.Observers
{
    public sealed class TransferFileBytesRequestObserver
        : RequestObserverBase<TransferFileBytesRequest, TransferFileBytesResponse>,
            IRpcRequestObserver
    {
        /// <summary>The download file transfer factory</summary>
        private readonly IDownloadFileTransferFactory _fileTransferFactory;

        /// <summary>Initializes a new instance of the <see cref="TransferFileBytesRequestObserver"/> class.</summary>
        /// <param name="fileTransferFactory">The download transfer factory.</param>
        /// <param name="peerSettings"></param>
        /// <param name="logger">The logger.</param>
        public TransferFileBytesRequestObserver(IDownloadFileTransferFactory fileTransferFactory,
            IPeerSettings peerSettings,
            ILibP2PPeerClient peerClient,
            ILogger logger)
            : base(logger, peerSettings, peerClient)
        {
            _fileTransferFactory = fileTransferFactory;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="transferFileBytesRequest"></param>
        /// <param name="channelHandlerContext"></param>
        /// <param name="senderPeerId"></param>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        protected override TransferFileBytesResponse HandleRequest(TransferFileBytesRequest transferFileBytesRequest,
            IChannelHandlerContext channelHandlerContext,
            MultiAddress senderPeerId,
            ICorrelationId correlationId)
        {
            Guard.Argument(transferFileBytesRequest, nameof(transferFileBytesRequest)).NotNull();
            Guard.Argument(channelHandlerContext, nameof(channelHandlerContext)).NotNull();
            Guard.Argument(senderPeerId, nameof(senderPeerId)).NotNull();
            Logger.Debug("received message of type TransferFileBytesRequest");

            FileTransferResponseCodeTypes responseCodeType = _fileTransferFactory.DownloadChunk(transferFileBytesRequest);

            return new TransferFileBytesResponse
            {
                ResponseCode = ByteString.CopyFrom((byte) responseCodeType.Id)
            };
        }
    }
}
