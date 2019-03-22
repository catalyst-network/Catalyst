/*
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
using Catalyst.Node.Common.Interfaces.P2P;
using Catalyst.Protocol.Transaction;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Groups;
using Google.Protobuf.WellKnownTypes;
using Serilog;

namespace Catalyst.Node.Common.Helpers.IO.Inbound
{
    public class AnyTypeServerBroadcastingHandler : AnyTypeServerHandlerBase
    {
        private static volatile IChannelGroup _broadCastGroup;
        private readonly object _groupLock = new object();
        public override void ChannelActive(IChannelHandlerContext context)
        {
            base.ChannelActive(context);

            if (_broadCastGroup == null)
            {
                lock (_groupLock)
                {
                    if (_broadCastGroup == null)
                    {
                        _broadCastGroup = new DefaultChannelGroup(Guid.NewGuid().ToString(), context.Executor);
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
    }
}