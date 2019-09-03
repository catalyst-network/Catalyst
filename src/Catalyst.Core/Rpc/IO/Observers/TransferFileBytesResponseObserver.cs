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
using Catalyst.Abstractions.P2P;
using Catalyst.Protocol.Rpc.Node;
using DotNetty.Transport.Channels;
using Serilog;

namespace Catalyst.Core.Rpc.IO.Observers
{
    public sealed class TransferFileBytesResponseObserver
        : RpcResponseObserver<TransferFileBytesRequest>
    {
        /// <summary>The download file transfer factory</summary>
        private readonly IDownloadFileTransferFactory _fileTransferFactory;

        /// <summary>Initializes a new instance of the <see cref="TransferFileBytesResponseObserver"/> class.</summary>
        /// <param name="fileTransferFactory">The download transfer factory.</param>
        /// <param name="logger">The logger.</param>
        public TransferFileBytesResponseObserver(IDownloadFileTransferFactory fileTransferFactory,
            ILogger logger)
            : base(logger, false)
        {
            _fileTransferFactory = fileTransferFactory;
        }

        protected override void HandleResponse(TransferFileBytesRequest message, IChannelHandlerContext channelHandlerContext, IPeerIdentifier senderPeerIdentifier, ICorrelationId correlationId)
        {
            _ = _fileTransferFactory.DownloadChunk(message);
        }
    }
}
