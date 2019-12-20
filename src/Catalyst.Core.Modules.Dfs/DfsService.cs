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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.Dfs.BlockExchange;
using Catalyst.Abstractions.Dfs.CoreApi;
using Catalyst.Abstractions.Dfs.Migration;
using Catalyst.Abstractions.FileSystem;
using Catalyst.Abstractions.Hashing;
using Catalyst.Abstractions.Keystore;
using Catalyst.Abstractions.Options;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Lib.Config;
using Catalyst.Core.Lib.P2P;
using Catalyst.Core.Modules.Dfs.BlockExchange;
using Catalyst.Core.Modules.Dfs.Migration;
using Catalyst.Core.Modules.Keystore;
using Common.Logging;
using Common.Logging.Serilog;
using Lib.P2P;
using Lib.P2P.Cryptography;
using Lib.P2P.Discovery;
using Lib.P2P.Protocols;
using Lib.P2P.PubSub;
using Lib.P2P.Routing;
using Lib.P2P.SecureCommunication;
using Makaretu.Dns;
using MultiFormats;
using Nito.AsyncEx;
using Serilog;

namespace Catalyst.Core.Modules.Dfs
{
    public class DfsService : IDfsService
    {
        static DfsService() { LogManager.Adapter = new SerilogFactoryAdapter(Log.Logger); }

        // (IPasswordManager passwordReader,
        //             IFileSystem fileSystem,
        //             ILogger logger,
        //             string swarmKey = "07a8e9d0c43400927ab274b7fa443596b71e609bacae47bd958e5cd9f59d6ca3",
        //             IEnumerable<MultiAddress> seedServers = null)
        //         {
        //             if (seedServers == null || seedServers.Count() == 0)
        //             {
        //                 seedServers = new[]
        //                 {
        //                     new MultiAddress("/ip4/46.101.132.61/tcp/4001/ipfs/18n3naE9kBZoVvgYMV6saMZdtAkDHgs8MDwwhtyLu8JpYitY4Nk8jmyGgQ4Gt3VKNson"),
        //                     new MultiAddress("/ip4/188.166.13.135/tcp/4001/ipfs/18n3naE9kBZoVvgYMV6saMZe2AAPTCoujCxhJHECaySDEsPrEz9W2u7uo6hAbJhYzhPg"),
        //                     new MultiAddress("/ip4/167.172.73.132/tcp/4001/ipfs/18n3naE9kBZoVvgYMV6saMZe1E9wXdykR6h3Q9EaQcQc6hdNAXyCTEzoGfcA2wQgCRyg")
        //                 };
        //             }
        //             
        //             _logger = logger;
        //
        //             // The password is used to access the private keys.
        //             var password = passwordReader.RetrieveOrPromptAndAddPasswordToRegistry(PasswordRegistryTypes.IpfsPassword, "Please provide your IPFS password");
        //             _ipfs = new Dfs();
        //             _ipfs.Options.KeyChain.DefaultKeyType = Constants.KeyChainDefaultKeyType;
        //
        //             // The IPFS repository is inside the catalyst home folder.
        //             _ipfs.Options.Repository.Folder = Path.Combine(
        //                 fileSystem.GetCatalystDataDir().FullName,
        //                 Constants.DfsDataSubDir);
        //
        //             // The seed nodes for the catalyst network.
        //             _ipfs.Options.Discovery.BootstrapPeers = seedServers;
        //
        //             // Do not use the public IPFS network, use a private network
        //             // of catalyst only nodes.
        //             _ipfs.Options.Swarm.PrivateNetworkKey = new PreSharedKey
        //             {
        //                 Value = swarmKey.ToHexBuffer()
        //             };
        //
        //             _logger.Information("IPFS configured.");
        //         }

