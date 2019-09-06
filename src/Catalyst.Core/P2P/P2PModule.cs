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
using Catalyst.Abstractions.KeySigner;
using Catalyst.Abstractions.Network;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.Discovery;
using Catalyst.Abstractions.P2P.IO;
using Catalyst.Abstractions.P2P.IO.Messaging.Broadcast;
using Catalyst.Abstractions.P2P.IO.Messaging.Correlation;
using Catalyst.Abstractions.P2P.Models;
using Catalyst.Abstractions.Util;
using Catalyst.Core.P2P.Discovery;
using Catalyst.Core.P2P.Discovery.Hastings;
using Catalyst.Core.P2P.IO.Messaging.Broadcast;
using Catalyst.Core.P2P.IO.Messaging.Correlation;
using Catalyst.Core.P2P.IO.Transport.Channels;
using Catalyst.Core.P2P.Models;
using Catalyst.Core.P2P.Repository;
using Catalyst.Core.P2P.ReputationSystem;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Serilog;
using SharpRepository.Repository;

namespace Catalyst.Core.P2P
{
    public class P2PModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(c => new HastingsDiscovery(
                c.Resolve<ILogger>(),
                c.Resolve<IPeerRepository>(),
                c.Resolve<IDns>(),
                c.Resolve<IPeerSettings>(),
                c.Resolve<IPeerClient>(),
                c.Resolve<IPeerMessageCorrelationManager>(),
                c.Resolve<ICancellationTokenProvider>(),
                c.Resolve<IEnumerable<IPeerClientObservable>>()
            )).As<IHastingsDiscovery>();
            
            builder.Register(c => new HealthChecker()).As<IHealthChecker>();
            builder.RegisterType<Peer>().As<IPeer>();

            builder.Register(c => new BroadcastManager(
                    c.Resolve<IPeerIdentifier>(),
                    c.Resolve<IPeerRepository>(),
                    c.Resolve<IMemoryCache>(),
                    c.Resolve<IPeerClient>(),
                    c.Resolve<IKeySigner>(),
                    c.Resolve<ILogger>()
                )).As<IBroadcastManager>()
               .SingleInstance();

            builder.Register(c => new PeerMessageCorrelationManager(
                    c.Resolve<IReputationManager>(),
                    c.Resolve<IMemoryCache>(),
                    c.Resolve<ILogger>(),
                    c.Resolve<IChangeTokenProvider>(),
                    c.ResolveOptional<IScheduler>()
                )).As<IPeerMessageCorrelationManager>()
               .SingleInstance();
            
            builder.Register(c => new PeerServerChannelFactory(
                c.Resolve<IPeerMessageCorrelationManager>(),
                c.Resolve<IBroadcastManager>(),
                c.Resolve<IKeySigner>(),
                c.Resolve<IPeerIdValidator>(),
                c.Resolve<ISigningContextProvider>(),
                c.ResolveOptional<IScheduler>()
            )).As<IUdpServerChannelFactory>();
            
            builder.Register(c => new PeerClientChannelFactory(
                c.Resolve<IKeySigner>(),
                c.Resolve<IPeerMessageCorrelationManager>(),
                c.Resolve<IPeerIdValidator>(),
                c.Resolve<ISigningContextProvider>(),
                c.ResolveOptional<IScheduler>()                
            )).As<IUdpClientChannelFactory>();
            
            builder.Register(c => new PeerRepository(
                    c.Resolve<IRepository<Peer, string>>()
                )).As<IPeerRepository>()
               .SingleInstance();
               
            builder.Register(c => new ReputationManager(
                    c.Resolve<IPeerRepository>(),
                    c.Resolve<ILogger>()
                )).As<IReputationManager>()
               .SingleInstance();

            builder.Register(c => new PeerSettings(
                c.Resolve<IConfigurationRoot>()
            )).As<IPeerSettings>();
            
            builder.Register(c => new PeerIdValidator(
                c.Resolve<ICryptoContext>()
            )).As<IPeerIdValidator>();
            
            builder.Register(c => new PeerIdentifier(
                    c.Resolve<IPeerSettings>(),
                    c.Resolve<IKeyRegistry>(),
                    c.Resolve<IUserOutput>()
                )).As<IPeerIdentifier>()
               .SingleInstance();
            
            builder.Register(c => new PeerIdentifier(
                    c.Resolve<IPeerSettings>()
                )).As<IPeerIdentifier>()
               .SingleInstance();

            builder.Register(c => new PeerClient(
                    c.Resolve<IUdpClientChannelFactory>(),
                    c.Resolve<IUdpClientEventLoopGroupFactory>(),
                    c.Resolve<IPeerSettings>()
                )).As<IPeerClient>()
               .SingleInstance();
            
            builder.RegisterType<PeerChallengerResponse>()
               .As<IPeerChallengeResponse>();
            
            builder.Register(c => new PeerChallenger(
                    c.Resolve<ILogger>(),
                    c.Resolve<IPeerClient>(),
                    c.Resolve<IPeerIdentifier>(),
                    12,
                    c.ResolveOptional<IScheduler>()
                ))
               .As<IPeerChallenger>()
               .SingleInstance();
            
            builder.Register(c => new PeerService(c.Resolve<IUdpServerEventLoopGroupFactory>(),
                    c.Resolve<IUdpServerChannelFactory>(),
                    c.Resolve<IPeerDiscovery>(),
                    c.Resolve<IEnumerable<IP2PMessageObserver>>(),
                    c.Resolve<IPeerSettings>(),
                    c.Resolve<ILogger>(),
                    c.Resolve<IHealthChecker>()
                ))
               .As<IPeerService>();
            
            base.Load(builder);
        }  
    }
}
