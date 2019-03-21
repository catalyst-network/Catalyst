﻿/*
* Copyright(c) 2019 Catalyst Network
*
* This file is part of Catalyst.Node<https: //github.com/catalyst-network/Catalyst.Node>
*
* Catalyst.Node is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 2 of the License, or
* (at your option) any later version.
*
* Catalyst.Node is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
* GNU General Public License for more details.
* 
* You should have received a copy of the GNU General Public License
* along with Catalyst.Node.If not, see<https: //www.gnu.org/licenses/>.
*/

using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using Catalyst.Node.Common.Helpers;
using Catalyst.Node.Common.Helpers.Util;
using Catalyst.Protocol.Transaction;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Groups;
using Google.Protobuf.WellKnownTypes;
using Serilog;

namespace Catalyst.Node.Core.P2P.Messaging
{
    internal class AnyTypeServerHandler : SimpleChannelInboundHandler<Any>, IMessageStreamer<Any>, IDisposable
    {
        private static readonly ILogger Logger = Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType);
        private static volatile IChannelGroup _broadCastGroup;
        private readonly object _groupLock = new object();
        private readonly BehaviorSubject<Any> _messageSubject = new BehaviorSubject<Any>(NullObjects.Any);

        public IObservable<Any> MessageStream => _messageSubject.AsObservable();

        public override void ChannelActive(IChannelHandlerContext context)
        {
            base.ChannelActive(context);

            if (_broadCastGroup == null)
            {
                lock (_groupLock)
                {
                    if (_broadCastGroup == null)
                    {
                        _broadCastGroup = new DefaultChannelGroup(context.Executor);
                    }
                }
            }
            context.WriteAndFlushAsync($"Welcome to {System.Net.Dns.GetHostName()} secure chat server!\n");
            _broadCastGroup.Add(context.Channel);
        }

        private class EveryOneBut : IChannelMatcher
        {

            private readonly IChannelId _id;

            public EveryOneBut(IChannelId id)
            {
                _id = id;
            }

            public bool Matches(IChannel channel) => !Equals(channel.Id, _id);
        }

        protected override void ChannelRead0(IChannelHandlerContext context, Any message)
        {
            _broadCastGroup.WriteAndFlushAsync(message, new EveryOneBut(context.Channel.Id));
            context.WriteAndFlushAsync(message);
        }

        public override void ChannelReadComplete(IChannelHandlerContext ctx) => ctx.Flush();

        public override void ExceptionCaught(IChannelHandlerContext ctx, Exception e)
        {
            Logger.Error(e, "Error in P2P server");
            ctx.CloseAsync().ContinueWith(_ => _messageSubject.OnCompleted());
        }

        public override bool IsSharable => true;

        protected virtual void Dispose(bool disposing)
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