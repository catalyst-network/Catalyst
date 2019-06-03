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

using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Outbound;
using Catalyst.Protocol.Common;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace Catalyst.Node.Core.P2P
{
    public sealed class PeerClient
        : UdpClient,
            IPeerClient
    {
        public IObservable<IChanneledMessage<AnySigned>> MessageStream { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ipEndPoint"></param>
        public PeerClient(IPeerSettings peerSettings)
            : base(Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType))
        {
            Logger.Debug("P2P client starting");
            IPEndPoint ipEndPoint = new IPEndPoint(peerSettings.BindAddress, peerSettings.Port + 10);
            Bootstrap(new OutboundChannelInitializerBase<IChannel>(channel => { },
                new List<IChannelHandler>(),
                ipEndPoint.Address
            ), ipEndPoint);
        }

        public async Task SendMessage(IByteBufferHolder datagramPacket)
        {
            await Channel.WriteAndFlushAsync(datagramPacket).ConfigureAwait(false);
        }
    }
}