        /// <summary>
        ///     Determines if the engine has started.
        /// </summary>
        /// <value>
        ///     <b>true</b> if the engine has started; otherwise, <b>false</b>.
        /// </value>
        /// <seealso cref="Start" />
        /// <seealso cref="StartAsync" />
        public bool IsStarted => _stopTasks.Count > 0;

        private readonly DfsState _dfsState;
        private readonly IFileSystem _fileSystem;
        private readonly SecureString passphrase;
        private readonly IHashProvider _hashProvider;
        private ConcurrentBag<Func<Task>> _stopTasks = new ConcurrentBag<Func<Task>>();

        public DfsService(IBitSwapApi bitSwapApi,
            BitswapService bitSwapService,
            IBlockApi blockApi,
            IBlockRepositoryApi blockRepositoryApi,
            IBootstrapApi bootstrapApi,
            IConfigApi configApi,
            IDagApi dagApi,
            IDhtApi dhtApi,
            IDnsApi dnsApi,
            KatDhtService dhtService,
            IUnixFsApi unixFsApi,
            IKeyApi keyApi,
            INameApi nameApi,
            IObjectApi objectApi,
            IPinApi pinApi,
            Ping1 pingService,
            IPubSubApi pubSubApi,
            PubSubService pubSubService,
            IStatsApi statsApi,
            ISwarmApi swarmApi,
            SwarmService swarmService,
            DfsOptions dfsOptions,
            IFileSystem fileSystem,
            IHashProvider hashProvider,
            DfsState dfsState,
            IPasswordManager passwordManager)
        {
            BitSwapApi = bitSwapApi;
            BitSwapService = bitSwapService;
            BlockApi = blockApi;
            BlockRepositoryApi = blockRepositoryApi;
            BootstrapApi = bootstrapApi;
            ConfigApi = configApi;
            DagApi = dagApi;
            DhtApi = dhtApi;
            DhtService = dhtService;
            UnixFsApi = unixFsApi;
            KeyApi = keyApi;
            NameApi = nameApi;
            ObjectApi = objectApi;
            PinApi = pinApi;
            PingService = pingService;
            PubSubApi = pubSubApi;
            PubSubService = pubSubService;
            StatsApi = statsApi;
            SwarmApi = swarmApi;
            SwarmService = swarmService;
            Options = dfsOptions;
            _fileSystem = fileSystem;
            _hashProvider = hashProvider;
            _dfsState = dfsState;
            DnsApi = dnsApi;

            passphrase = passwordManager.RetrieveOrPromptAndAddPasswordToRegistry(PasswordRegistryTypes.IpfsPassword,
                "Please provide your IPFS password");

            var swarmKey = "07a8e9d0c43400927ab274b7fa443596b71e609bacae47bd958e5cd9f59d6ca3";

            var seedServers = new[]
            {
                new MultiAddress(
                    "/ip4/46.101.132.61/tcp/4001/ipfs/18n3naE9kBZoVvgYMV6saMZdtAkDHgs8MDwwhtyLu8JpYitY4Nk8jmyGgQ4Gt3VKNson"),
                new MultiAddress(
                    "/ip4/188.166.13.135/tcp/4001/ipfs/18n3naE9kBZoVvgYMV6saMZe2AAPTCoujCxhJHECaySDEsPrEz9W2u7uo6hAbJhYzhPg"),
                new MultiAddress(
                    "/ip4/167.172.73.132/tcp/4001/ipfs/18n3naE9kBZoVvgYMV6saMZe1E9wXdykR6h3Q9EaQcQc6hdNAXyCTEzoGfcA2wQgCRyg")
            };

            Options.KeyChain.DefaultKeyType = "rsa";

            //Constants.KeyChainDefaultKeyType;
            Options.Repository.Folder = new DirectoryInfo(Path.Combine(
                Path.Combine(_fileSystem.GetCatalystDataDir().FullName, Constants.CatalystDataDir),
                Constants.DfsDataSubDir)).FullName;

            // The seed nodes for the catalyst network.
            //Options.Discovery.BootstrapPeers = seedServers;

            // Do not use the public IPFS network, use a private network
            // of catalyst only nodes.
            Options.Swarm.PrivateNetworkKey = new PreSharedKey
            {
                Value = swarmKey.ToHexBuffer()
            };

            InitAsync().Wait();
        }

