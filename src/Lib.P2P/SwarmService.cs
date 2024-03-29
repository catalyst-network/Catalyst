#region LICENSE

/**
* Copyright (c) 2024 Catalyst Network
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
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using Lib.P2P.Cryptography;
using Lib.P2P.Protocols;
using Lib.P2P.Routing;
using Lib.P2P.SecureCommunication;
using Lib.P2P.Transports;
using MultiFormats;
using Nito.AsyncEx;

namespace Lib.P2P
{
    /// <summary>
    ///   Manages communication with other peers.
    /// </summary>
    public class SwarmService : ISwarmService
    {
        private static ILog _log = LogManager.GetLogger(typeof(SwarmService));

        /// <summary>
        ///   The time to wait for a low level connection to be established.
        /// </summary>
        /// <value>
        ///   Defaults to 30 seconds.
        /// </value>
        public TimeSpan TransportConnectionTimeout = TimeSpan.FromSeconds(30);

        /// <summary>
        ///  The supported protocols.
        /// </summary>
        /// <remarks>
        ///   Use sychronized access, e.g. <code>lock (protocols) { ... }</code>.
        /// </remarks>
        private List<IPeerProtocol> _protocols = new List<IPeerProtocol>
        {
            new Multistream1(),
            new Secio1(),
            new Identify1(),
            new Mplex67()
        };

        /// <summary>
        ///   Added to connection protocols when needed.
        /// </summary>
        private readonly Plaintext1 _plaintext1 = new Plaintext1();

        private Peer _localPeer;

        /// <summary>
        ///   Raised when a listener is establihed.
        /// </summary>
        /// <remarks>
        ///   Raised when <see cref="StartListeningAsync(MultiAddress)"/>
        ///   succeeds.
        /// </remarks>
        public event EventHandler<Peer> ListenerEstablished;

        /// <summary>
        ///   Raised when a connection to another peer is established.
        /// </summary>
        public event EventHandler<PeerConnection> ConnectionEstablished;

        /// <summary>
        ///   Raised when a new peer is discovered for the first time.
        /// </summary>
        public event EventHandler<Peer> PeerDiscovered;

        /// <summary>
        ///   Raised when a peer's connection is closed.
        /// </summary>
        public event EventHandler<Peer> PeerDisconnected;

        /// <summary>
        ///   Raised when a peer should no longer be used.
        /// </summary>
        /// <remarks>
        ///   This event indicates that the peer has been removed
        ///   from the <see cref="KnownPeers"/> and should no longer
        ///   be used.
        /// </remarks>
        public event EventHandler<Peer> PeerRemoved;

        /// <summary>
        ///   Raised when a peer cannot be connected to.
        /// </summary>
        public event EventHandler<Peer> PeerNotReachable;

        /// <summary>
        ///  The local peer.
        /// </summary>
        /// <value>
        ///   The local peer must have an <see cref="Peer.Id"/> and
        ///   <see cref="Peer.PublicKey"/>.
        /// </value>
        public Peer LocalPeer
        {
            get => _localPeer;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException();
                }

                if (value.Id == null)
                {
                    throw new ArgumentNullException($"peer.id");
                }

                if (value.PublicKey == null)
                {
                    throw new ArgumentNullException($"peer.PublicKey");
                }

                if (!value.IsValid())
                {
                    throw new ArgumentException("Invalid peer.");
                }
                
                _localPeer = value;
            }
        }

        /// <summary>
        ///   The private key of the local peer.
        /// </summary>
        /// <value>
        ///   Used to prove the identity of the <see cref="LocalPeer"/>.
        /// </value>
        public Key LocalPeerKey { get; set; }

        /// <summary>
        ///   Other nodes. Key is the bae58 hash of the peer ID.
        /// </summary>
        private readonly ConcurrentDictionary<string, Peer> _otherPeers = new ConcurrentDictionary<string, Peer>();

        /// <summary>
        ///   Used to cancel any task when the swarm is stopped.
        /// </summary>
        private CancellationTokenSource _swarmCancellation;

        /// <summary>
        ///  Outstanding connection tasks initiated by the local peer.
        /// </summary>
        private readonly ConcurrentDictionary<Peer, AsyncLazy<PeerConnection>> _pendingConnections =
            new ConcurrentDictionary<Peer, AsyncLazy<PeerConnection>>();

        /// <summary>
        ///  Outstanding connection tasks initiated by a remote peer.
        /// </summary>
        private readonly ConcurrentDictionary<MultiAddress, object> _pendingRemoteConnections =
            new ConcurrentDictionary<MultiAddress, object>();

        /// <summary>
        ///   Manages the swarm's peer connections.
        /// </summary>
        internal readonly ConnectionManager Manager = new ConnectionManager();

        /// <summary>
        ///   Use to find addresses of a peer.
        /// </summary>
        public IPeerRouting Router { get; set; }

        /// <summary>
        ///   Provides access to a private network of peers.
        /// </summary>
        public INetworkProtector NetworkProtector { get; set; }

        /// <summary>
        ///   Determines if the swarm has been started.
        /// </summary>
        /// <value>
        ///   <b>true</b> if the swarm has started; otherwise, <b>false</b>.
        /// </value>
        /// <seealso cref="StartAsync"/>
        /// <seealso cref="StopAsync"/>
        public bool IsRunning { get; private set; }

        /// <summary>
        ///   Cancellation tokens for the listeners.
        /// </summary>
        private ConcurrentDictionary<MultiAddress, CancellationTokenSource> _listeners =
            new ConcurrentDictionary<MultiAddress, CancellationTokenSource>();

        /// <summary>
        ///   Get the sequence of all known peer addresses.
        /// </summary>
        /// <value>
        ///   Contains any peer address that has been
        ///   <see cref="RegisterPeerAddress">discovered</see>.
        /// </value>
        /// <seealso cref="RegisterPeerAddress"/>
        public IEnumerable<MultiAddress> KnownPeerAddresses
        {
            get
            {
                return _otherPeers
                   .Values
                   .SelectMany(p => p.Addresses);
            }
        }

        /// <summary>
        ///   Get the sequence of all known peers.
        /// </summary>
        /// <value>
        ///   Contains any peer that has been
        ///   <see cref="RegisterPeerAddress">discovered</see>.
        /// </value>
        /// <seealso cref="RegisterPeerAddress"/>
        public IEnumerable<Peer> KnownPeers => _otherPeers.Values;

        /// <summary>
        ///   Register that a peer's address has been discovered.
        /// </summary>
        /// <param name="address">
        ///   An address to the peer. It must end with the peer ID.
        /// </param>
        /// <returns>
        ///   The <see cref="Peer"/> that is registered.
        /// </returns>
        /// <exception cref="Exception">
        ///   The <see cref="BlackList"/> or <see cref="WhiteList"/> policies forbid it.
        ///   Or the "p2p/ipfs" protocol name is missing.
        /// </exception>
        /// <remarks>
        ///   If the <paramref name="address"/> is not already known, then it is
        ///   added to the <see cref="KnownPeerAddresses"/>.
        /// </remarks>
        /// <seealso cref="RegisterPeer(Peer)"/>
        public Peer RegisterPeerAddress(MultiAddress address)
        {
            var peer = new Peer
            {
                Id = address.PeerId,
                Addresses = new List<MultiAddress> {address}
            };

            return RegisterPeer(peer);
        }

        /// <summary>
        ///   Register that a peer has been discovered.
        /// </summary>
        /// <param name="peer">
        ///   The newly discovered peer.
        /// </param>
        /// <returns>
        ///   The registered peer.
        /// </returns>
        /// <remarks>
        ///   If the peer already exists, then the existing peer is updated with supplied
        ///   information and is then returned.  Otherwise, the <paramref name="peer"/>
        ///   is added to known peers and is returned.
        ///   <para>
        ///   If the peer already exists, then a union of the existing and new addresses
        ///   is used.  For all other information the <paramref name="peer"/>'s information
        ///   is used if not <b>null</b>.
        ///   </para>
        ///   <para>
        ///   If peer does not already exist, then the <see cref="PeerDiscovered"/> event
        ///   is raised.
        ///   </para>
        /// </remarks>
        /// <exception cref="Exception">
        ///   The <see cref="BlackList"/> or <see cref="WhiteList"/> policies forbid it.
        /// </exception>
        public Peer RegisterPeer(Peer peer)
        {
            if (peer.Id == null)
            {
                throw new ArgumentNullException(nameof(peer));
            }

            if (peer.Id == LocalPeer.Id)
            {
                throw new ArgumentException("Cannot register self.");
            }

            //if (!IsAllowed(peer))
            //{
            //    throw new Exception($"Communication with '{peer}' is not allowed.");
            //}

            var isNew = false;
            var p = _otherPeers.AddOrUpdate(peer.Id.ToBase58(),
                id =>
                {
                    isNew = true;
                    return peer;
                },
                (id, existing) =>
                {
                    if (ReferenceEquals(existing, peer))
                    {
                        return existing;
                    }
                    
                    existing.AgentVersion = peer.AgentVersion ?? existing.AgentVersion;
                    existing.ProtocolVersion = peer.ProtocolVersion ?? existing.ProtocolVersion;
                    existing.PublicKey = peer.PublicKey ?? existing.PublicKey;
                    existing.Latency = peer.Latency ?? existing.Latency;
                    existing.Addresses = existing
                       .Addresses
                       .Union(peer.Addresses)
                       .ToList();

                    return existing;
                });

            if (!isNew)
            {
                return p;
            }

            if (_log.IsDebugEnabled)
            {
                _log.Debug($"New peer registerd {p}");
            }
            
            PeerDiscovered?.Invoke(this, p);

            return p;
        }

        /// <summary>
        ///   Deregister a peer.
        /// </summary>
        /// <param name="peer">
        ///   The peer to remove..
        /// </param>
        /// <remarks>
        ///   Remove all knowledge of the peer. The <see cref="PeerRemoved"/> event
        ///   is raised.
        /// </remarks>
        public void DeregisterPeer(Peer peer)
        {
            if (peer.Id == null)
            {
                throw new ArgumentNullException(nameof(peer));
            }

            if (_otherPeers.TryRemove(peer.Id.ToBase58(), out var found))
            {
                peer = found;
            }
            
            PeerRemoved?.Invoke(this, peer);
        }

        /// <summary>
        ///   Determines if a connection is being made to the peer.
        /// </summary>
        /// <param name="peer">
        ///   A <see cref="Peer"/>.
        /// </param>
        /// <returns>
        ///   <b>true</b> is the <paramref name="peer"/> has a pending connection.
        /// </returns>
        public bool HasPendingConnection(Peer peer) { return _pendingConnections.TryGetValue(peer, out _); }

        /// <summary>
        ///   The addresses that cannot be used.
        /// </summary>
        public MultiAddressBlackList BlackList { get; set; } = new MultiAddressBlackList();

        /// <summary>
        ///   The addresses that can be used.
        /// </summary>
        public MultiAddressWhiteList WhiteList { get; set; } = new MultiAddressWhiteList();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dhtService"></param>
        public SwarmService(IPeerRouting dhtService = null) { Router = dhtService; }

        /// <inheritdoc />
        public Task StartAsync()
        {
            if (LocalPeer == null)
            {
                throw new NotSupportedException("The LocalPeer is not defined.");
            }

            // Many of the unit tests do not setup the LocalPeerKey.  If
            // its missing, then just use plaintext connection.
            // TODO: make the tests setup the security protocols.
            if (LocalPeerKey == null)
            {
                lock (_protocols)
                {
                    var security = _protocols.OfType<IEncryptionProtocol>().ToArray();
                    foreach (var p in security)
                    {
                        _protocols.Remove(p);
                    }
                    
                    _protocols.Add(_plaintext1);
                }

                _log.Warn("Peer key is missing, using unencrypted connections.");
            }

            Manager.PeerDisconnected += OnPeerDisconnected;
            IsRunning = true;
            _swarmCancellation = new CancellationTokenSource();
            _log.Debug("Started");

            return Task.CompletedTask;
        }

        private void OnPeerDisconnected(object sender, MultiHash peerId)
        {
            if (!_otherPeers.TryGetValue(peerId.ToBase58(), out var peer))
            {
                peer = new Peer {Id = peerId};
            }
            
            PeerDisconnected?.Invoke(this, peer);
        }

        /// <inheritdoc />
        public async Task StopAsync()
        {
            IsRunning = false;
            _swarmCancellation?.Cancel(true);

            _log.Debug($"Stopping {LocalPeer}");

            // Stop the listeners.
            while (_listeners.Count > 0)
            {
                await StopListeningAsync(_listeners.Keys.First()).ConfigureAwait(false);
            }

            // Disconnect from remote peers.
            Manager.Clear();
            Manager.PeerDisconnected -= OnPeerDisconnected;

            _otherPeers.Clear();
            _listeners.Clear();
            _pendingConnections.Clear();
            _pendingRemoteConnections.Clear();
            BlackList = new MultiAddressBlackList();
            WhiteList = new MultiAddressWhiteList();

            _log.Debug($"Stopped {LocalPeer}");
        }

        /// <summary>
        ///   Connect to a peer using the specified <see cref="MultiAddress"/>.
        /// </summary>
        /// <param name="address">
        ///   An ipfs <see cref="MultiAddress"/>, such as
        ///  <c>/ip4/104.131.131.82/tcp/4001/ipfs/QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ</c>.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result
        ///   is the <see cref="PeerConnection"/>.
        /// </returns>
        /// <remarks>
        ///   If already connected to the peer and is active on any address, then
        ///   the existing connection is returned.
        /// </remarks>
        public async Task<PeerConnection> ConnectAsync(MultiAddress address,
            CancellationToken cancel = default)
        {
            var peer = RegisterPeerAddress(address);
            return await ConnectAsync(peer, cancel).ConfigureAwait(false);
        }

        /// <summary>
        ///   Connect to a peer.
        /// </summary>
        /// <param name="peer">
        ///  A peer to connect to.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result
        ///   is the <see cref="PeerConnection"/>.
        /// </returns>
        /// <remarks>
        ///   If already connected to the peer and is active on any address, then
        ///   the existing connection is returned.
        /// </remarks>
        public async Task<PeerConnection> ConnectAsync(Peer peer, CancellationToken cancel = default)
        {
            if (!IsRunning)
            {
                throw new Exception("The swarm is not running.");
            }

            peer = RegisterPeer(peer);

            // If connected and still open, then use the existing connection.
            if (Manager.TryGet(peer, out var conn))
            {
                return conn;
            }

            // Use a current connection attempt to the peer or create a new one.
            try
            {
                using (var cts = CancellationTokenSource.CreateLinkedTokenSource(_swarmCancellation.Token, cancel))
                {
                    return await _pendingConnections
                       .GetOrAdd(peer,
                            key => new AsyncLazy<PeerConnection>(() => DialAsync(peer, peer.Addresses, cts.Token)))
                       .ConfigureAwait(false);
                }
            }
            catch (Exception)
            {
                PeerNotReachable?.Invoke(this, peer);
                throw;
            }
            finally
            {
                _pendingConnections.TryRemove(peer, out _);
            }
        }

        /// <summary>
        ///   Create a stream to the peer that talks the specified protocol.
        /// </summary>
        /// <param name="peer">
        ///   The remote peer.
        /// </param>
        /// <param name="protocol">
        ///   The protocol name, such as "/foo/0.42.0".
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result
        ///   is the new <see cref="Stream"/> to the <paramref name="peer"/>.
        /// </returns>
        /// <remarks>
        ///   <para>
        ///   When finished, the caller must <see cref="Stream.Dispose()"/> the
        ///   new stream.
        ///   </para>
        /// </remarks>
        public async Task<Stream> DialAsync(Peer peer,
            string protocol,
            CancellationToken cancel = default)
        {
            peer = RegisterPeer(peer);

            // Get a connection and then a muxer to the peer.
            var connection = await ConnectAsync(peer, cancel).ConfigureAwait(false);
            var muxer = await connection.MuxerEstablished.Task.ConfigureAwait(false);

            // Create a new stream for the peer protocol.
            var stream = await muxer.CreateStreamAsync(protocol, cancel).ConfigureAwait(false);
            try
            {
                await connection.EstablishProtocolAsync("/multistream/", stream, cancel).ConfigureAwait(false);

                await Message.WriteAsync(protocol, stream, cancel).ConfigureAwait(false);
                var result = await Message.ReadStringAsync(stream, cancel).ConfigureAwait(false);
                if (result != protocol) throw new Exception($"Protocol '{protocol}' not supported by '{peer}'.");

                return stream;
            }
            catch (Exception)
            {
                stream?.DisposeAsync();
                throw;
            }
        }

        /// <summary>
        ///   Establish a duplex stream between the local and remote peer.
        /// </summary>
        /// <param name="remote"></param>
        /// <param name="addrs"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        private async Task<PeerConnection> DialAsync(Peer remote,
            IEnumerable<MultiAddress> addrs,
            CancellationToken cancel)
        {
            _log.Debug($"Dialing {remote}");

            if (remote == LocalPeer) throw new Exception("Cannot dial self.");

            // If no addresses, then ask peer routing.
            var multiAddresses = addrs.ToList();
            if (Router != null && !multiAddresses.Any())
            {
                var found = await Router.FindPeerAsync(remote.Id, cancel).ConfigureAwait(false);
                addrs = found.Addresses;
                remote.Addresses = addrs;
            }

            // Get the addresses we can use to dial the remote.  Filter
            // out any addresses (ip and port) we are listening on.
            var blackList = _listeners.Keys
               .Select(a => a.WithoutPeerId())
               .ToArray();
            
            var possibleAddresses =
                (await Task.WhenAll(multiAddresses.Select(a => a.ResolveAsync(cancel))).ConfigureAwait(false))
               .SelectMany(a => a)
               .Where(a => !blackList.Contains(a.WithoutPeerId()))
               .Select(a => a.WithPeerId(remote.Id))
               .Distinct()
               .ToArray();
               
            if (possibleAddresses.Length == 0)
            {
                throw new Exception($"{remote} has no known or reachable address.");
            }

            // Try the various addresses in parallel.  The first one to complete wins.
            PeerConnection connection = null;
            try
            {
                using var timeout = new CancellationTokenSource(TransportConnectionTimeout);
                using (var cts = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, cancel))
                {
                    var attempts = possibleAddresses
                       .Select(a => DialAsync(remote, a, cts.Token));
                    connection = await TaskHelper.WhenAnyResultAsync(attempts, cts.Token).ConfigureAwait(false);
                    await cts.CancelAsync(); // stop other dialing tasks.
                }
            }
            catch (Exception e)
            {
                var attemped = string.Join(", ", possibleAddresses.Select(a => a.ToString()));
                _log.Trace($"Cannot dial {attemped}");
                throw new Exception($"Cannot dial {remote}.", e);
            }

            // Do the connection handshake.
            try
            {
                MountProtocols(connection);
                IEncryptionProtocol[] security;
                lock (_protocols)
                {
                    security = _protocols.OfType<IEncryptionProtocol>().ToArray();
                }

                await connection.InitiateAsync(security, cancel).ConfigureAwait(false);
                await connection.MuxerEstablished.Task.ConfigureAwait(false);
                Identify1 identify;
                lock (_protocols)
                {
                    identify = _protocols.OfType<Identify1>().First();
                }

                await identify.GetRemotePeerAsync(connection, cancel).ConfigureAwait(false);
            }
            catch (Exception)
            {
                connection.Dispose();
                throw;
            }

            var actual = Manager.Add(connection);
            if (actual == connection)
            {
                ConnectionEstablished?.Invoke(this, connection);
            }

            return actual;
        }

        private async Task<PeerConnection> DialAsync(Peer remote, MultiAddress addr, CancellationToken cancel)
        {
            // TODO: HACK: Currenty only the ipfs/p2p is supported.
            // short circuit to make life faster.
            if (addr.Protocols.Count != 3
             || !(addr.Protocols[2].Name == "ipfs" || addr.Protocols[2].Name == "p2p"))
            {
                throw new Exception($"Cannnot dial; unknown protocol in '{addr}'.");
            }

            // Establish the transport stream.
            Stream stream = null;
            foreach (var protocol in addr.Protocols)
            {
                cancel.ThrowIfCancellationRequested();
                if (!TransportRegistry.Transports.TryGetValue(protocol.Name, out var transport))
                {
                    continue;
                }
                
                stream = await transport().ConnectAsync(addr, cancel).ConfigureAwait(false);
                if (cancel.IsCancellationRequested)
                {
                    stream?.DisposeAsync();
                    continue;
                }

                break;
            }

            if (stream == null)
            {
                throw new Exception("Missing a known transport protocol name.");
            }

            // Build the connection.
            var connection = new PeerConnection
            {
                IsIncoming = false,
                LocalPeer = LocalPeer,

                // TODO: LocalAddress
                LocalPeerKey = LocalPeerKey,
                RemotePeer = remote,
                RemoteAddress = addr,
                Stream = stream
            };

            // Are we communicating to a private network?
            if (NetworkProtector != null)
            {
                connection.Stream = await NetworkProtector.ProtectAsync(connection, cancel).ConfigureAwait(false);
            }

            return connection;
        }

        /// <summary>
        ///   Disconnect from a peer.
        /// </summary>
        /// <param name="address">
        ///   An ipfs <see cref="MultiAddress"/>, such as
        ///  <c>/ip4/104.131.131.82/tcp/4001/ipfs/QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ</c>.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation.
        /// </returns>
        /// <remarks>
        ///   If the peer is not conected, then nothing happens.
        /// </remarks>
        public Task DisconnectAsync(MultiAddress address, CancellationToken cancel = default)
        {
            Manager.Remove(address.PeerId);
            return Task.CompletedTask;
        }

        /// <summary>
        ///   Start listening on the specified <see cref="MultiAddress"/>.
        /// </summary>
        /// <param name="address">
        ///   Typically "/ip4/0.0.0.0/tcp/4001" or "/ip6/::/tcp/4001".
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation.  The task's result
        ///   is a <see cref="MultiAddress"/> than can be used by another peer
        ///   to connect to tis peer.
        /// </returns>
        /// <exception cref="Exception">
        ///   Already listening on <paramref name="address"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="address"/> is missing a transport protocol (such as tcp or udp).
        /// </exception>
        /// <remarks>
        ///   Allows other peers to <see cref="ConnectAsync(MultiAddress, CancellationToken)">connect</see>
        ///   to the <paramref name="address"/>.
        ///   <para>
        ///   The <see cref="Peer.Addresses"/> of the <see cref="LocalPeer"/> are updated.  If the <paramref name="address"/> refers to
        ///   any IP address ("/ip4/0.0.0.0" or "/ip6/::") then all network interfaces addresses
        ///   are added.  If the port is zero (as in "/ip6/::/tcp/0"), then the peer addresses contains the actual port number
        ///   that was assigned.
        ///   </para>
        /// </remarks>
        public Task<MultiAddress> StartListeningAsync(MultiAddress address)
        {
            var cancel = new CancellationTokenSource();

            if (!_listeners.TryAdd(address, cancel))
            {
                throw new Exception($"Already listening on '{address}'.");
            }

            // Start a listener for the transport
            var didSomething = false;
            foreach (var protocol in address.Protocols)
                if (TransportRegistry.Transports.TryGetValue(protocol.Name, out var transport))
                {
                    address = transport().Listen(address, OnRemoteConnect, cancel.Token);
                    _listeners.TryAdd(address, cancel);
                    didSomething = true;
                    break;
                }

            if (!didSomething)
            {
                throw new ArgumentException($"Missing a transport protocol name '{address}'.", nameof(address));
            }

            var result = new MultiAddress($"{address}/ipfs/{LocalPeer.Id}");

            // Get the actual IP address(es).
            IEnumerable<MultiAddress> addresses = new List<MultiAddress>();
            var ips = NetworkInterface.GetAllNetworkInterfaces()

                // It appears that the loopback adapter is not UP on Ubuntu 14.04.5 LTS
               .Where(nic => nic.OperationalStatus == OperationalStatus.Up
                 || nic.NetworkInterfaceType == NetworkInterfaceType.Loopback)
               .SelectMany(nic => nic.GetIPProperties().UnicastAddresses);
            if (result.ToString().StartsWith("/ip4/0.0.0.0/"))
            {
                addresses = ips
                   .Where(ip => ip.Address.AddressFamily == AddressFamily.InterNetwork)
                   .Select(ip => new MultiAddress(result.ToString().Replace("0.0.0.0", ip.Address.ToString())))
                   .ToArray();
            }
            else if (result.ToString().StartsWith("/ip6/::/"))
            {
                addresses = ips
                   .Where(ip => ip.Address.AddressFamily == AddressFamily.InterNetworkV6)
                   .Select(ip => { return new MultiAddress(result.ToString().Replace("::", ip.Address.ToString())); })
                   .ToArray();
            }
            else
            {
                addresses = new[] {result};
            }
            
            if (!addresses.Any())
            {
                var msg = "Cannot determine address(es) for " + result;
                
                foreach (var ip in ips)
                {
                    msg += " nic-ip: " + ip.Address;
                }
                
                cancel.Cancel();
                throw new Exception(msg);
            }

            // Add actual addresses to listeners and local peer addresses.
            foreach (var a in addresses)
            {
                _log.Debug($"Listening on {a}");
                _listeners.TryAdd(a, cancel);
            }

            LocalPeer.Addresses = LocalPeer
               .Addresses
               .Union(addresses)
               .ToArray();

            ListenerEstablished?.Invoke(this, LocalPeer);
            return Task.FromResult(addresses.First());
        }

#pragma warning disable VSTHRD100 // Avoid async void methods
        /// <summary>
        ///   Called when a remote peer is connecting to the local peer.
        /// </summary>
        /// <param name="stream">
        ///   The stream to the remote peer.
        /// </param>
        /// <param name="local">
        ///   The local peer's address.
        /// </param>
        /// <param name="remote">
        ///   The remote peer's address.
        /// </param>
        /// <remarks>
        ///   Establishes the protocols of the connection.  Any exception is simply
        ///   logged as warning.
        /// </remarks>
        private async void OnRemoteConnect(Stream stream, MultiAddress local, MultiAddress remote)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            if (!IsRunning)
            {
                try
                {
                    stream.Dispose();
                }
                catch (Exception)
                {
                    // eat it.
                }

                return;
            }

            // If the remote is already trying to establish a connection, then we
            // can just refuse this one.
            if (!_pendingRemoteConnections.TryAdd(remote, null))
            {
                _log.Debug($"Duplicate remote connection from {remote}");
                try
                {
                    stream.Dispose();
                }
                catch (Exception)
                {
                    // eat it.
                }

                return;
            }

            try
            {
                if (_log.IsDebugEnabled)
                {
                    _log.Debug($"remote connect from {remote}");
                }

                // TODO: Check the policies

                var connection = new PeerConnection
                {
                    IsIncoming = true,
                    LocalPeer = LocalPeer,
                    LocalAddress = local,
                    LocalPeerKey = LocalPeerKey,
                    RemoteAddress = remote,
                    Stream = stream
                };

                // Are we communicating to a private network?
                if (NetworkProtector != null)
                {
                    connection.Stream = await NetworkProtector.ProtectAsync(connection).ConfigureAwait(false);
                }

                // Mount the protocols.
                MountProtocols(connection);

                // Start the handshake
                // TODO: Isn't connection cancel token required.
                _ = connection.ReadMessagesAsync(default);

                // Wait for security to be established.
                await connection.SecurityEstablished.Task.ConfigureAwait(false);

                // TODO: Maybe connection.LocalPeerKey = null;

                // Wait for the handshake to complete.
                await connection.MuxerEstablished.Task;

                // Need details on the remote peer.
                Identify1 identify;
                lock (_protocols)
                {
                    identify = _protocols.OfType<Identify1>().First();
                }

                connection.RemotePeer = await identify.GetRemotePeerAsync(connection, default)
                   .ConfigureAwait(false);

                connection.RemotePeer = RegisterPeer(connection.RemotePeer);
                connection.RemoteAddress = new MultiAddress($"{remote}/ipfs/{connection.RemotePeer.Id}");
                var actual = Manager.Add(connection);
                if (actual == connection)
                {
                    ConnectionEstablished?.Invoke(this, connection);
                }
            }
            catch (Exception e)
            {
                if (_log.IsDebugEnabled)
                {
                    _log.Debug($"remote connect from {remote} failed: {e.Message}");
                }
                
                try
                {
                    stream.Dispose();
                }
                catch (Exception)
                {
                    // ignore
                }
            }
            finally
            {
                _pendingRemoteConnections.TryRemove(remote, out _);
            }
        }

        /// <summary>
        ///   Add a protocol that is supported by the swarm.
        /// </summary>
        /// <param name="protocol">
        ///   The protocol to add.
        /// </param>
        public void AddProtocol(IPeerProtocol protocol)
        {
            lock (_protocols)
            {
                _protocols.Add(protocol);
            }
        }

        /// <summary>
        ///   Remove a protocol from the swarm.
        /// </summary>
        /// <param name="protocol">
        ///   The protocol to remove.
        /// </param>
        public void RemoveProtocol(IPeerProtocol protocol)
        {
            lock (_protocols)
            {
                _protocols.Remove(protocol);
            }
        }

        private void MountProtocols(PeerConnection connection)
        {
            lock (_protocols)
            {
                connection.AddProtocols(_protocols);
            }
        }

        /// <summary>
        ///   Stop listening on the specified <see cref="MultiAddress"/>.
        /// </summary>
        /// <param name="address"></param>
        /// <returns>
        ///   A task that represents the asynchronous operation.
        /// </returns>
        /// <remarks>
        ///   Allows other peers to <see cref="ConnectAsync(MultiAddress, CancellationToken)">connect</see>
        ///   to the <paramref name="address"/>.
        ///   <para>
        ///   The addresses of the <see cref="LocalPeer"/> are updated.
        ///   </para>
        /// </remarks>
        public async Task StopListeningAsync(MultiAddress address)
        {
            if (!_listeners.TryRemove(address, out var listener))
            {
                return;
            }

            try
            {
                if (!listener.IsCancellationRequested)
                {
                    listener.Cancel(false);
                }

                // Remove any local peer address that depend on the cancellation token.
                var others = _listeners
                   .Where(l => l.Value == listener)
                   .Select(l => l.Key)
                   .ToArray();

                LocalPeer.Addresses = LocalPeer.Addresses
                   .Where(a => a != address)
                   .Where(a => !others.Contains(a))
                   .ToArray();

                foreach (var other in others)
                {
                    _listeners.TryRemove(other, out _);
                }

                // Give some time away, so that cancel can run
                // TODO: Would be nice to make this deterministic.
                await Task.Delay(TimeSpan.FromMilliseconds(100)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _log.Error("stop listening failed", e);
            }
        }

        /// <inheritdoc />
        public bool IsAllowed(MultiAddress target)
        {
            return BlackList.IsAllowed(target)
             && WhiteList.IsAllowed(target);
        }

        /// <inheritdoc />
        public bool IsAllowed(Peer peer) { return peer.Addresses.All(IsAllowed); }
    }
}
