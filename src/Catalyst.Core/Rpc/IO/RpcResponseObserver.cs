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

using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Abstractions.IO.Observers;
using Catalyst.Abstractions.P2P;
using Catalyst.Core.IO.Observers;
using Dawn;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using Serilog;

namespace Catalyst.Core.Rpc.IO
{
    public abstract class RpcResponseObserver<TProto> : ResponseObserverBase<TProto>, IRpcResponseObserver
        where TProto : IMessage<TProto>
    {
        protected RpcResponseObserver(ILogger logger, bool assertMessageNameCheck = true) : base(logger,
            assertMessageNameCheck) { }

        protected abstract override void HandleResponse(TProto messageDto, IChannelHandlerContext channelHandlerContext, IPeerIdentifier senderPeerIdentifier, ICorrelationId correlationId);

        public void HandleResponseObserver(IMessage message,
            IChannelHandlerContext channelHandlerContext,
            IPeerIdentifier senderPeerIdentifier,
            ICorrelationId correlationId)
        {
            Guard.Argument(channelHandlerContext, nameof(channelHandlerContext)).NotNull();
            Guard.Argument(senderPeerIdentifier, nameof(senderPeerIdentifier)).NotNull();
            Guard.Argument(message, nameof(message)).NotNull("The message cannot be null");

            HandleResponse((TProto) message, channelHandlerContext, senderPeerIdentifier, correlationId);
        }
    }
}