        internal virtual AddFileOptions AddFileOptions()
        {
            return new AddFileOptions
            {
                Hash = _hashProvider.HashingAlgorithm.Name,
                RawLeaves = true
            };
        }

        /// <summary>
        ///     The configuration options.
        /// </summary>
        public DfsOptions Options { get; set; }

        private IKeyStoreService _keyStoreService;

        private async Task InitAsync()
        {
            MigrationManager = new MigrationManager(Options.Repository);

            Log.Debug("Building local peer");
            Log.Debug("Getting key info about self");
            await KeyApi.SetPassphraseAsync(passphrase).ConfigureAwait(false);

            var self = await KeyApi.GetPublicKeyAsync("self").ConfigureAwait(false)
             ?? await KeyApi.CreateAsync("self", null, 0).ConfigureAwait(false);

            var localPeer = new Peer
            {
                Id = self.Id,
                PublicKey = await(await KeyChainAsync()).GetPublicKeyAsync("self").ConfigureAwait(false),
                ProtocolVersion = "ipfs/0.1.0"
            };

            var version = typeof(DfsService).GetTypeInfo().Assembly.GetName().Version;
            localPeer.AgentVersion = $"net-ipfs/{version.Major}.{version.Minor}.{version.Revision}";
            Log.Debug("Built local peer");
            LocalPeer = localPeer;

            //Swarm
            Log.Debug("Building swarm service");

            if (Options.Swarm.PrivateNetworkKey == null)
            {
                var path = Path.Combine(Options.Repository.Folder, "swarm.key");
                if (File.Exists(path))
                {
                    using (var x = File.OpenText(path))
                    {
                        Options.Swarm.PrivateNetworkKey = new PreSharedKey();
                        Options.Swarm.PrivateNetworkKey.Import(x);
                    }
                }
            }

            //var peer = await LocalPeer.ConfigureAwait(false);
            //var self = await KeyApi.GetPrivateKeyAsync("self").ConfigureAwait(false);

            //var swarm = new SwarmService
            //{
            //    LocalPeer = LocalPeer,
            //    LocalPeerKey = Key.CreatePrivateKey(await KeyApi.GetPrivateKeyAsync("self").ConfigureAwait(false)),
            //    NetworkProtector = Options.Swarm.PrivateNetworkKey == null
            //        ? null
            //        : new Psk1Protector
            //        {
            //            Key = Options.Swarm.PrivateNetworkKey
            //        }
            //};

            SwarmService.LocalPeer = LocalPeer;
            SwarmService.LocalPeerKey =
                Key.CreatePrivateKey(await KeyApi.GetPrivateKeyAsync("self").ConfigureAwait(false));
            SwarmService.NetworkProtector = Options.Swarm.PrivateNetworkKey == null
                ? null
                : new Psk1Protector
                {
                    Key = Options.Swarm.PrivateNetworkKey
                };


            if (Options.Swarm.PrivateNetworkKey != null)
            {
                Log.Debug($"Private network {Options.Swarm.PrivateNetworkKey.Fingerprint().ToHexString()}");
            }

            Log.Debug("Built swarm service");

            //SwarmService = swarm;


            Log.Debug("Building bitswap service");
            //var bitswap = new BitswapService
            //{
            //    SwarmService = SwarmService,
            //    BlockService = BlockApi
            //};
            Log.Debug("Built bitswap service");
            //BitSwapService = bitswap;
            BitSwapService.SwarmService = SwarmService;
            BitSwapService.BlockService = BlockApi;


            Log.Debug("Building DHT service");
            //var dht = new DhtService
            //{
            //    SwarmService = SwarmService
            //};
            DhtService.SwarmService.Router = DhtService;
            
            Log.Debug("Built DHT service");
            //DhtService = dht;


            Log.Debug("Building Ping service");
            //var ping = new Ping1
            //{
            //    SwarmService = SwarmService
            //};
            Log.Debug("Built Ping service");
            PingService.SwarmService = SwarmService;


            Log.Debug("Building PubSub service");
            //var pubsub = new PubSubService
            //{
            //    LocalPeer = LocalPeer
            //};
            PubSubService.LocalPeer = LocalPeer;
            PubSubService.Routers.Add(new FloodRouter
            {
                SwarmService = SwarmService
            });
            Log.Debug("Built PubSub service");
            //PubSubService = pubsub;
        }

