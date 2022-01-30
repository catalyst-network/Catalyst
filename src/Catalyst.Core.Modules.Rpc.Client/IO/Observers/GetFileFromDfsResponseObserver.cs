#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Lib.Rpc.IO;
using Catalyst.Modules.Network.Dotnetty.Abstractions.FileTransfer;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using DotNetty.Transport.Channels;
using MultiFormats;
using Serilog;

namespace Catalyst.Core.Modules.Rpc.Client.IO.Observers
{
    /// <summary>
    /// Handles Get file from DFS response
    /// </summary>
    /// <seealso cref="IRpcResponseObserver" />
    public sealed class GetFileFromDfsResponseObserver :
        RpcResponseObserver<GetFileFromDfsResponse>
    {
        /// <summary>The file transfer factory</summary>
        private readonly IDownloadFileTransferFactory _fileTransferFactory;

        /// <summary>Initializes a new instance of the <see cref="GetFileFromDfsResponseObserver"/> class.</summary>
        /// <param name="logger">The logger.</param>
        /// <param name="fileTransferFactory">The file transfer.</param>
        public GetFileFromDfsResponseObserver(ILogger logger,
            IDownloadFileTransferFactory fileTransferFactory) : base(logger)
        {
            _fileTransferFactory = fileTransferFactory;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="getFileFromDfsResponse"></param>
        /// <param name="channelHandlerContext"></param>
        /// <param name="senderentifier"></param>
        /// <param name="correlationId"></param>
        protected override void HandleResponse(GetFileFromDfsResponse getFileFromDfsResponse,
            IChannelHandlerContext channelHandlerContext,
            MultiAddress senderAddress,
            ICorrelationId correlationId)
        {
            Guard.Argument(getFileFromDfsResponse, nameof(getFileFromDfsResponse)).NotNull();
            Guard.Argument(channelHandlerContext, nameof(channelHandlerContext)).NotNull();
            Guard.Argument(senderAddress, nameof(senderAddress)).NotNull();
            
            var responseCode = (FileTransferResponseCodeTypes) getFileFromDfsResponse.ResponseCode[0];

            var fileTransferInformation = _fileTransferFactory.GetFileTransferInformation(correlationId);

            if (fileTransferInformation == null)
            {
                return;
            }

            if (responseCode == FileTransferResponseCodeTypes.Successful)
            {
                fileTransferInformation.SetLength(getFileFromDfsResponse.FileSize);

                CancellationTokenSource ctx = new();
                _fileTransferFactory.FileTransferAsync(fileTransferInformation.CorrelationId, CancellationToken.None)?.ContinueWith(task =>
                {
                    File.Move(fileTransferInformation.TempPath, fileTransferInformation.FileOutputPath);
                }, ctx.Token, TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
            }
            else
            {
                fileTransferInformation.Expire();
            }
        }
    }
}
