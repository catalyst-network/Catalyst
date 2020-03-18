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

using System.Reflection;
using Autofac;
using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.Dfs.CoreApi;
using Catalyst.Abstractions.Dfs.Migration;
using Catalyst.Abstractions.Keystore;
using Catalyst.Abstractions.Options;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Lib.Config;
using Catalyst.Core.Lib.Kernel;
using Catalyst.Core.Lib.P2P;
using Catalyst.Core.Modules.Dfs.BlockExchange;
using Catalyst.Core.Modules.Dfs.CoreApi;
using Catalyst.Core.Modules.Dfs.Migration;
using Catalyst.Core.Modules.Keystore;
using Lib.P2P;
using Lib.P2P.Protocols;
using Lib.P2P.PubSub;
using Makaretu.Dns;

namespace Catalyst.Core.Modules.Dfs
{
    public sealed class DfsModule : CatalystModule
    {
        protected override void LoadApi(ContainerBuilder builder)
        {
            builder.RegisterType<BlockApi>().As<IBlockApi>().SingleInstance()
               .OnActivated(e => e.Instance.PinApi = e.Context.Resolve<IPinApi>());

            builder.RegisterType<PinApi>().As<IPinApi>().SingleInstance()
               .OnActivated(e => e.Instance.BlockApi = e.Context.Resolve<IBlockApi>());

            builder.RegisterType<BitSwapApi>().As<IBitSwapApi>().SingleInstance();
            builder.RegisterType<BlockRepositoryApi>().As<IBlockRepositoryApi>().SingleInstance();
            builder.RegisterType<BootstrapApi>().As<IBootstrapApi>().SingleInstance();
            builder.RegisterType<ConfigApi>().As<IConfigApi>().SingleInstance();
            builder.RegisterType<DagApi>().As<IDagApi>().SingleInstance();
            builder.RegisterType<DnsApi>().As<IDnsApi>().SingleInstance();
            builder.RegisterType<UnixFsApi>().As<IUnixFsApi>().SingleInstance();
            builder.RegisterType<KeyApi>().As<IKeyApi>().SingleInstance();
            builder.RegisterType<NameApi>().As<INameApi>().SingleInstance();
            builder.RegisterType<ObjectApi>().As<IObjectApi>().SingleInstance();
            builder.RegisterType<PubSubApi>().As<IPubSubApi>().SingleInstance();
            builder.RegisterType<StatsApi>().As<IStatsApi>().SingleInstance();
            builder.RegisterType<SwarmApi>().As<ISwarmApi>().SingleInstance();
            builder.RegisterType<DhtApi>().As<IDhtApi>().SingleInstance();
            builder.RegisterType<MigrationManager>().As<IMigrationManager>().SingleInstance();
            builder.RegisterType<DeltaDfsReader>().As<IDeltaDfsReader>().SingleInstance();

            builder.RegisterType<DfsState>().As<DfsState>().SingleInstance();
        }

        protected override void LoadService(ContainerBuilder builder)
        {
            builder.RegisterType<DfsService>()
               .As<IDfsService>()
               .SingleInstance();

            builder.RegisterType<BitSwapService>()
               .As<BitSwapService>()
               .SingleInstance();

            builder.RegisterBuildCallback(async x =>
            {
                //Log.Debug("Building bitswap service");
                //Log.Debug("Built bitswap service");

                var localPeer = x.Resolve<Peer>();
                var swarmService = x.Resolve<SwarmService>();

                var bitSwapService = x.Resolve<BitSwapService>();
                bitSwapService.SwarmService = swarmService;
                bitSwapService.BlockService = x.Resolve<IBlockApi>();

                var pubService = x.Resolve<PubSubService>();
                pubService.LocalPeer = localPeer;
                pubService.Routers.Add(new FloodRouter
                {
                    SwarmService = swarmService
                });

                //var keyApi = x.Resolve<IKeyApi>();
                //var swarmOptions = x.Resolve<SwarmOptions>();
                //swarmService.LocalPeer = localPeer;
                //swarmService.LocalPeerKey =
                //    Key.CreatePrivateKey(await keyApi.GetPrivateKeyAsync("self").ConfigureAwait(false));
                //swarmService.NetworkProtector = swarmOptions.PrivateNetworkKey == null
                //    ? null
                //    : new Psk1Protector
                //    {
                //        Key = swarmOptions.PrivateNetworkKey
                //    };

                //if (swarmOptions.PrivateNetworkKey != null)
                //{
                //    Log.Debug($"Private network {swarmOptions.PrivateNetworkKey.Fingerprint().ToHexString()}");
                //}
            });

            builder.Register(x =>
            {
                var keyStoreService = x.Resolve<IKeyStoreService>();
                var keyApi = x.Resolve<IKeyApi>();
                var passwordManager = x.Resolve<IPasswordManager>();

                var passphrase = passwordManager.RetrieveOrPromptAndAddPasswordToRegistry(
                    PasswordRegistryTypes.IpfsPassword,
                    "Please provide your IPFS password");

                keyApi.SetPassphraseAsync(passphrase).ConfigureAwait(false).GetAwaiter().GetResult();

                var self = keyApi.GetPublicKeyAsync("self").ConfigureAwait(false).GetAwaiter().GetResult()
                 ?? keyApi.CreateAsync("self", "rsa", 0).ConfigureAwait(false).GetAwaiter().GetResult();

                var localPeer = new Peer
                {
                    Id = self.Id,
                    PublicKey =
                        keyStoreService.GetPublicKeyAsync("self").ConfigureAwait(false).GetAwaiter().GetResult(),
                    ProtocolVersion = "ipfs/0.1.0"
                };

                var version = typeof(DfsService).GetTypeInfo().Assembly.GetName().Version;
                localPeer.AgentVersion = $"net-ipfs/{version.Major}.{version.Minor}.{version.Revision}";

                return localPeer;
            }).As<Peer>();

            builder.RegisterType<SwarmService>()
               .As<SwarmService>()
               .SingleInstance();

            builder.RegisterType<KatDhtService>()
               .As<KatDhtService>()
               .SingleInstance();

            builder.RegisterType<PubSubService>()
               .As<PubSubService>()
               .SingleInstance();

            builder.RegisterType<Makaretu.Dns.DnsClient>()
               .As<IDnsClient>();

            builder.RegisterType<Ping1>()
               .As<Ping1>();

            builder.RegisterType<MigrationManager>()
               .As<IMigrationManager>();
        }

        protected override void LoadOptions(ContainerBuilder builder)
        {
            builder.RegisterType<DfsOptions>().SingleInstance();
            builder.RegisterType<BlockOptions>().SingleInstance();
            builder.RegisterType<RepositoryOptions>().SingleInstance()
               .WithParameter("dfsDirectory", Constants.DfsDataSubDir);
            builder.RegisterType<DiscoveryOptions>().SingleInstance();
            builder.RegisterType<KeyChainOptions>().SingleInstance().WithProperty("DefaultKeyType", "ed25519");
            builder.RegisterType<SwarmOptions>().SingleInstance();
        }
    }
}