        /// <summary>
        ///     Starts the network services.
        /// </summary>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        /// </returns>
        /// <remarks>
        ///     Starts the various IPFS and Lib.P2P network services.  This should
        ///     be called after any configuration changes.
        /// </remarks>
        /// <exception cref="Exception">
        ///     When the engine is already started.
        /// </exception>
        public async Task StartAsync()
        {
            _dfsState.IsStarted = true;
            if (_stopTasks.Count > 0)
            {
                throw new Exception("IPFS engine is already started.");
            }

            // Repository must be at the correct version.
            await MigrationManager.MirgrateToVersionAsync(MigrationManager.LatestVersion).ConfigureAwait(false);

            var localPeer = LocalPeer;
            Log.Debug("starting " + localPeer.Id);

            // Everybody needs the swarm.
            var swarm = SwarmService;
            _stopTasks.Add(async () => { await swarm.StopAsync().ConfigureAwait(false); });
            await swarm.StartAsync().ConfigureAwait(false);

            var peerManager = new PeerManager
            {
                SwarmService = swarm
            };
            await peerManager.StartAsync().ConfigureAwait(false);
            _stopTasks.Add(async () => { await peerManager.StopAsync().ConfigureAwait(false); });

            // Start the primary services.
            var tasks = new List<Func<Task>>
            {
                async () =>
                {
                    var bitSwap = BitSwapService;
                    _stopTasks.Add(async () => await bitSwap.StopAsync().ConfigureAwait(false));
                    await bitSwap.StartAsync().ConfigureAwait(false);
                },
                async () =>
                {
                    var dht = DhtService;
                    _stopTasks.Add(async () => await dht.StopAsync().ConfigureAwait(false));
                    await dht.StartAsync().ConfigureAwait(false);
                },
                async () =>
                {
                    var ping = PingService;
                    _stopTasks.Add(async () => await ping.StopAsync().ConfigureAwait(false));
                    await ping.StartAsync().ConfigureAwait(false);
                },
                async () =>
                {
                    var pubsub = PubSubService;
                    _stopTasks.Add(async () => await pubsub.StopAsync().ConfigureAwait(false));
                    await pubsub.StartAsync().ConfigureAwait(false);
                }
            };

            Log.Debug("waiting for services to start");
            await Task.WhenAll(tasks.Select(t => t())).ConfigureAwait(false);

            // Starting listening to the swarm.
            var json = await ConfigApi.GetAsync("Addresses.Swarm").ConfigureAwait(false);
            var numberListeners = 0;
            foreach (string a in json)
            {
                try
                {
                    await swarm.StartListeningAsync(a).ConfigureAwait(false);
                    ++numberListeners;
                }
                catch (Exception e)
                {
                    Log.Warning($"Listener failure for '{a}'", e);
                }
            }

            if (numberListeners == 0)
            {
                Log.Error("No listeners were created.");
            }

            // Now that the listener addresses are established, the discovery 
            // services can begin.
            MulticastService multicast = null;
            if (!Options.Discovery.DisableMdns)
            {
                multicast = new MulticastService();
#pragma warning disable CS1998
                _stopTasks.Add(async () => multicast.Dispose());
#pragma warning restore CS1998
            }

            var autodialer = new AutoDialer(swarm)
            {
                MinConnections = Options.Swarm.MinConnections
            };
#pragma warning disable CS1998
            _stopTasks.Add(async () => autodialer.Dispose());
#pragma warning restore CS1998

            tasks = new List<Func<Task>>
            {
                // Bootstrap discovery
                async () =>
                {
                    var bootstrap = new Bootstrap
                    {
                        Addresses = await BootstrapApi.ListAsync()
                    };
                    bootstrap.PeerDiscovered += OnPeerDiscovered;
                    _stopTasks.Add(async () => await bootstrap.StopAsync().ConfigureAwait(false));
                    await bootstrap.StartAsync().ConfigureAwait(false);
                },

                async () =>
                {
                    if (Options.Discovery.DisableRandomWalk)
                        return;
                    var randomWalk = new RandomWalk {Dht = DhtApi};
                    _stopTasks.Add(async () => await randomWalk.StopAsync().ConfigureAwait(false));
                    await randomWalk.StartAsync().ConfigureAwait(false);
                }
            };
            Log.Debug("waiting for discovery services to start");
            await Task.WhenAll(tasks.Select(t => t())).ConfigureAwait(false);

            multicast?.Start();

            Log.Debug("started");
        }

