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
using Catalyst.Abstractions.IO.EventLoop;
using Catalyst.Abstractions.IO.Transport.Channels;
using Catalyst.Abstractions.P2P;
using Catalyst.Core.IO.Transport;
using Serilog;

namespace Catalyst.Core.P2P
{
    public sealed class PeerClient : UdpClient, IPeerClient
    {
        private readonly IPeerSettings _peerSettings;

        /// <param name="clientChannelFactory">A factory used to build the appropriate kind of channel for a udp client.</param>
        /// <param name="eventLoopGroupFactory"></param>
        /// <param name="peerSettings"></param>
        public PeerClient(IUdpClientChannelFactory clientChannelFactory,
            IUdpClientEventLoopGroupFactory eventLoopGroupFactory,
            IPeerSettings peerSettings)
            : base(clientChannelFactory,
                Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType),
                eventLoopGroupFactory)
        {
            _peerSettings = peerSettings;
        }

        public override async Task StartAsync()
        {
            var bindingEndpoint = new IPEndPoint(_peerSettings.BindAddress, IPEndPoint.MinPort);
            var observableChannel = await ChannelFactory.BuildChannel(EventLoopGroupFactory,
                bindingEndpoint.Address,
                bindingEndpoint.Port);
            Channel = observableChannel.Channel;
        }
    }
}
