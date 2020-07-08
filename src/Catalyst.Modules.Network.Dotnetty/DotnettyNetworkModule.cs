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

using Autofac;
using Catalyst.Abstractions.IO.EventLoop;
using Catalyst.Abstractions.IO.Transport.Channels;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.IO.Messaging.Broadcast;
using Catalyst.Core.Lib.IO.EventLoop;
using Catalyst.Core.Lib.P2P;
using Catalyst.Core.Lib.P2P.IO.Messaging.Broadcast;
using Catalyst.Core.Lib.P2P.IO.Transport.Channels;

namespace Catalyst.Modules.Network.Dotnetty
{
    public class DotnettyNetworkModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // Register IO.EventLoop
            builder.RegisterType<UdpClientEventLoopGroupFactory>().As<IUdpClientEventLoopGroupFactory>()
               .SingleInstance();
            builder.RegisterType<UdpServerEventLoopGroupFactory>().As<IUdpServerEventLoopGroupFactory>()
               .SingleInstance();
            builder.RegisterType<TcpServerEventLoopGroupFactory>().As<ITcpServerEventLoopGroupFactory>()
               .SingleInstance();
            builder.RegisterType<TcpClientEventLoopGroupFactory>().As<ITcpClientEventLoopGroupFactory>();
            builder.RegisterType<EventLoopGroupFactoryConfiguration>().As<IEventLoopGroupFactoryConfiguration>()
               .WithProperty("TcpServerHandlerWorkerThreads", 4)
               .WithProperty("TcpClientHandlerWorkerThreads", 4)
               .WithProperty("UdpServerHandlerWorkerThreads", 8)
               .WithProperty("UdpClientHandlerWorkerThreads", 2);

            // Register P2P.IO.Transport.Channels
            builder.RegisterType<PeerServerChannelFactory>().As<IUdpServerChannelFactory>();
            builder.RegisterType<PeerClientChannelFactory>().As<IUdpClientChannelFactory>();

            builder.RegisterType<DotnettyPeerClient>().As<IPeerClient>().SingleInstance();
            builder.RegisterType<DotnettyPeerService>().As<IPeerService>().SingleInstance();

            //  Register P2P.Messaging.Broadcast
            builder.RegisterType<BroadcastManager>().As<IBroadcastManager>().SingleInstance();
        }
    }
}
