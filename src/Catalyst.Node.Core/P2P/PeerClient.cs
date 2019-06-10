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

using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Catalyst.Common.Interfaces.IO.Outbound;
using Catalyst.Common.IO.Outbound;
using Catalyst.Common.Interfaces.P2P;
using DotNetty.Buffers;
using Serilog;

namespace Catalyst.Node.Core.P2P
{
    public sealed class PeerClient : UdpClient, IPeerClient
    {
        /// <param name="clientChannelFactory">A factory used to build the appropriate kind of channel for a udp client.</param>
        public PeerClient(IUdpClientChannelFactory clientChannelFactory)
            : base(clientChannelFactory, Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType))
        {
            var bindingEndpoint = new IPEndPoint(IPAddress.Loopback, IPEndPoint.MinPort);
            Channel = ChannelFactory.BuildChannel(bindingEndpoint.Address, bindingEndpoint.Port).Channel;
        }

        public Task SendMessageAsync(IByteBufferHolder datagramPacket)
        {
            return Channel.WriteAndFlushAsync(datagramPacket);
        }
    }
}
