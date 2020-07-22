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
using Catalyst.Abstractions.IO.Events;
using Catalyst.Abstractions.Keystore;
using Catalyst.Abstractions.Network;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.Discovery;
using Catalyst.Abstractions.P2P.IO.Messaging.Correlation;
using Catalyst.Abstractions.P2P.Models;
using Catalyst.Abstractions.P2P.Protocols;
using Catalyst.Abstractions.Rpc.IO.Messaging.Correlation;
using Catalyst.Abstractions.Util;
using Catalyst.Abstractions.Validators;
using Catalyst.Core.Lib.Cryptography;
using Catalyst.Core.Lib.IO.Events;
using Catalyst.Core.Lib.P2P;
using Catalyst.Core.Lib.P2P.Discovery;
using Catalyst.Core.Lib.P2P.IO.Messaging.Correlation;
using Catalyst.Core.Lib.P2P.Models;
using Catalyst.Core.Lib.P2P.Protocols;
using Catalyst.Core.Lib.P2P.ReputationSystem;
using Catalyst.Core.Lib.Rpc.IO.Messaging.Correlation;
using Catalyst.Core.Lib.Util;
using Catalyst.Core.Lib.Validators;
using Catalyst.Protocol.Transaction;
using DnsClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using LibP2P = Lib.P2P;

// ReSharper disable WrongIndentSize

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
            builder.RegisterType<LocalPeer>().As<LibP2P.Peer>().SingleInstance();
            
            // Register P2P
            builder.RegisterType<LibP2PPeerService>().As<ILibP2PPeerService>().SingleInstance();
            builder.RegisterType<LibP2PPeerClient>().As<ILibP2PPeerClient>().SingleInstance();

            builder.RegisterType<PeerSettings>().As<IPeerSettings>().SingleInstance();

            builder.RegisterType<Peer>().As<IPeer>();

            builder.RegisterType<PeerIdValidator>().As<IPeerIdValidator>();

            builder.RegisterType<PeerChallengeRequest>().As<IPeerChallengeRequest>()
               .WithParameter("ttl", 5)
               .SingleInstance();
            builder.RegisterType<PeerChallengeResponse>().As<IPeerChallengeResponse>();
            
            builder.RegisterType<PeerQueryTipRequestRequest>().As<IPeerQueryTipRequest>();
            builder.RegisterType<PeerQueryTipResponse>().As<IPeerQueryTipResponse>();

            builder.RegisterType<PeerDeltaHistoryRequest>().As<IPeerDeltaHistoryRequest>();
            builder.RegisterType<PeerDeltaHistoryResponse>().As<IPeerDeltaHistoryResponse>();

            // Register P2P.Discovery
            builder.RegisterType<HealthChecker>().As<IHealthChecker>();


            //  Register P2P.Messaging.Correlation
            builder.RegisterType<PeerMessageCorrelationManager>().As<IPeerMessageCorrelationManager>().SingleInstance();

            //  Register P2P.ReputationSystem
            builder.RegisterType<ReputationManager>().As<IReputationManager>().SingleInstance();

            // Register Registry #inception
            builder.RegisterType<KeyRegistry>().As<IKeyRegistry>().SingleInstance();
            builder.RegisterType<PasswordRegistry>().As<IPasswordRegistry>().SingleInstance();

            // Register Cryptography
            builder.RegisterType<IsaacRandom>().As<IDeterministicRandom>();
            builder.RegisterType<ConsolePasswordReader>().As<IPasswordReader>().SingleInstance();
            builder.RegisterType<CertificateStore>().As<ICertificateStore>().SingleInstance();
            builder.RegisterType<PasswordManager>().As<IPasswordManager>().SingleInstance();
            builder.RegisterType<NodePasswordRepeater>().As<IPasswordRepeater>().SingleInstance();

            // Register FileSystem
            builder.RegisterType<FileSystem.FileSystem>().As<IFileSystem>().SingleInstance();
            
            // Register Rpc.IO.Messaging.Correlation
            builder.RegisterType<RpcMessageCorrelationManager>().As<IRpcMessageCorrelationManager>().SingleInstance();

            // Register Utils
            builder.RegisterType<CancellationTokenProvider>().As<ICancellationTokenProvider>()
               .WithParameter("goodTillCancel", true);
            builder.RegisterType<TtlChangeTokenProvider>().As<IChangeTokenProvider>()
               .WithParameter("timeToLiveInMs", 8000);

            // Register Cache
            builder.RegisterType<MemoryCache>().As<IMemoryCache>().SingleInstance();
            builder.RegisterType<MemoryCacheOptions>().As<IOptions<MemoryCacheOptions>>();

            // Transaction validators
            builder.RegisterType<TransactionValidator>().As<ITransactionValidator>().SingleInstance();
            builder.RegisterType<TransactionReceivedEvent>().As<ITransactionReceivedEvent>().SingleInstance();

            // Register PRNG
            builder.RegisterType<IsaacRandomFactory>().As<IDeterministicRandomFactory>();

            // Dns Client
            builder.RegisterType<Network.DnsClient>().As<IDns>();
            builder.RegisterType<LookupClient>().As<ILookupClient>().UsingConstructor();

            base.Load(builder);
        }

        // TODO: rethink validation of transaction signature
        class AlwaysTrueTransactionValidator : ITransactionValidator
        {
            public bool ValidateTransaction(PublicEntry transaction) => true;
        }
    }
}
