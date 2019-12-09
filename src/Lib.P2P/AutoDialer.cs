using System;
using System.Linq;
using System.Threading;
using Common.Logging;

namespace Lib.P2P
{
    /// <summary>
    ///   Maintains a minimum number of peer connections.
    /// </summary>
    /// <remarks>
    ///   Listens to the <see cref="SwarmService"/> and automically dials a
    ///   new <see cref="Peer"/> when required.
    /// </remarks>
    public class AutoDialer : IDisposable
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(AutoDialer));

        /// <summary>
        ///   The default minimum number of connections to maintain (16).
        /// </summary>
        public const int DefaultMinConnections = 16;

        private readonly SwarmService _swarmService;
        private int pendingConnects;

        /// <summary>
        ///   Creates a new instance of the <see cref="AutoDialer"/> class.
        /// </summary>
        /// <param name="swarmService">
        ///   Provides access to other peers.
        /// </param>
        public AutoDialer(SwarmService swarmService)
        {
            this._swarmService = swarmService;
            swarmService.PeerDiscovered += OnPeerDiscovered;
            swarmService.PeerDisconnected += OnPeerDisconnected;
        }

        /// <summary>
        ///  Releases the unmanaged and optionally managed resources.
        /// </summary>
        /// <param name="disposing">
        ///   <b>true</b> to release both managed and unmanaged resources; <b>false</b> 
        ///   to release only unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _swarmService.PeerDiscovered -= OnPeerDiscovered;
                _swarmService.PeerDisconnected -= OnPeerDisconnected;
            }
        }

        /// <summary>
        ///   Performs application-defined tasks associated with freeing, 
        ///   releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() { Dispose(true); }

        /// <summary>
        ///   The low water mark for peer connections.
        /// </summary>
        /// <value>
        ///   Defaults to <see cref="DefaultMinConnections"/>.
        /// </value>
        /// <remarks>
        ///   Setting this to zero will basically disable the auto dial features.
        /// </remarks>
        public int MinConnections { get; set; } = DefaultMinConnections;

#pragma warning disable VSTHRD100 // Avoid async void methods
        /// <summary>
        ///   Called when the swarm has a new peer.
        /// </summary>
        /// <param name="sender">
        ///   The swarm of peers.
        /// </param>
        /// <param name="peer">
        ///   The peer that was discovered.
        /// </param>
        /// <remarks>
        ///   If the <see cref="MinConnections"/> is not reached, then the
        ///   <paramref name="peer"/> is dialed.
        /// </remarks>
        private async void OnPeerDiscovered(object sender, Peer peer)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            var n = _swarmService.Manager.Connections.Count() + pendingConnects;
            if (_swarmService.IsRunning && n < MinConnections)
            {
                Interlocked.Increment(ref pendingConnects);
                log.Debug($"Dialing new {peer}");
                try
                {
                    await _swarmService.ConnectAsync(peer).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    log.Warn($"Failed to dial {peer}");
                }
                finally
                {
                    Interlocked.Decrement(ref pendingConnects);
                }
            }
        }

#pragma warning disable VSTHRD100 // Avoid async void methods
        /// <summary>
        ///   Called when the swarm has lost a connection to a peer.
        /// </summary>
        /// <param name="sender">
        ///   The swarm of peers.
        /// </param>
        /// <param name="disconnectedPeer">
        ///   The peer that was disconnected.
        /// </param>
        /// <remarks>
        ///   If the <see cref="MinConnections"/> is not reached, then another
        ///   peer is dialed.
        /// </remarks>
        private async void OnPeerDisconnected(object sender, Peer disconnectedPeer)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            var n = _swarmService.Manager.Connections.Count() + pendingConnects;
            if (!_swarmService.IsRunning || n >= MinConnections)
                return;

            // Find a random peer to connect with.
            var peers = _swarmService.KnownPeers
               .Where(p => p.ConnectedAddress == null)
               .Where(p => p != disconnectedPeer)
               .Where(p => _swarmService.IsAllowed(p))
               .Where(p => !_swarmService.HasPendingConnection(p))
               .ToArray();
            if (peers.Length == 0)
                return;
            var rng = new Random();
            var peer = peers[rng.Next(peers.Count())];

            Interlocked.Increment(ref pendingConnects);
            log.Debug($"Dialing {peer}");
            try
            {
                await _swarmService.ConnectAsync(peer).ConfigureAwait(false);
            }
            catch (Exception)
            {
                log.Warn($"Failed to dial {peer}");
            }
            finally
            {
                Interlocked.Decrement(ref pendingConnects);
            }
        }
    }
}