        /// <summary>
        ///     Stops the running services.
        /// </summary>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        /// </returns>
        /// <remarks>
        ///     Multiple calls are okay.
        /// </remarks>
        public async Task StopAsync()
        {
            Log.Debug("stopping");
            try
            {
                var tasks = _stopTasks.ToArray();
                _stopTasks = new ConcurrentBag<Func<Task>>();
                await Task.WhenAll(tasks.Select(t => t())).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Log.Error("Failure when stopping the engine", e);
            }

            // Many services use cancellation to stop.  A cancellation may not run
            // immediately, so we need to give them some.
            // TODO: Would be nice to make this deterministic.
            await Task.Delay(TimeSpan.FromMilliseconds(100)).ConfigureAwait(false);

            Log.Debug("stopped");
        }

        /// <summary>
        ///     A synchronous start.
        /// </summary>
        /// <remarks>
        ///     Calls <see cref="StartAsync" /> and waits for it to complete.
        /// </remarks>
        public void Start() { StartAsync().ConfigureAwait(false).GetAwaiter().GetResult(); }

        /// <summary>
        ///     A synchronous stop.
        /// </summary>
        /// <remarks>
        ///     Calls <see cref="StopAsync" /> and waits for it to complete.
        /// </remarks>
        public void Stop()
        {
            Log.Debug("stopping");
            try
            {
                var tasks = _stopTasks.ToArray();
                _stopTasks = new ConcurrentBag<Func<Task>>();
                foreach (var task in tasks)
                {
                    task().ConfigureAwait(false).GetAwaiter().GetResult();
                }
            }
            catch (Exception e)
            {
                Log.Error("Failure when stopping the engine", e);
            }
        }

#pragma warning disable VSTHRD100 // Avoid async void methods
        /// <summary>
        ///     Fired when a peer is discovered.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="peer"></param>
        /// <remarks>
        ///     Registers the peer with the <see cref="SwarmService" />.
        /// </remarks>
        private async void OnPeerDiscovered(object sender, Peer peer)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            try
            {
                var swarm = SwarmService;
                swarm.RegisterPeer(peer);
            }
            catch (Exception ex)
            {
                Log.Warning("failed to register peer " + peer, ex);

                // eat it, nothing we can do.
            }
        }

