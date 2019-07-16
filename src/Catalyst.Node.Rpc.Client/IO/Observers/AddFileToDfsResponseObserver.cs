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
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using Catalyst.Common.Config;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.Interfaces.FileTransfer;
using Catalyst.Common.Interfaces.IO.Messaging.Correlation;
using Catalyst.Common.Interfaces.IO.Observers;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.Rpc.IO.Messaging.Dto;
using Catalyst.Common.IO.Observers;
using Catalyst.Node.Rpc.Client.IO.Messaging.Dto;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using Serilog;

namespace Catalyst.Node.Rpc.Client.IO.Observers
{
    /// <summary>
    /// Add File to DFS Response handler
    /// </summary>
    /// <seealso cref="IRpcResponseObserver" />
    public sealed class AddFileToDfsResponseObserver :
        ResponseObserverBase<AddFileToDfsResponse>,
        IRpcResponseObserver
    {
        private readonly ReplaySubject<IRPCClientMessageDto<IMessage>> _messageResponse;
        public IObservable<IRPCClientMessageDto<IMessage>> MessageResponseStream { private set; get; }

        /// <summary>The upload file transfer factory</summary>
        private readonly IUploadFileTransferFactory _rpcFileTransferFactory;

        private readonly IUserOutput _userOutput;

        /// <summary>Initializes a new instance of the <see cref="AddFileToDfsResponseObserver"/> class.</summary>
        /// <param name="logger">The logger.</param>
        /// <param name="rpcFileTransferFactory">The upload file transfer factory</param>
        /// <param name="userOutput"></param>
        public AddFileToDfsResponseObserver(ILogger logger,
            IUploadFileTransferFactory rpcFileTransferFactory,
            IUserOutput userOutput) : base(logger)
        {
            _userOutput = userOutput;
            _rpcFileTransferFactory = rpcFileTransferFactory;
            _messageResponse = new ReplaySubject<IRPCClientMessageDto<IMessage>>(1);
            MessageResponseStream = _messageResponse.AsObservable();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="addFileToDfsResponse"></param>
        /// <param name="channelHandlerContext"></param>
        /// <param name="senderPeerIdentifier"></param>
        /// <param name="correlationId"></param>
        protected override void HandleResponse(AddFileToDfsResponse addFileToDfsResponse,
            IChannelHandlerContext channelHandlerContext,
            IPeerIdentifier senderPeerIdentifier,
            ICorrelationId correlationId)
        {
            Guard.Argument(addFileToDfsResponse, nameof(addFileToDfsResponse)).NotNull();
            Guard.Argument(channelHandlerContext, nameof(channelHandlerContext)).NotNull();
            Guard.Argument(senderPeerIdentifier, nameof(senderPeerIdentifier)).NotNull();

            // @TODO return int not byte
            // var responseCode = Enumeration.Parse<FileTransferResponseCodes>(deserialised.ResponseCode[0].ToString());

            var responseCode = (FileTransferResponseCodes)addFileToDfsResponse.ResponseCode[0];

            if (responseCode == FileTransferResponseCodes.Failed || responseCode == FileTransferResponseCodes.Finished)
            {
                _userOutput.WriteLine("File transfer completed, Response: " + responseCode.Name + " Dfs Hash: " + addFileToDfsResponse.DfsHash);
            }
            else
            {
                if (responseCode == FileTransferResponseCodes.Successful)
                {
                    _rpcFileTransferFactory.FileTransferAsync(correlationId, CancellationToken.None)
                       .ConfigureAwait(false);
                }
                else
                {
                    _rpcFileTransferFactory.Remove(correlationId);
                }
            }

            _messageResponse.OnNext(new RPCClientMessageDto<IMessage>(addFileToDfsResponse, senderPeerIdentifier));
        }
    }
}
