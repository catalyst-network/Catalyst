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

using System.Collections.Generic;
using System.Reactive.Concurrency;
using Autofac;
using Catalyst.Abstractions.Cli;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.IO.EventLoop;
using Catalyst.Abstractions.IO.Observers;
using Catalyst.Abstractions.IO.Transport.Channels;
using Catalyst.Abstractions.Keystore;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.Discovery;
using Serilog;

namespace Catalyst.Core.P2P
{
    public class P2PModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(c => new PeerIdValidator(
                c.Resolve<ICryptoContext>()
            )).As<IPeerIdValidator>();
            
            builder.Register(c => new PeerIdentifier(
                c.Resolve<IPeerSettings>(),
                c.Resolve<IKeyRegistry>(),
                c.Resolve<IUserOutput>()
            )).As<IPeerIdentifier>();
            
            builder.Register(c => new PeerIdentifier(
                c.Resolve<IPeerSettings>()
            )).As<IPeerIdentifier>();
            
            builder.Register(c => new PeerClient(
                c.Resolve<IUdpClientChannelFactory>(),
                c.Resolve<IUdpClientEventLoopGroupFactory>(),
                c.Resolve<IPeerSettings>()
            )).As<IPeerClient>();
            
            builder.RegisterType<PeerChallengerResponse>()
                .As<IPeerChallengeResponse>();
            
            builder.Register(c => new PeerChallenger(
                    c.Resolve<ILogger>(),
                    c.Resolve<IPeerClient>(),
                    c.Resolve<IPeerIdentifier>(),
                    10,
                    c.Resolve<IScheduler>()
                    ))
                .As<IPeerChallenger>();
            
            builder.Register(c => new PeerService(c.Resolve<IUdpServerEventLoopGroupFactory>(),
                    c.Resolve<IUdpServerChannelFactory>(),
                    c.Resolve<IPeerDiscovery>(),
                    c.Resolve<IEnumerable<IP2PMessageObserver>>(),
                    c.Resolve<IPeerSettings>(),
                    c.Resolve<ILogger>(),
                    c.Resolve<IHealthChecker>()
                ))
               .As<IPeerService>();
        }  
    }
}
