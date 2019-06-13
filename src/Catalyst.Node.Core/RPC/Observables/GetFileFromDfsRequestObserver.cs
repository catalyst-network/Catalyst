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
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Common.Config;
using Catalyst.Common.Enumerator;
using Catalyst.Common.Extensions;
using Catalyst.Common.FileTransfer;
using Catalyst.Common.Interfaces.FileTransfer;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.IO.Observables;
using Catalyst.Common.Interfaces.Modules.Dfs;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Common.IO.Observables;
using Catalyst.Common.P2P;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using Serilog;

namespace Catalyst.Node.Core.RPC.Observables
{
    /// <summary>
    /// The request handler to get a file from the DFS
    /// </summary>
    /// <seealso cref="IRpcRequestMessageObserver" />
    public sealed class GetFileFromDfsRequestObserver 
        : RequestMessageObserverBase<GetFileFromDfsRequest>,
            IRpcRequestMessageObserver
    {
        /// <summary>The RPC message factory</summary>
        private readonly IProtocolMessageFactory _protocolMessageFactory;

        /// <summary>The upload file transfer factory</summary>
        private readonly IUploadFileTransferFactory _fileTransferFactory;

        /// <summary>The DFS</summary>
        private readonly IDfs _dfs;

        /// <summary>Initializes a new instance of the <see cref="AddFileToDfsRequestObserver"/> class.</summary>
        /// <param name="dfs">The DFS.</param>
        /// <param name="peerIdentifier">The peer identifier.</param>
        /// <param name="fileTransferFactory">The upload file transfer factory.</param>
        /// <param name="protocolMessageFactory"></param>
        /// <param name="logger">The logger.</param>
        public GetFileFromDfsRequestObserver(IDfs dfs,
            IPeerIdentifier peerIdentifier,
            IUploadFileTransferFactory fileTransferFactory,
            IProtocolMessageFactory protocolMessageFactory,
            ILogger logger) : base(logger, peerIdentifier)
        {
            _protocolMessageFactory = protocolMessageFactory;
            _fileTransferFactory = fileTransferFactory;
            _dfs = dfs;
        }

        /// <summary>Handles the specified message.</summary>
        /// <param name="messageDto">The message.</param>
        public override IMessage HandleRequest(IProtocolMessageDto<ProtocolMessage> messageDto)
        {
            Guard.Argument(messageDto, nameof(messageDto)).NotNull("Message cannot be null");

            var deserialised = messageDto.Payload.FromProtocolMessage<GetFileFromDfsRequest>();
            
            var recipientPeerIdentifier = new PeerIdentifier(messageDto.Payload.PeerId);
            var correlationGuid = messageDto.Payload.CorrelationId.ToGuid();
            long fileLen = 0;

            // FileTransferResponseCodes responseCode;
            MemoryStream ms = null;

            FileTransferResponseCodes responseCode;

            var task = Task.Run(async () =>
            {
                try
                {
                    responseCode = await Task.Run(async () =>
                    {
                        using (var stream = await _dfs.ReadAsync(deserialised.DfsHash).ConfigureAwait(false))
                        {
                            ms = new MemoryStream();
                            stream.CopyTo(ms);
                            fileLen = stream.Length;

                            using (var fileTransferInformation = new UploadFileTransferInformation(
                                ms,
                                PeerIdentifier,
                                recipientPeerIdentifier,
                                messageDto.Context.Channel,
                                correlationGuid,
                                _protocolMessageFactory
                            ))
                            {
                                return _fileTransferFactory.RegisterTransfer(fileTransferInformation);                          
                            }
                        }
                    }).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Logger.Error(e,
                        "Failed to handle GetFileFromDfsRequestHandler after receiving message {0}", messageDto);
                    responseCode = FileTransferResponseCodes.Error;
                }
                
                if (responseCode == FileTransferResponseCodes.Successful)
                {
                    await _fileTransferFactory.FileTransferAsync(correlationGuid, CancellationToken.None).ConfigureAwait(false);
                }
                else
                {
                    ms?.Dispose();
                }
                
                return ReturnResponse(recipientPeerIdentifier, messageDto.Context.Channel, responseCode, correlationGuid, fileLen);
            });

            return task.Result;
        }

        /// <summary>Returns the response.</summary>
        /// <param name="recipientIdentifier">The recipient identifier.</param>
        /// <param name="recipientChannel">The recipient channel.</param>
        /// <param name="responseCode">The response code.</param>
        /// <param name="correlationGuid">The correlation unique identifier.</param>
        /// <param name="fileSize">Size of the file.</param>
        private IMessage ReturnResponse(IPeerIdentifier recipientIdentifier, IChannel recipientChannel, Enumeration responseCode, Guid correlationGuid, long fileSize)
        {
            Logger.Information("File upload response code: " + responseCode);
            
            // Build Response
            var response = new GetFileFromDfsResponse
            {
                ResponseCode = ByteString.CopyFrom((byte) responseCode.Id),
                FileSize = (ulong) fileSize
            };

            return response as IMessage;

            // // Send Response
            // var responseMessage = _protocolMessageFactory.GetMessage(new MessageDto(
            //         response,
            //         MessageTypes.Response,
            //         recipientIdentifier,
            //         PeerIdentifier
            //     ),
            //     correlationGuid
            // );
            //
            // recipientChannel.WriteAndFlushAsync(responseMessage).GetAwaiter().GetResult();
        }
    }
}
