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
using Catalyst.Abstractions.Hashing;
using Catalyst.Abstractions.Keystore;
using Catalyst.Abstractions.Options;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Lib.Config;
using Catalyst.Core.Modules.Dfs.CoreApi;
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
    public class Dfs : IDfs
    {
        static Dfs()
        {
            LogManager.Adapter = new SerilogFactoryAdapter(Log.Logger);
        }

        /// <summary>
        ///   Determines if the engine has started.
        /// </summary>
        /// <value>
        ///   <b>true</b> if the engine has started; otherwise, <b>false</b>.
        /// </value>
        /// <seealso cref="Start"/>
        /// <seealso cref="StartAsync"/>
        public bool IsStarted => _stopTasks.Count > 0;
        
        private KeyChain _keyChain;
        private SecureString passphrase;
        private readonly IHashProvider _hashProvider;
        private ConcurrentBag<Func<Task>> _stopTasks = new ConcurrentBag<Func<Task>>();

        public Dfs(IHashProvider hashProvider, IPasswordManager passwordManager)
        {
            _hashProvider = hashProvider;
         
            passphrase = passwordManager.RetrieveOrPromptAndAddPasswordToRegistry(PasswordRegistryTypes.IpfsPassword, "Please provide your IPFS password");

            // Options.KeyChain.DefaultKeyType = Constants.KeyChainDefaultKeyType;
            Options.Repository.Folder = new DirectoryInfo(Path.Combine(
                Path.Combine(Catalyst.Core.Lib.FileSystem.FileSystem.GetUserHomeDir(), Constants.CatalystDataDir),
                Constants.DfsDataSubDir)).FullName;
            
            // // The seed nodes for the catalyst network.
            // Options.Discovery.BootstrapPeers = seedServers;
            //
            // // Do not use the public IPFS network, use a private network
            // // of catalyst only nodes.
            // _ipfs.Options.Swarm.PrivateNetworkKey = new PreSharedKey
            // {
            //     Value = swarmKey.ToHexBuffer()
            // };
            
            Init();
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
        ///   The configuration options.
        /// </summary>
        public DfsOptions Options { get; set; } = new DfsOptions();

        void Init()
        {
            // Init the core api inteface.
            Bitswap = new BitswapApi(this);
            Block = new BlockApi(this);
            BlockRepository = new BlockRepositoryApi(this);
            Bootstrap = new BootstrapApi(this);
            Config = new ConfigApi(this);
            Dag = new DagApi(this);
            Dht = new DhtApi(this);
            Dns = new DnsApi(this);
            FileSystem = new FileSystemApi(this);
            Generic = new GenericApi(this);
            Key = new KeyApi(this);
            Name = new NameApi(this);
            Object = new ObjectApi(this);
            Pin = new PinApi(this);
            PubSub = new PubSubApi(this);
            Stats = new StatsApi(this);
            Swarm = new SwarmApi(this);

            MigrationManager = new MigrationManager(this);

            // Async properties
            LocalPeer = new AsyncLazy<Peer>(async () =>
            {
                Log.Debug("Building local peer");
                var keyChain = await KeyChainAsync().ConfigureAwait(false);
                Log.Debug("Getting key info about self");
                var self = await keyChain.FindKeyByNameAsync("self").ConfigureAwait(false);
                var localPeer = new Peer
                {
                    Id = self.Id,
                    PublicKey = await keyChain.GetPublicKeyAsync("self").ConfigureAwait(false),
                    ProtocolVersion = "ipfs/0.1.0"
                };
                var version = typeof(Dfs).GetTypeInfo().Assembly.GetName().Version;
                localPeer.AgentVersion = $"net-ipfs/{version.Major}.{version.Minor}.{version.Revision}";
                Log.Debug("Built local peer");
                return localPeer;
            });
            
            SwarmService = new AsyncLazy<Swarm>(async () =>
            {
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

                var peer = await LocalPeer.ConfigureAwait(false);
                var keyChain = await KeyChainAsync().ConfigureAwait(false);
                var self = await keyChain.GetPrivateKeyAsync("self").ConfigureAwait(false);
                
                var swarm = new Swarm
                {
                    LocalPeer = peer,
                    LocalPeerKey = global::Lib.P2P.Cryptography.Key.CreatePrivateKey(self),
                    NetworkProtector = Options.Swarm.PrivateNetworkKey == null
                        ? null
                        : new Psk1Protector
                        {
                            Key = Options.Swarm.PrivateNetworkKey
                        }
                };
                
                if (Options.Swarm.PrivateNetworkKey != null)
                {
                    Log.Debug($"Private network {Options.Swarm.PrivateNetworkKey.Fingerprint().ToHexString()}");
                }

                Log.Debug("Built swarm service");
                return swarm;
            });
            
            BitswapService = new AsyncLazy<IBitswapService>(async () =>
            {
                Log.Debug("Building bitswap service");
                var bitswap = new BlockExchange.BitswapService
                {
                    Swarm = await SwarmService.ConfigureAwait(false),
                    BlockService = Block
                };
                Log.Debug("Built bitswap service");
                return bitswap;
            });
            
            DhtService = new AsyncLazy<Dht1>(async () =>
            {
                Log.Debug("Building DHT service");
                var dht = new Dht1
                {
                    Swarm = await SwarmService.ConfigureAwait(false)
                };
                dht.Swarm.Router = dht;
                Log.Debug("Built DHT service");
                return dht;
            });
            
            PingService = new AsyncLazy<Ping1>(async () =>
            {
                Log.Debug("Building Ping service");
                var ping = new Ping1
                {
                    Swarm = await SwarmService.ConfigureAwait(false)
                };
                Log.Debug("Built Ping service");
                return ping;
            });
            
            PubSubService = new AsyncLazy<NotificationService>(async () =>
            {
                Log.Debug("Building PubSub service");
                var pubsub = new NotificationService
                {
                    LocalPeer = await LocalPeer.ConfigureAwait(false)
                };
                pubsub.Routers.Add(new FloodRouter
                {
                    Swarm = await SwarmService.ConfigureAwait(false)
                });
                Log.Debug("Built PubSub service");
                return pubsub;
            });
        }
        
        /// <summary>
        ///   Resolve an "IPFS path" to a content ID.
        /// </summary>
        /// <param name="path">
        ///   A IPFS path, such as "Qm...", "Qm.../a/b/c" or "/ipfs/QM..."
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   The content ID of <paramref name="path"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   The <paramref name="path"/> cannot be resolved.
        /// </exception>
        public async Task<Cid> ResolveIpfsPathToCidAsync(string path,
            CancellationToken cancel = default(CancellationToken))
        {
            var r = await Generic.ResolveAsync(path, true, cancel).ConfigureAwait(false);
            return Cid.Decode(r.Remove(0, 6)); // strip '/ipfs/'.
        }
        
        /// <summary>
        ///   Starts the network services.
        /// </summary>
        /// <returns>
        ///   A task that represents the asynchronous operation.
        /// </returns>
        /// <remarks>
        ///   Starts the various IPFS and Lib.P2P network services.  This should
        ///   be called after any configuration changes.
        /// </remarks>
        /// <exception cref="Exception">
        ///   When the engine is already started.
        /// </exception>
        public async Task StartAsync()
        {
            if (_stopTasks.Count > 0)
            {
                throw new Exception("IPFS engine is already started.");
            }

            // Repository must be at the correct version.
            await MigrationManager.MirgrateToVersionAsync(MigrationManager.LatestVersion).ConfigureAwait(false);

            var localPeer = await LocalPeer.ConfigureAwait(false);
            Log.Debug("starting " + localPeer.Id);

            // Everybody needs the swarm.
            var swarm = await SwarmService.ConfigureAwait(false);
            _stopTasks.Add(async () => { await swarm.StopAsync().ConfigureAwait(false); });
            await swarm.StartAsync().ConfigureAwait(false);

            var peerManager = new PeerManager
            {
                Swarm = swarm
            };
            await peerManager.StartAsync().ConfigureAwait(false);
            _stopTasks.Add(async () => { await peerManager.StopAsync().ConfigureAwait(false); });

            // Start the primary services.
            var tasks = new List<Func<Task>>
            {
                async () =>
                {
                    var bitswap = await BitswapService.ConfigureAwait(false);
                    _stopTasks.Add(async () => await bitswap.StopAsync().ConfigureAwait(false));
                    await bitswap.StartAsync().ConfigureAwait(false);
                },
                async () =>
                {
                    var dht = await DhtService.ConfigureAwait(false);
                    _stopTasks.Add(async () => await dht.StopAsync().ConfigureAwait(false));
                    await dht.StartAsync().ConfigureAwait(false);
                },
                async () =>
                {
                    var ping = await PingService.ConfigureAwait(false);
                    _stopTasks.Add(async () => await ping.StopAsync().ConfigureAwait(false));
                    await ping.StartAsync().ConfigureAwait(false);
                },
                async () =>
                {
                    var pubsub = await PubSubService.ConfigureAwait(false);
                    _stopTasks.Add(async () => await pubsub.StopAsync().ConfigureAwait(false));
                    await pubsub.StartAsync().ConfigureAwait(false);
                },
            };

            Log.Debug("waiting for services to start");
            await Task.WhenAll(tasks.Select(t => t())).ConfigureAwait(false);

            // Starting listening to the swarm.
            var json = await Config.GetAsync("Addresses.Swarm").ConfigureAwait(false);
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

                    // eat the exception
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
                        Addresses = await this.Bootstrap.ListAsync()
                    };
                    bootstrap.PeerDiscovered += OnPeerDiscovered;
                    _stopTasks.Add(async () => await bootstrap.StopAsync().ConfigureAwait(false));
                    await bootstrap.StartAsync().ConfigureAwait(false);
                },

                // New multicast DNS discovery
                async () =>
                {
                    if (Options.Discovery.DisableMdns)
                        return;
                    var mdns = new MdnsNext
                    {
                        LocalPeer = localPeer,
                        MulticastService = multicast
                    };
                    if (Options.Swarm.PrivateNetworkKey != null)
                    {
                        mdns.ServiceName = $"_p2p-{Options.Swarm.PrivateNetworkKey.Fingerprint().ToHexString()}._udp";
                    }

                    mdns.PeerDiscovered += OnPeerDiscovered;
                    _stopTasks.Add(async () => await mdns.StopAsync().ConfigureAwait(false));
                    await mdns.StartAsync().ConfigureAwait(false);
                },

                // Old style JS multicast DNS discovery
                async () =>
                {
                    if (Options.Discovery.DisableMdns || Options.Swarm.PrivateNetworkKey != null)
                        return;
                    var mdns = new MdnsJs
                    {
                        LocalPeer = localPeer,
                        MulticastService = multicast
                    };
                    mdns.PeerDiscovered += OnPeerDiscovered;
                    _stopTasks.Add(async () => await mdns.StopAsync().ConfigureAwait(false));
                    await mdns.StartAsync().ConfigureAwait(false);
                },

                // Old style GO multicast DNS discovery
                async () =>
                {
                    if (Options.Discovery.DisableMdns || Options.Swarm.PrivateNetworkKey != null)
                        return;
                    var mdns = new MdnsGo
                    {
                        LocalPeer = localPeer,
                        MulticastService = multicast
                    };
                    mdns.PeerDiscovered += OnPeerDiscovered;
                    _stopTasks.Add(async () => await mdns.StopAsync().ConfigureAwait(false));
                    await mdns.StartAsync().ConfigureAwait(false);
                },
                async () =>
                {
                    if (Options.Discovery.DisableRandomWalk)
                        return;
                    var randomWalk = new RandomWalk {Dht = Dht};
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
        ///   Stops the running services.
        /// </summary>
        /// <returns>
        ///   A task that represents the asynchronous operation.
        /// </returns>
        /// <remarks>
        ///   Multiple calls are okay.
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
        ///   A synchronous start.
        /// </summary>
        /// <remarks>
        ///   Calls <see cref="StartAsync"/> and waits for it to complete.
        /// </remarks>
        public void Start() { StartAsync().ConfigureAwait(false).GetAwaiter().GetResult(); }

        /// <summary>
        ///   A synchronous stop.
        /// </summary>
        /// <remarks>
        ///   Calls <see cref="StopAsync"/> and waits for it to complete.
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
        ///   Fired when a peer is discovered.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="peer"></param>
        /// <remarks>
        ///   Registers the peer with the <see cref="SwarmService"/>.
        /// </remarks>
        async void OnPeerDiscovered(object sender, Peer peer)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            try
            {
                var swarm = await SwarmService.ConfigureAwait(false);
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
        ///   Provides access to the <see cref="KeyChain"/>.
        /// </summary>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result is
        ///   the <see cref="KeyChain"/>.
        /// </returns>
        public async Task<IKeyApi> KeyChainAsync(CancellationToken cancel = default(CancellationToken))
        {
            // TODO: this should be a LazyAsync property.
            if (_keyChain == null)
            {
                lock (this)
                {
                    if (_keyChain == null)
                    {
                        _keyChain = new KeyChain(Options.Repository.Folder)
                        {
                            Options = Options.KeyChain
                        };
                    }
                }

                await _keyChain.SetPassphraseAsync(passphrase, cancel).ConfigureAwait(false);

                // Maybe create "self" key, this is the local peer's id.
                var self = await _keyChain.FindKeyByNameAsync("self", cancel).ConfigureAwait(false) ?? await _keyChain.CreateAsync("self", null, 0, cancel).ConfigureAwait(false);
            }

            return _keyChain;
        }

        #region Class members

        /// <summary>
        ///   Determines latency to a peer.
        /// </summary>
        public AsyncLazy<Ping1> PingService { get; private set; }
        
        /// <summary>
        ///   Provides access to the local peer.
        /// </summary>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result is
        ///   a <see cref="Peer"/>.
        /// </returns>
        public AsyncLazy<Peer> LocalPeer { get; private set; }
        
        /// <summary>
        ///   Manages the version of the repository.
        /// </summary>
        public IMigrationManager MigrationManager { get; set; }

        #region Dfs Services
        
        /// <summary>
        ///   Manages communication with other peers.
        /// </summary>
        public AsyncLazy<Swarm> SwarmService { get; private set; }

        /// <summary>
        ///   Manages publishng and subscribing to messages.
        /// </summary>
        public AsyncLazy<NotificationService> PubSubService { get; private set; }

        /// <summary>
        ///   Exchange blocks with other peers.
        /// </summary>
        public AsyncLazy<IBitswapService> BitswapService { get; private set; }
        
        /// <summary>
        ///   Finds information with a distributed hash table.
        /// </summary>
        public AsyncLazy<Dht1> DhtService { get; private set; }
        
        #endregion

        #region CoreAPI Support

        /// <inheritdoc />
        public IBitswapApi Bitswap { get; set; }

        /// <inheritdoc />
        public IBlockApi Block { get; set; }

        /// <inheritdoc />
        public IBlockRepositoryApi BlockRepository { get; set; }

        /// <inheritdoc />
        public IBootstrapApi Bootstrap { get; set; }

        /// <inheritdoc />
        public IConfigApi Config { get; set; }

        /// <inheritdoc />
        public IDagApi Dag { get; set; }

        /// <inheritdoc />
        public IDhtApi Dht { get; set; }

        /// <inheritdoc />
        public IDnsApi Dns { get; set; }

        /// <inheritdoc />
        public IFileSystemApi FileSystem { get; set; }

        /// <inheritdoc />
        public IGenericApi Generic { get; set; }

        /// <inheritdoc />
        public IKeyApi Key { get; set; }

        /// <inheritdoc />
        public INameApi Name { get; set; }

        /// <inheritdoc />
        public IObjectApi Object { get; set; }

        /// <inheritdoc />
        public IPinApi Pin { get; set; }

        /// <inheritdoc />
        public IPubSubApi PubSub { get; set; }

        /// <inheritdoc />
        public ISwarmApi Swarm { get; set; }

        /// <inheritdoc />
        public IStatsApi Stats { get; set; }

        #endregion
        
        #endregion
        
        #region IDisposable Support

        bool disposedValue = false; // To detect redundant calls

        /// <summary>
        ///  Releases the unmanaged and optionally managed resources.
        /// </summary>
        /// <param name="disposing">
        ///   <b>true</b> to release both managed and unmanaged resources; <b>false</b> 
        ///   to release only unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposedValue)
            {
                return;
            }
            
            disposedValue = true;

            if (!disposing)
            {
                return;
            }
            
            passphrase?.Dispose();
            Stop();
        }

        /// <summary>
        ///   Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}
