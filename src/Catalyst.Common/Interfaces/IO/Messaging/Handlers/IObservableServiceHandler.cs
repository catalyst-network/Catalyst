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
using System.Net;
using System.Threading.Tasks;
using Catalyst.Protocol.Common;
using DotNetty.Transport.Channels;

namespace Catalyst.Common.Interfaces.IO.Messaging.Handlers
{
    public interface IObservableServiceHandler : IChannelHandler, IChanneledMessageStreamer<AnySigned>, IDisposable
    {
        bool IsSharable { get; }

        /// <summary>
        ///     Reads the channel once accepted and pushed into a stream.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="msg"></param>
        void ChannelRead(IChannelHandlerContext ctx, object msg);

        void ChannelReadComplete(IChannelHandlerContext ctx);
        void ExceptionCaught(IChannelHandlerContext context, Exception e);
        void Dispose();
        void ChannelRegistered(IChannelHandlerContext context);
        void ChannelUnregistered(IChannelHandlerContext context);
        void ChannelActive(IChannelHandlerContext context);
        void ChannelInactive(IChannelHandlerContext context);
        void HandlerAdded(IChannelHandlerContext context);
        void HandlerRemoved(IChannelHandlerContext context);
        void UserEventTriggered(IChannelHandlerContext context, object evt);
        Task WriteAsync(IChannelHandlerContext context, object message);
        void Flush(IChannelHandlerContext context);
        Task BindAsync(IChannelHandlerContext context, EndPoint localAddress);
        Task ConnectAsync(IChannelHandlerContext context, EndPoint remoteAddress, EndPoint localAddress);
        Task DisconnectAsync(IChannelHandlerContext context);
        Task CloseAsync(IChannelHandlerContext context);
        Task DeregisterAsync(IChannelHandlerContext context);
        void Read(IChannelHandlerContext context);
    }
}
