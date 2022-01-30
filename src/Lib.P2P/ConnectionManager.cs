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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using MultiFormats;

namespace Lib.P2P
{
    /// <summary>
    ///   Manages the peer connections in a <see cref="SwarmService"/>.
    /// </summary>
    /// <remarks>
    ///   Enforces that only one connection exists to the <see cref="Peer"/>.  This
    ///   prevents the race condition when two simultaneously connect to each other.
    ///   <para>
    ///   TODO: Enforces a maximum number of open connections.
    ///   </para>
    /// </remarks>
    public sealed class ConnectionManager
    {
        /// <summary>
        ///   The connections to other peers. Key is the base58 hash of the peer ID.
        /// </summary>
        private readonly ConcurrentDictionary<string, List<PeerConnection>> _connections =
            new();

        private static string Key(Peer peer) { return peer.Id.ToBase58(); }

        private static string Key(MultiHash id) { return id.ToBase58(); }

        /// <summary>
        ///   Raised when a peer's connection is closed.
        /// </summary>
        public event EventHandler<MultiHash> PeerDisconnected;

        /// <summary>
        ///   Gets the current active connections.
        /// </summary>
        public IEnumerable<PeerConnection> Connections =>
            _connections.Values
               .SelectMany(c => c)
               .Where(c => c.IsActive);

        /// <summary>
        ///   Determines if a connection exists to the specified peer.
        /// </summary>
        /// <param name="peer">
        ///   Another peer.
        /// </param>
        /// <returns>
        ///   <b>true</b> if there is a connection to the <paramref name="peer"/> and
        ///   the connection is active; otherwise <b>false</b>.
        /// </returns>
        public bool IsConnected(Peer peer) { return TryGet(peer, out _); }

        /// <summary>
        ///    Gets the connection to the peer.
        /// </summary>
        /// <param name="peer">
        ///   A peer.
        /// </param>
        /// <param name="connection">
        ///   The connection to the peer.
        /// </param>
        /// <returns>
        ///   <b>true</b> if a connection exists; otherwise <b>false</b>.
        /// </returns>
        /// <remarks>
        ///   If the connection's underlaying <see cref="PeerConnection.Stream"/>
        ///   is closed, then the connection is removed.
        /// </remarks>
        internal bool TryGet(Peer peer, out PeerConnection connection)
        {
            connection = null;
            if (!_connections.TryGetValue(Key(peer), out var conns))
            {
                return false;
            }

            connection = conns
               .FirstOrDefault(c => c.IsActive);

            return connection != null;
        }

        /// <summary>
        ///   Adds a new connection.
        /// </summary>
        /// <param name="connection">
        ///   The <see cref="PeerConnection"/> to add.
        /// </param>
        /// <returns>
        ///   The connection that should be used.
        /// </returns>
        /// <remarks>
        ///   If a connection already exists to the peer, the specified
        ///   <paramref name="connection"/> is closed and existing connection
        ///   is returned.
        /// </remarks>
        public PeerConnection Add(PeerConnection connection)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            if (connection.RemotePeer == null)
            {
                throw new ArgumentNullException(nameof(connection.RemotePeer));
            }

            if (connection.RemotePeer.Id == null)
            {
                throw new ArgumentNullException(nameof(connection.RemotePeer.Id));
            }

            _connections.AddOrUpdate(
                Key(connection.RemotePeer),
                key => new List<PeerConnection> {connection},
                (key, conns) =>
                {
                    if (!conns.Contains(connection))
                    {
                        conns.Add(connection);
                    }
                    
                    return conns;
                }
            );

            if (connection.RemotePeer.ConnectedAddress == null)
            {
                connection.RemotePeer.ConnectedAddress = connection.RemoteAddress;
            }
            
            connection.Closed += (s, e) => Remove(e);
            return connection;
        }

        /// <summary>
        ///   Remove a connection.
        /// </summary>
        /// <param name="connection">
        ///   The <see cref="PeerConnection"/> to remove.
        /// </param>
        /// <returns>
        ///   <b>true</b> if the connection was removed; otherwise, <b>false</b>.
        /// </returns>
        /// <remarks>
        ///    The <paramref name="connection"/> is removed from the list of
        ///    connections and is closed.
        /// </remarks>
        public bool Remove(PeerConnection connection)
        {
            if (connection == null)
            {
                return false;
            }

            if (!_connections.TryGetValue(Key(connection.RemotePeer), out var originalConns))
            {
                connection.Dispose();
                return false;
            }

            if (!originalConns.Contains(connection))
            {
                connection.Dispose();
                return false;
            }

            List<PeerConnection> newConns = new();
            newConns.AddRange(originalConns.Where(c => c != connection));
            _connections.TryUpdate(Key(connection.RemotePeer), newConns, originalConns);

            connection.Dispose();
            if (newConns.Count > 0)
            {
                var last = newConns.Last();
                last.RemotePeer.ConnectedAddress = last.RemoteAddress;
            }
            else
            {
                connection.RemotePeer.ConnectedAddress = null;
                PeerDisconnected?.Invoke(this, connection.RemotePeer.Id);
            }

            return true;
        }

        /// <summary>
        ///   Remove and close all connection to the peer ID.
        /// </summary>
        /// <param name="id">
        ///   The ID of a <see cref="Peer"/> to remove.
        /// </param>
        /// <returns>
        ///   <b>true</b> if a connection was removed; otherwise, <b>false</b>.
        /// </returns>
        public bool Remove(MultiHash id)
        {
            if (!_connections.TryRemove(Key(id), out var conns))
            {
                return false;
            }
            
            foreach (var conn in conns)
            {
                conn.RemotePeer.ConnectedAddress = null;
                conn.Dispose();
            }

            PeerDisconnected?.Invoke(this, id);
            return true;
        }

        /// <summary>
        ///   Removes and closes all connections.
        /// </summary>
        public void Clear()
        {
            var conns = _connections.Values.SelectMany(c => c).ToArray();
            foreach (var conn in conns)
            {
                Remove(conn);
            }
        }
    }
}
