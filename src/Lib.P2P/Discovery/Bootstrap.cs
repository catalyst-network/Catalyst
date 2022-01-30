#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Logging;
using MultiFormats;

namespace Lib.P2P.Discovery
{
    /// <summary>
    ///   Discovers the pre-configured peers.
    /// </summary>
    public class Bootstrap : IPeerDiscovery
    {
        private static ILog _log = LogManager.GetLogger(typeof(Bootstrap));

        /// <inheritdoc />
        public event EventHandler<Peer> PeerDiscovered;

        /// <summary>
        ///   The addresses of the pre-configured peers.
        /// </summary>
        /// <value>
        ///   Each address must end with the ipfs protocol and the public ID
        ///   of the peer.  For example "/ip4/104.131.131.82/tcp/4001/ipfs/QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ"
        /// </value>
        public IEnumerable<MultiAddress> Addresses { get; set; }

        /// <inheritdoc />
        public Task StartAsync()
        {
            _log.Debug("Starting");
            if (Addresses == null)
            {
                _log.Warn("No bootstrap addresses");
                return Task.CompletedTask;
            }

            var peers = Addresses
               .Where(a => a.HasPeerId)
               .GroupBy(
                    a => a.PeerId,
                    a => a,
                    (key, g) => new Peer {Id = key, Addresses = g.ToList()});
            foreach (var peer in peers)
                try
                {
                    PeerDiscovered?.Invoke(this, peer);
                }
                catch (Exception e)
                {
                    _log.Error(e);
                }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task StopAsync()
        {
            _log.Debug("Stopping");
            PeerDiscovered = null;
            return Task.CompletedTask;
        }
    }
}
