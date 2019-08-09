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
using Catalyst.Common.Interfaces.IO.Handlers;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Protocol.Common;
using DotNetty.Transport.Channels;

namespace Catalyst.Common.IO.Handlers
{
    /// <summary>
    ///     This handler terminates DotNetty involvement and passes service messages into rx land,
    ///     by this point all messages should be treated as genuine and sanitised.
    /// </summary>
    public sealed class ObservableServiceHandler : InboundChannelHandlerBase<ProtocolMessage>, IObservableServiceHandler
    {
        public IObservable<IObserverDto<ProtocolMessage>> MessageStream => _messageSubject.AsObservable();

        private readonly ReplaySubject<IObserverDto<ProtocolMessage>> _messageSubject 
            = new ReplaySubject<IObserverDto<ProtocolMessage>>(1);

        /// <summary>
        ///     Reads the channel once accepted and pushed into a stream.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="message"></param>
        protected override void ChannelRead0(IChannelHandlerContext ctx, ProtocolMessage message)
        {
            Logger.Verbose("Received {message}", message);
            var contextAny = new ObserverDto(ctx, message);
            _messageSubject.OnNext(contextAny);
            ctx.FireChannelRead(message);
        }
        
        public override void ExceptionCaught(IChannelHandlerContext context, Exception e)
        {
            Logger.Error(e, "Error in ObservableServiceHandler");
            context.CloseAsync().ContinueWith(_ => _messageSubject.OnError(e));
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _messageSubject?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public override bool IsSharable => true;
    }
}
