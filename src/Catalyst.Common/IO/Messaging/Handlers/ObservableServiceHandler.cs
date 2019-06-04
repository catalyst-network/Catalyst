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
using Catalyst.Common.Util;
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.IO.Messaging.Handlers;
using Catalyst.Common.IO.Inbound;
using Catalyst.Protocol.Common;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;
using Serilog;

namespace Catalyst.Common.IO.Messaging.Handlers
{
    /// <summary>
    ///     This handler terminates dotnetty involvement and passes service messages into rx land,
    ///     by this point all messages should be treated as genuine and sanitised.
    /// </summary>
    public sealed class ObservableServiceHandler : ChannelHandlerAdapter, IObservableServiceHandler
    {
        private readonly ILogger _logger;
        public IObservable<IChanneledMessage<AnySigned>> MessageStream => _messageSubject.AsObservable();

        private readonly BehaviorSubject<IChanneledMessage<AnySigned>> _messageSubject 
            = new BehaviorSubject<IChanneledMessage<AnySigned>>(NullObjects.ChanneledAnySigned);

        private readonly bool _autoRelease;
        public override bool IsSharable => true;

        public ObservableServiceHandler(ILogger logger)
            : this(true)
        {
            _logger = logger;
        }

        private ObservableServiceHandler(bool autoRelease)
        {
            _autoRelease = autoRelease;
        }

        private bool AcceptInboundMessage(object msg)
        {
            //@TODO hook now into keySigner
            return msg is AnySigned;
        }

        /// <summary>
        ///     Reads the channel once accepted and pushed into a stream.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="msg"></param>
        public override void ChannelRead(IChannelHandlerContext ctx, object msg)
        {
            var flag = true;
            try
            {
                if (AcceptInboundMessage(msg))
                {
                    var contextAny = new ChanneledAnySigned(ctx, (AnySigned) msg);
                    _messageSubject.OnNext(contextAny);
                }
                else
                {
                    // can't accept it so shut it down.
                    flag = false;
                    ctx.CloseAsync();
                }
            }
            finally
            {
                if (_autoRelease & flag)
                {
                    ReferenceCountUtil.Release(msg);
                }
            }
        }
        
        public override void ChannelReadComplete(IChannelHandlerContext ctx)
        {
            ctx.Flush();
            ctx.FireChannelReadComplete();
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception e)
        {
            _logger.Error(e, "Error in ProtoDatagramChannelHandler");
            context.CloseAsync().ContinueWith(_ => _messageSubject.OnCompleted());
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
    }
}
