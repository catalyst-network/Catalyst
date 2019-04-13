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
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Catalyst.Node.Common.Helpers.IO;
using Catalyst.Node.Common.Helpers.IO.Inbound;
using Catalyst.Node.Common.Helpers.IO.Outbound;
using Catalyst.Node.Common.Interfaces.P2P;
using Catalyst.Node.Core.P2P.Messaging.Handlers;
using Catalyst.Protocol.Common;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using Serilog;

namespace Catalyst.Node.Core.P2P
{
    public sealed class PeerClient : UdpClient, IPeerClient
    {
        private readonly PingResponseHandler _pingRequestHandler;

        public IObservable<IChanneledMessage<AnySigned>> MessageStream { get; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="peerIdentifier"></param>
        /// <param name="ipEndPoint"></param>
        public PeerClient(IPeerIdentifier peerIdentifier, IPEndPoint ipEndPoint)
            : base(Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType))
        {
            Logger.Debug("P2P client starting");

            var protoDatagramChannelHandler = new ProtoDatagramChannelHandler();
            MessageStream = protoDatagramChannelHandler.MessageStream;
            _pingRequestHandler = new PingResponseHandler(protoDatagramChannelHandler.MessageStream, Logger);

            IList<IChannelHandler> channelHandlers = new List<IChannelHandler>
            {
                protoDatagramChannelHandler
            };

            Bootstrap(new OutboundChannelInitializer<IChannel>(channel => { },
                channelHandlers,
                ipEndPoint.Address
            ), ipEndPoint);
        }

        public async Task SendMessage(IByteBufferHolder datagramPacket)
        {
            await Channel.WriteAndFlushAsync(datagramPacket).ConfigureAwait(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Logger.Information("disposing peerClient");
                _pingRequestHandler.Dispose();
            }

            base.Dispose();
        }
    }
}
