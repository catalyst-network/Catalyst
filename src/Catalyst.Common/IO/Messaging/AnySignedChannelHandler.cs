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
using System.Reflection;
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.IO.Inbound;
using Catalyst.Common.IO.Messaging.Handlers;
using Catalyst.Protocol.Common;
using Dawn;
using DotNetty.Transport.Channels;
using Serilog;

namespace Catalyst.Common.IO.Messaging
{
    public sealed class AnySignedChannelHandler : ObservableHandlerBase<IChanneledMessage<AnySigned>>
    {
        private static readonly ILogger Logger = Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType);

        protected override void ChannelRead0(IChannelHandlerContext context, IChanneledMessage<AnySigned> packet)
        {
            Guard.Argument(context).NotNull();

            MessageSubject.OnNext(packet);
            context.FireChannelRead(new ChanneledAnySigned(context, packet.Payload));
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception e)
        {
            Logger.Error(e, "Error in AnySignedChannelHandler");
            context.CloseAsync().ContinueWith(_ => MessageSubject.OnCompleted());
        }
    }
}
