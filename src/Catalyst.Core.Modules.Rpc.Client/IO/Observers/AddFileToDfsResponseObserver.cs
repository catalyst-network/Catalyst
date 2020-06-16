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

using System.Threading;
using Catalyst.Abstractions.FileTransfer;
using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Abstractions.IO.Observers;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Lib.Rpc.IO;
using Catalyst.Protocol.Peer;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using DotNetty.Transport.Channels;
using MultiFormats;
using Serilog;

namespace Catalyst.Core.Modules.Rpc.Client.IO.Observers
{
    /// <summary>
    /// Add File to DFS Response handler
    /// </summary>
    /// <seealso cref="IRpcResponseObserver" />
    public sealed class AddFileToDfsResponseObserver : RpcResponseObserver<AddFileToDfsResponse>
    {
        /// <summary>The upload file transfer factory</summary>
        private readonly IUploadFileTransferFactory _rpcFileTransferFactory;

        /// <summary>Initializes a new instance of the <see cref="AddFileToDfsResponseObserver"/> class.</summary>
        /// <param name="logger">The logger.</param>
        /// <param name="rpcFileTransferFactory">The upload file transfer factory</param>
        public AddFileToDfsResponseObserver(ILogger logger,
            IUploadFileTransferFactory rpcFileTransferFactory) : base(logger)
        {
            _rpcFileTransferFactory = rpcFileTransferFactory;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="addFileToDfsResponse"></param>
        /// <param name="channelHandlerContext"></param>
        /// <param name="senderentifier"></param>
        /// <param name="correlationId"></param>
        protected override void HandleResponse(AddFileToDfsResponse addFileToDfsResponse,
            IChannelHandlerContext channelHandlerContext,
            MultiAddress senderentifier,
            ICorrelationId correlationId)
        {
            Guard.Argument(addFileToDfsResponse, nameof(addFileToDfsResponse)).NotNull();
            Guard.Argument(senderentifier, nameof(senderentifier)).NotNull();

            // @TODO return int not byte
            // var responseCode = Enumeration.Parse<FileTransferResponseCodes>(deserialised.ResponseCode[0].ToString());

            var responseCode = (FileTransferResponseCodeTypes) addFileToDfsResponse.ResponseCode[0];
            if (responseCode == FileTransferResponseCodeTypes.Successful)
            {
                _rpcFileTransferFactory.FileTransferAsync(correlationId, CancellationToken.None)
                   .ConfigureAwait(false);
            }
            else
            {
                var fileTransferInformation = _rpcFileTransferFactory.GetFileTransferInformation(correlationId);
                if (fileTransferInformation != null)
                {
                    _rpcFileTransferFactory.Remove(fileTransferInformation, true);
                }
            }
        }
    }
}
