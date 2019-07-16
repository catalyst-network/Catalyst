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
using Catalyst.Common.Interfaces.Cli;
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
using ILogger = Serilog.ILogger;

namespace Catalyst.Node.Rpc.Client.IO.Observers
{
    /// <summary>
    /// The response handler for removing a peer
    /// </summary>
    /// <seealso cref="IRpcResponseObserver" />
    public sealed class RemovePeerResponseObserver
        : ResponseObserverBase<RemovePeerResponse>,
            IRpcResponseObserver
    {
        private readonly ReplaySubject<IRPCClientMessageDto<IMessage>> _messageResponse;
        public IObservable<IRPCClientMessageDto<IMessage>> MessageResponseStream { private set; get; }

        private readonly IUserOutput _userOutput;

        public RemovePeerResponseObserver(IUserOutput userOutput,
            ILogger logger) : base(logger)
        {
            _userOutput = userOutput;
            _messageResponse = new ReplaySubject<IRPCClientMessageDto<IMessage>>(1);
            MessageResponseStream = _messageResponse.AsObservable();
        }

        protected override void HandleResponse(RemovePeerResponse removePeerResponse,
            IChannelHandlerContext channelHandlerContext,
            IPeerIdentifier senderPeerIdentifier,
            ICorrelationId correlationId)
        {
            Guard.Argument(removePeerResponse, nameof(removePeerResponse)).NotNull();
            Guard.Argument(channelHandlerContext, nameof(channelHandlerContext)).NotNull();
            Guard.Argument(senderPeerIdentifier, nameof(senderPeerIdentifier)).NotNull();

            _messageResponse.OnNext(new RPCClientMessageDto<IMessage>(removePeerResponse, senderPeerIdentifier));
        }
    }
}
