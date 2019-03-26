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
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using Catalyst.Node.Common.Interfaces;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Groups;
using Google.Protobuf.WellKnownTypes;
using NSec.Cryptography;

namespace Catalyst.Node.Common.Helpers.IO.Inbound
{
    public class AnyTypeServerBroadcastingHandler : AnyTypeServerHandler
    {
        public static readonly ConcurrentDictionary<string, IChannelHandlerContext> ContextByPeerId 
            = new ConcurrentDictionary<string, IChannelHandlerContext>();

        public override void ChannelActive(IChannelHandlerContext context)
        {
            base.ChannelActive(context);
            //var endpoint = (IPEndPoint)context.Channel.RemoteAddress;
            //var publicKey = new PublicKey(new Ed25519());
            //var peerId = new PeerIdentifier(publicKey.Export(KeyBlobFormat.NSecPublicKey).Take(20).ToArray(), endpoint);
            var key = Guid.NewGuid().ToString();
            ContextByPeerId.TryAdd(key, context);
        }

        protected override void ChannelRead0(IChannelHandlerContext context, Any message)
        {
            base.ChannelRead0(context, message);
        }
    }
}