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
using System.Linq;
using System.Threading.Tasks;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.Dfs.BlockExchange;
using Catalyst.Abstractions.Dfs.CoreApi;
using Catalyst.Abstractions.Dfs.Migration;
using Catalyst.Abstractions.Hashing;
using Catalyst.Abstractions.Keystore;
using Catalyst.Abstractions.Options;
using Catalyst.Abstractions.P2P.Repository;
using Catalyst.Core.Lib.P2P;
using Catalyst.Core.Modules.Dfs.BlockExchange;
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
using Serilog;

namespace Catalyst.Core.Modules.Dfs
{
    public class DfsService : IDfsService
    {
        static DfsService() { LogManager.Adapter = new SerilogFactoryAdapter(Log.Logger); }

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
        private readonly IHashProvider _hashProvider;
        private ConcurrentBag<Func<Task>> _stopTasks = new ConcurrentBag<Func<Task>>();

        public DfsService(
            Peer localPeer,
            IBitswapService bitSwapService,
            IDhtService dhtService,
            Ping1 pingService,
            IPubSubService pubSubService,
            ISwarmService swarmService,
            IBootstrapApi bootstrapApi,
            IConfigApi configApi,
            IBitSwapApi bitSwapApi,
            IBlockApi blockApi,
            IBlockRepositoryApi blockRepositoryApi,
            IDagApi dagApi,
            IDhtApi dhtApi,
            IDnsApi dnsApi,
            IUnixFsApi unixFsApi,
            IKeyApi keyApi,
            INameApi nameApi,
            IObjectApi objectApi,
            IPinApi pinApi,
            IPubSubApi pubSubApi,
            IStatsApi statsApi,
            ISwarmApi swarmApi,
            IHashProvider hashProvider,
            DfsOptions dfsOptions,
            DfsState dfsState,
            IMigrationManager migrationManager)
        {
            LocalPeer = localPeer;

            BitSwapService = bitSwapService;
            DhtService = dhtService;
            PingService = pingService;
            PubSubService = pubSubService;
            SwarmService = swarmService;

            BootstrapApi = bootstrapApi;
            ConfigApi = configApi;     
            BitSwapApi = bitSwapApi;
            BlockApi = blockApi;
            BlockRepositoryApi = blockRepositoryApi;
            DagApi = dagApi;
            DhtApi = dhtApi;
            DnsApi = dnsApi;
            UnixFsApi = unixFsApi;
            KeyApi = keyApi;
            NameApi = nameApi;
            ObjectApi = objectApi;
            PinApi = pinApi;
            PubSubApi = pubSubApi;
            StatsApi = statsApi;
            SwarmApi = swarmApi;
            Options = dfsOptions;
            _hashProvider = hashProvider;
            _dfsState = dfsState;
            MigrationManager = migrationManager;

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

        //private IKeyStoreService _keyStoreService;

        private async Task InitAsync()
        {
            //Swarm
            Log.Debug("Building swarm service");

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

            Log.Debug("Building PubSub service");

            //PubSubService.LocalPeer = LocalPeer;
            Log.Debug("Built PubSub service");
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

            Log.Debug("starting " + LocalPeer.Id);

            // Everybody needs the swarm.
            _stopTasks.Add(async () => { await SwarmService.StopAsync().ConfigureAwait(false); });
            await SwarmService.StartAsync().ConfigureAwait(false);

            var peerManager = new PeerManager
            {
                SwarmService = SwarmService
            };
            await peerManager.StartAsync().ConfigureAwait(false);
            _stopTasks.Add(async () => { await peerManager.StopAsync().ConfigureAwait(false); });

            // Start the primary services.
            var tasks = new List<Func<Task>>
            {
                async () =>
                {
                    _stopTasks.Add(async () => await BitSwapService.StopAsync().ConfigureAwait(false));
                    await BitSwapService.StartAsync().ConfigureAwait(false);
                },
                async () =>
                {
                    _stopTasks.Add(async () => await DhtService.StopAsync().ConfigureAwait(false));
                    await DhtService.StartAsync().ConfigureAwait(false);
                },
                async () =>
                {
                    _stopTasks.Add(async () => await PingService.StopAsync().ConfigureAwait(false));
                    await PingService.StartAsync().ConfigureAwait(false);
                },
                async () =>
                {
                    _stopTasks.Add(async () => await PubSubService.StopAsync().ConfigureAwait(false));
                    await PubSubService.StartAsync().ConfigureAwait(false);
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
                    await SwarmService.StartListeningAsync(a).ConfigureAwait(false);
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

            var autodialer = new AutoDialer(SwarmService)
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
        private void OnPeerDiscovered(object sender, Peer peer)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            try
            {
                SwarmService.RegisterPeer(peer);
            }
            catch (Exception ex)
            {
                Log.Warning("failed to register peer " + peer, ex);

                // eat it, nothing we can do.
            }
        }

        #region Class members

        /// <summary>
        ///     Determines latency to a peer.
        /// </summary>
        public Ping1 PingService { get; }

        /// <summary>
        ///     Provides access to the local peer.
        /// </summary>
        /// <returns>
        ///     A task that represents the asynchronous operation. The task's result is
        ///     a <see cref="Peer" />.
        /// </returns>
        public Peer LocalPeer { get; }

        /// <summary>
        ///     Manages the version of the repository.
        /// </summary>
        public IMigrationManager MigrationManager { get; set; }

        #region Dfs Services

        /// <summary>
        ///     Manages communication with other peers.
        /// </summary>
        public ISwarmService SwarmService { get; }

        /// <summary>
        ///     Manages publishng and subscribing to messages.
        /// </summary>
        public IPubSubService PubSubService { get; }

        /// <summary>
        ///     Exchange blocks with other peers.
        /// </summary>
        public IBitswapService BitSwapService { get; }

        /// <summary>
        ///     Finds information with a distributed hash table.
        /// </summary>
        public IDhtService DhtService { get; }

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

            Stop();
        }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() { Dispose(true); }

        #endregion
    }
}
