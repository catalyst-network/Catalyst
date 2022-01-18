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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;

namespace Lib.P2P
{
    /// <summary>
    ///   Manages the peers.
    /// </summary>
    /// <remarks>
    ///    Listens to the <see cref="SwarmService"/> events to determine the state
    ///    of a peer.
    /// </remarks>
    public class PeerManager : IService
    {
        private static ILog _log = LogManager.GetLogger(typeof(PeerManager));
        private CancellationTokenSource _cancel;

        /// <summary>
        ///   Initial time to wait before attempting a reconnection
        ///   to a dead peer.
        /// </summary>
        /// <value>
        ///   Defaults to 1 minute.
        /// </value>
        public TimeSpan InitialBackoff = TimeSpan.FromMinutes(1);

        /// <summary>
        ///   When reached, the peer is considered permanently dead.
        /// </summary>
        /// <value>
        ///   Defaults to 64 minutes.
        /// </value>
        public TimeSpan MaxBackoff = TimeSpan.FromMinutes(64);

        /// <summary>
        ///   Provides access to other peers.
        /// </summary>
        public ISwarmService SwarmService { get; set; }

        /// <summary>
        ///   The peers that are reachable.
        /// </summary>
        public ConcurrentDictionary<Peer, DeadPeer> DeadPeers = new();

        /// <inheritdoc />
        public Task StartAsync()
        {
            SwarmService.ConnectionEstablished += Swarm_ConnectionEstablished;
            SwarmService.PeerNotReachable += Swarm_PeerNotReachable;

            _cancel = new CancellationTokenSource();
            var _ = PhoenixAsync(_cancel.Token);

            _log.Debug("started");
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task StopAsync()
        {
            SwarmService.ConnectionEstablished -= Swarm_ConnectionEstablished;
            SwarmService.PeerNotReachable -= Swarm_PeerNotReachable;
            DeadPeers.Clear();

            _cancel.Cancel();
            _cancel.Dispose();

            _log.Debug("stopped");
            return Task.CompletedTask;
        }

        /// <summary>
        ///   Indicates that the peer can not be connected to.
        /// </summary>
        /// <param name="peer"></param>
        public void SetNotReachable(Peer peer)
        {
            var dead = DeadPeers.AddOrUpdate(peer,
                new DeadPeer
                {
                    Peer = peer,
                    Backoff = InitialBackoff,
                    NextAttempt = DateTime.Now + InitialBackoff
                },
                (key, existing) =>
                {
                    existing.Backoff += existing.Backoff;
                    existing.NextAttempt = existing.Backoff <= MaxBackoff
                        ? DateTime.Now + existing.Backoff
                        : DateTime.MaxValue;
                    return existing;
                });

            SwarmService.BlackList.Add($"/p2p/{peer.Id}");
            if (dead.NextAttempt != DateTime.MaxValue)
            {
                _log.DebugFormat("Dead '{0}' for {1} minutes.", dead.Peer, dead.Backoff.TotalMinutes);
            }
            else
            {
                SwarmService.DeregisterPeer(dead.Peer);
                _log.DebugFormat("Permanently dead '{0}'.", dead.Peer);
            }
        }

        /// <summary>
        ///   Indicates that the peer can be connected to.
        /// </summary>
        /// <param name="peer"></param>
        public void SetReachable(Peer peer)
        {
            _log.DebugFormat("Alive '{0}'.", peer);

            DeadPeers.TryRemove(peer, out _);
            SwarmService.BlackList.Remove($"/p2p/{peer.Id}");
        }

        /// <summary>
        ///   Is invoked by the <see cref="SwarmService"/> when a peer can not be connected to.
        /// </summary>
        private void Swarm_PeerNotReachable(object sender, Peer peer) { SetNotReachable(peer); }

        /// <summary>
        ///   Is invoked by the <see cref="SwarmService"/> when a peer is connected to.
        /// </summary>
        private void Swarm_ConnectionEstablished(object sender, PeerConnection connection)
        {
            SetReachable(connection.RemotePeer);
        }

        /// <summary>
        ///   Background process to try reconnecting to a dead peer.
        /// </summary>
        private async Task PhoenixAsync(CancellationToken cancellation)
        {
            while (!cancellation.IsCancellationRequested)
                try
                {
                    await Task.Delay(InitialBackoff, cancellation);
                    var now = DateTime.Now;
                    await DeadPeers.Values
                       .Where(p => p.NextAttempt < now)
                       .ParallelForEachAsync(async dead =>
                        {
                            _log.DebugFormat("Attempt reconnect to {0}", dead.Peer);
                            SwarmService.BlackList.Remove($"/p2p/{dead.Peer.Id}");
                            try
                            {
                                await SwarmService.ConnectAsync(dead.Peer, _cancel.Token);
                            }
                            catch
                            {
                                // eat it
                            }
                        }, 10);
                }
                catch
                {
                    // eat it.
                }
        }
    }

    /// <summary>
    ///   Information on a peer that is not reachable.
    /// </summary>
    public class DeadPeer
    {
        /// <summary>
        ///   The peer that does not respond.
        /// </summary>
        public Peer Peer { get; set; }

        /// <summary>
        ///   How long to wait before attempting another connect.
        /// </summary>
        public TimeSpan Backoff { get; set; }

        /// <summary>
        ///   When another connect should be tried.
        /// </summary>
        public DateTime NextAttempt { get; set; }
    }
}