        /// <summary>
        /// @TODO this should reeally go lower down in the kernal when we load keystores
        ///   Provides access to the <see cref="KeyStoreService"/>.
        /// </summary>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result is
        ///   the <see cref="KeyStoreService"/>.
        /// </returns>
        public async Task<IKeyStoreService> KeyChainAsync(CancellationToken cancel = default(CancellationToken))
        {
            // TODO: this should be a LazyAsync property.
            if (_keyStoreService == null)
            {
                lock (this)
                {
                    if (_keyStoreService == null)
                    {
                        _keyStoreService = new KeyStoreService(_fileSystem)
                        {
                            Options = Options.KeyChain
                        };
                    }
                }

                await _keyStoreService.SetPassphraseAsync(passphrase, cancel).ConfigureAwait(false);

                // Maybe create "self" key, this is the local peer's id.
                var self = await _keyStoreService.FindKeyByNameAsync("self", cancel).ConfigureAwait(false) ?? await _keyStoreService.CreateAsync("self", null, 0, cancel).ConfigureAwait(false);
            }

            return _keyStoreService;
        }

        #region Class members

        /// <summary>
        ///     Determines latency to a peer.
        /// </summary>
        public Ping1 PingService { get; private set; }

        /// <summary>
        ///     Provides access to the local peer.
        /// </summary>
        /// <returns>
        ///     A task that represents the asynchronous operation. The task's result is
        ///     a <see cref="Peer" />.
        /// </returns>
        public Peer LocalPeer { get; private set; }

        /// <summary>
        ///     Manages the version of the repository.
        /// </summary>
        public IMigrationManager MigrationManager { get; set; }

        #region Dfs Services

        /// <summary>
        ///     Manages communication with other peers.
        /// </summary>
        public SwarmService SwarmService { get; private set; }

        /// <summary>
        ///     Manages publishng and subscribing to messages.
        /// </summary>
        public PubSubService PubSubService { get; private set; }

        /// <summary>
        ///     Exchange blocks with other peers.
        /// </summary>
        public IBitswapService BitSwapService { get; private set; }

        /// <summary>
        ///     Finds information with a distributed hash table.
        /// </summary>
        public DhtService DhtService { get; private set; }

        #endregion

        #region CoreAPI Support

        /// <inheritdoc />
        public IBitSwapApi BitSwapApi { get; set; }

        /// <inheritdoc />
        public IBlockApi BlockApi { get; set; }

        /// <inheritdoc />
        public IBlockRepositoryApi BlockRepositoryApi { get; set; }

        /// <inheritdoc />
        public IBootstrapApi BootstrapApi { get; set; }

        /// <inheritdoc />
        public IConfigApi ConfigApi { get; set; }

        /// <inheritdoc />
        public IDagApi DagApi { get; set; }

        /// <inheritdoc />
        public IDhtApi DhtApi { get; set; }

        /// <inheritdoc />
        public IDnsApi DnsApi { get; set; }

        /// <inheritdoc />
        public IUnixFsApi UnixFsApi { get; set; }

        /// <inheritdoc />
        public IKeyApi KeyApi { get; set; }

        /// <inheritdoc />
        public INameApi NameApi { get; set; }

        /// <inheritdoc />
        public IObjectApi ObjectApi { get; set; }

        /// <inheritdoc />
        public IPinApi PinApi { get; set; }

        /// <inheritdoc />
        public IPubSubApi PubSubApi { get; set; }

        /// <inheritdoc />
        public ISwarmApi SwarmApi { get; set; }

        /// <inheritdoc />
        public IStatsApi StatsApi { get; set; }

        #endregion

        #endregion

        #region IDisposable Support

        private bool _disposedValue; // To detect redundant calls

        /// <summary>
        ///     Releases the unmanaged and optionally managed resources.
        /// </summary>
        /// <param name="disposing">
        ///     <b>true</b> to release both managed and unmanaged resources; <b>false</b>
        ///     to release only unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposedValue)
            {
                return;
            }

            _disposedValue = true;

            if (!disposing)
            {
                return;
            }

            passphrase?.Dispose();
            Stop();
        }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() { Dispose(true); }

        #endregion
    }
}
