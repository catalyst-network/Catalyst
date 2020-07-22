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

using Catalyst.Abstractions.P2P;
using Catalyst.Modules.Network.Dotnetty.Abstractions;
using Catalyst.Modules.Network.Dotnetty.Abstractions.IO.EventLoop;
using Catalyst.Modules.Network.Dotnetty.Abstractions.IO.Transport.Channels;
using Catalyst.Modules.Network.Dotnetty.IO.Messaging.Dto;
using Catalyst.Modules.Network.Dotnetty.IO.Transport;
using Catalyst.Protocol.Wire;
using MultiFormats;
using Serilog;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Catalyst.Modules.Network.Dotnetty
{
    public class DotnettyUdpClient : UdpClient<ProtocolMessage>, IDotnettyUdpClient
    {
        private readonly IPeerSettings _peerSettings;

        public DotnettyUdpClient(IUdpClientChannelFactory<ProtocolMessage> clientChannelFactory,
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
            await StartAsync(CancellationToken.None).ConfigureAwait(false);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var observableChannel = await ChannelFactory.BuildChannelAsync(EventLoopGroupFactory, _peerSettings.Address).ConfigureAwait(false);
            Channel = observableChannel.Channel;
        }

        public Task SendMessageAsync(ProtocolMessage message, MultiAddress recipient)
        {
            SendMessage(new MessageDto(message, recipient));
            return Task.CompletedTask;
        }
    }
}
