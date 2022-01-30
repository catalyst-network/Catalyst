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

using System;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.Enumerator;
using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Modules.Network.Dotnetty.Abstractions.FileTransfer;
using Catalyst.Modules.Network.Dotnetty.Abstractions.IO.Messaging.Dto;
using Catalyst.Modules.Network.Dotnetty.FileTransfer;
using Catalyst.Modules.Network.Dotnetty.IO.Observers;
using Catalyst.Modules.Network.Dotnetty.Rpc.IO.Observers;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.Protocol.Wire;
using Dawn;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using Lib.P2P;
using MultiFormats;
using Serilog;

namespace Catalyst.Core.Modules.Rpc.Server.IO.Observers
{
    /// <summary>
    ///     The request handler to get a file from the DFS
    /// </summary>
    /// <seealso cref="IRpcRequestObserver" />
    public sealed class GetFileFromDfsRequestObserver
        : RpcRequestObserverBase<GetFileFromDfsRequest, GetFileFromDfsResponse>,
            IRpcRequestObserver
    {
        /// <summary>The upload file transfer factory</summary>
        private readonly IUploadFileTransferFactory _fileTransferFactory;

        /// <summary>The DFS</summary>
        private readonly IDfsService _dfsService;

        /// <summary>Initializes a new instance of the <see cref="AddFileToDfsRequestObserver" /> class.</summary>
        /// <param name="dfsService">The DFS.</param>
        /// <param name="peerSettings"></param>
        /// <param name="fileTransferFactory">The upload file transfer factory.</param>
        /// <param name="logger">The logger.</param>
        public GetFileFromDfsRequestObserver(IDfsService dfsService,
            IPeerSettings peerSettings,
            IUploadFileTransferFactory fileTransferFactory,
            ILogger logger) : base(logger, peerSettings)
        {
            _fileTransferFactory = fileTransferFactory;
            _dfsService = dfsService;
        }

        /// <summary>
        /// </summary>
        /// <param name="getFileFromDfsRequest"></param>
        /// <param name="channelHandlerContext"></param>
        /// <param name="senderPeerId"></param>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        protected override GetFileFromDfsResponse HandleRequest(GetFileFromDfsRequest getFileFromDfsRequest,
            IChannelHandlerContext channelHandlerContext,
            MultiAddress senderAddress,
            ICorrelationId correlationId)
        {
            Guard.Argument(getFileFromDfsRequest, nameof(getFileFromDfsRequest)).NotNull();
            Guard.Argument(channelHandlerContext, nameof(channelHandlerContext)).NotNull();
            Guard.Argument(senderAddress, nameof(senderAddress)).NotNull();

            long fileLen = 0;

            FileTransferResponseCodeTypes responseCodeType;

            var response = Task.Run(async () =>
            {
                try
                {
                    responseCodeType = await Task.Run(async () =>
                    {
                        var stream = await _dfsService.UnixFsApi.ReadFileAsync(Cid.Decode(getFileFromDfsRequest.DfsHash))
                           .ConfigureAwait(false);
                        fileLen = stream.Length;
                        using (var fileTransferInformation = new UploadFileTransferInformation(
                            stream,
                            senderAddress,
                            PeerSettings.Address,
                            channelHandlerContext.Channel,
                            correlationId
                        ))
                        {
                            return _fileTransferFactory.RegisterTransfer(fileTransferInformation);
                        }
                    }).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Logger.Error(e,
                        "Failed to handle GetFileFromDfsRequestHandler after receiving message {0}",
                        getFileFromDfsRequest);
                    responseCodeType = FileTransferResponseCodeTypes.Error;
                }

                return ReturnResponse(responseCodeType, fileLen);
            }).ConfigureAwait(false).GetAwaiter().GetResult();

            return response;
        }

        public override void OnNext(IObserverDto<ProtocolMessage> messageDto)
        {
            base.OnNext(messageDto);

            var correlationId = messageDto.Payload.CorrelationId.ToCorrelationId();
            if (_fileTransferFactory.GetFileTransferInformation(correlationId) != null)
            {
                _fileTransferFactory.FileTransferAsync(correlationId, CancellationToken.None).ConfigureAwait(false);
            }
        }

        /// <summary>Returns the response.</summary>
        /// <param name="responseCode">The response code.</param>
        /// <param name="fileSize">Size of the file.</param>
        private GetFileFromDfsResponse ReturnResponse(IEnumeration responseCode, long fileSize)
        {
            Logger.Information("File upload response code: " + responseCode);

            // Build Response
            var response = new GetFileFromDfsResponse
            {
                ResponseCode = ByteString.CopyFrom((byte) responseCode.Id),
                FileSize = (ulong) fileSize
            };

            return response;
        }
    }
}
