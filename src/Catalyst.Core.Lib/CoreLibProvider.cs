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
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.FileSystem;
using Catalyst.Abstractions.FileTransfer;
using Catalyst.Abstractions.IO.EventLoop;
using Catalyst.Abstractions.IO.Events;
using Catalyst.Abstractions.IO.Transport.Channels;
using Catalyst.Abstractions.Keystore;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.Discovery;
using Catalyst.Abstractions.P2P.IO.Messaging.Broadcast;
using Catalyst.Abstractions.P2P.IO.Messaging.Correlation;
using Catalyst.Abstractions.P2P.Models;
using Catalyst.Abstractions.Rpc.IO.Messaging.Correlation;
using Catalyst.Abstractions.Util;
using Catalyst.Abstractions.Validators;
using Catalyst.Core.Lib.Cryptography;
using Catalyst.Core.Lib.FileTransfer;
using Catalyst.Core.Lib.IO.EventLoop;
using Catalyst.Core.Lib.IO.Events;
using Catalyst.Core.Lib.P2P;
using Catalyst.Core.Lib.P2P.Discovery;
using Catalyst.Core.Lib.P2P.IO.Messaging.Broadcast;
using Catalyst.Core.Lib.P2P.IO.Messaging.Correlation;
using Catalyst.Core.Lib.P2P.IO.Transport.Channels;
using Catalyst.Core.Lib.P2P.Models;
using Catalyst.Core.Lib.P2P.Repository;
using Catalyst.Core.Lib.P2P.ReputationSystem;
using Catalyst.Core.Lib.Registry;
using Catalyst.Core.Lib.Rpc.IO.Messaging.Correlation;
using Catalyst.Core.Lib.Util;
using Catalyst.Core.Lib.Validators;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Catalyst.Core.Lib
{
    /// <summary>
    ///     Catalyst Core Libraries Module Provider.
    ///     Registers core dependencies in AutoFac container.
    /// </summary>
    public class CoreLibProvider : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // Register IO.EventLoop
            builder.RegisterType<UdpClientEventLoopGroupFactory>().As<IUdpClientEventLoopGroupFactory>().SingleInstance();
            builder.RegisterType<UdpServerEventLoopGroupFactory>().As<IUdpServerEventLoopGroupFactory>().SingleInstance();
            builder.RegisterType<TcpServerEventLoopGroupFactory>().As<ITcpServerEventLoopGroupFactory>().SingleInstance();
            builder.RegisterType<TcpClientEventLoopGroupFactory>().As<ITcpClientEventLoopGroupFactory>();
            builder.RegisterType<EventLoopGroupFactoryConfiguration>().As<IEventLoopGroupFactoryConfiguration>()
               .WithProperty("TcpServerHandlerWorkerThreads", 4)
               .WithProperty("TcpClientHandlerWorkerThreads", 4)
               .WithProperty("UdpServerHandlerWorkerThreads", 8)
               .WithProperty("UdpClientHandlerWorkerThreads", 2);
            
            // Register P2P
            builder.RegisterType<PeerService>().As<IPeerService>().SingleInstance();
            builder.RegisterType<PeerSettings>().As<IPeerSettings>();
            builder.RegisterType<Peer>().As<IPeer>();
            builder.RegisterType<PeerIdValidator>().As<IPeerIdValidator>();
            builder.RegisterType<PeerChallengerResponse>().As<IPeerChallengeResponse>();
            builder.RegisterType<PeerIdentifier>().As<IPeerIdentifier>().SingleInstance();
            builder.RegisterType<PeerClient>().As<IPeerClient>().SingleInstance();
            builder.RegisterType<PeerChallenger>().As<IPeerChallenger>().SingleInstance();

            // Register P2P.Discovery
            builder.RegisterType<HealthChecker>().As<IHealthChecker>();

            // Register P2P.IO.Transport.Channels
            builder.RegisterType<PeerServerChannelFactory>().As<IUdpServerChannelFactory>();
            builder.RegisterType<PeerClientChannelFactory>().As<IUdpClientChannelFactory>();

            //  Register P2P.Messaging.Correlation
            builder.RegisterType<PeerMessageCorrelationManager>().As<IPeerMessageCorrelationManager>().SingleInstance();

            //  Register P2P.Messaging.Broadcast
            builder.RegisterType<BroadcastManager>().As<IBroadcastManager>().SingleInstance();
            
            //  Register P2P.Repository
            builder.RegisterType<PeerRepository>().As<IPeerRepository>().SingleInstance();
            
            //  Register P2P.ReputationSystem
            builder.RegisterType<ReputationManager>().As<IReputationManager>().SingleInstance();
            
            // Register Registry #inception
            builder.RegisterType<KeyRegistry>().As<IKeyRegistry>().SingleInstance();
            builder.RegisterType<PasswordRegistry>().As<IPasswordRegistry>().SingleInstance();
            
            // Register Cryptography
            builder.RegisterType<CryptoContext>().As<ICryptoContext>();
            builder.RegisterType<IsaacRandom>().As<IDeterministicRandom>();
            builder.RegisterType<ConsolePasswordReader>().As<IPasswordReader>().SingleInstance();
            builder.RegisterType<CertificateStore>().As<ICertificateStore>().SingleInstance();
            builder.RegisterType<PasswordManager>().As<IPasswordManager>().SingleInstance();

            // Register FileSystem
            builder.RegisterType<FileSystem.FileSystem>().As<IFileSystem>().SingleInstance();
            
            // Register Rpc.IO.Messaging.Correlation
            builder.RegisterType<RpcMessageCorrelationManager>().As<IRpcMessageCorrelationManager>().SingleInstance();

            // Register Utils
            builder.RegisterType<CancellationTokenProvider>().As<ICancellationTokenProvider>();
            builder.RegisterType<TtlChangeTokenProvider>().As<IChangeTokenProvider>()
               .WithParameter("timeToLiveInMs", 8000);
            
            builder.RegisterType<AddressHelper>().As<IAddressHelper>();
            
            // Register Cache
            builder.RegisterType<MemoryCache>().As<IMemoryCache>().SingleInstance();
            builder.RegisterType<MemoryCacheOptions>().As<IOptions<MemoryCacheOptions>>();
            
            // Register file transfer
            builder.RegisterType<DownloadFileTransferFactory>().As<IDownloadFileTransferFactory>().SingleInstance();
            builder.RegisterType<UploadFileTransferFactory>().As<IUploadFileTransferFactory>().SingleInstance();

            // Transaction validators
            builder.RegisterType<TransactionValidator>().As<ITransactionValidator>().SingleInstance();
            builder.RegisterType<TransactionReceivedEvent>().As<ITransactionReceivedEvent>().SingleInstance();

            base.Load(builder);
        }
    }
}
