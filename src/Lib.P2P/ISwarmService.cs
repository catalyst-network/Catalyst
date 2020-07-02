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
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Lib.P2P.Cryptography;
using Lib.P2P.Protocols;
using Lib.P2P.Routing;
using MultiFormats;

namespace Lib.P2P
{
    /// <summary>
    /// 
    /// </summary>
    public interface ISwarmService : IService, IPolicy<MultiAddress>, IPolicy<Peer>
    {
        /// <summary>
        ///   Raised when a listener is establihed.
        /// </summary>
        /// <remarks>
        ///   Raised when <see cref="SwarmService.StartListeningAsync"/>
        ///   succeeds.
        /// </remarks>
        event EventHandler<Peer> ListenerEstablished;

        /// <summary>
        ///   Raised when a connection to another peer is established.
        /// </summary>
        event EventHandler<PeerConnection> ConnectionEstablished;

        /// <summary>
        ///   Raised when a new peer is discovered for the first time.
        /// </summary>
        event EventHandler<Peer> PeerDiscovered;

        /// <summary>
        ///   Raised when a peer's connection is closed.
        /// </summary>
        event EventHandler<Peer> PeerDisconnected;

        /// <summary>
        ///   Raised when a peer should no longer be used.
        /// </summary>
        /// <remarks>
        ///   This event indicates that the peer has been removed
        ///   from the <see cref="SwarmService.KnownPeers"/> and should no longer
        ///   be used.
        /// </remarks>
        event EventHandler<Peer> PeerRemoved;

        /// <summary>
        ///   Raised when a peer cannot be connected to.
        /// </summary>
        event EventHandler<Peer> PeerNotReachable;

        /// <summary>
        ///  The local peer.
        /// </summary>
        /// <value>
        ///   The local peer must have an <see cref="Peer.Id"/> and
        ///   <see cref="Peer.PublicKey"/>.
        /// </value>
        Peer LocalPeer { get; set; }

        /// <summary>
        ///   The private key of the local peer.
        /// </summary>
        /// <value>
        ///   Used to prove the identity of the <see cref="LocalPeer"/>.
        /// </value>
        Key LocalPeerKey { get; set; }

        /// <summary>
        ///   Use to find addresses of a peer.
        /// </summary>
        IPeerRouting Router { get; set; }

        /// <summary>
        ///   Provides access to a private network of peers.
        /// </summary>
        INetworkProtector NetworkProtector { get; set; }

        /// <summary>
        ///   Determines if the swarm has been started.
        /// </summary>
        /// <value>
        ///   <b>true</b> if the swarm has started; otherwise, <b>false</b>.
        /// </value>
        bool IsRunning { get; }

        /// <summary>
        ///   Get the sequence of all known peer addresses.
        /// </summary>
        /// <value>
        ///   Contains any peer address that has been
        ///   <see cref="RegisterPeerAddress">discovered</see>.
        /// </value>
        /// <seealso cref="RegisterPeerAddress"/>
        IEnumerable<MultiAddress> KnownPeerAddresses { get; }

        /// <summary>
        ///   Get the sequence of all known peers.
        /// </summary>
        /// <value>
        ///   Contains any peer that has been
        ///   <see cref="RegisterPeerAddress">discovered</see>.
        /// </value>
        /// <seealso cref="RegisterPeerAddress"/>
        IEnumerable<Peer> KnownPeers { get; }

        /// <summary>
        ///   The addresses that cannot be used.
        /// </summary>
        MultiAddressBlackList BlackList { get; set; }

        /// <summary>
        ///   The addresses that can be used.
        /// </summary>
        MultiAddressWhiteList WhiteList { get; set; }

        ConnectionManager Manager { get; }

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
        ///   The <see cref="SwarmService.BlackList"/> or <see cref="SwarmService.WhiteList"/> policies forbid it.
        ///   Or the "p2p/ipfs" protocol name is missing.
        /// </exception>
        /// <remarks>
        ///   If the <paramref name="address"/> is not already known, then it is
        ///   added to the <see cref="SwarmService.KnownPeerAddresses"/>.
        /// </remarks>
        /// <seealso cref="SwarmService.RegisterPeer"/>
        Peer RegisterPeerAddress(MultiAddress address);

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
        ///   If peer does not already exist, then the <see cref="SwarmService.PeerDiscovered"/> event
        ///   is raised.
        ///   </para>
        /// </remarks>
        /// <exception cref="Exception">
        ///   The <see cref="SwarmService.BlackList"/> or <see cref="SwarmService.WhiteList"/> policies forbid it.
        /// </exception>
        Peer RegisterPeer(Peer peer);

        /// <summary>
        ///   Register that a peer has been discovered.
        /// </summary>
        /// <param name="peer">
        ///   The newly discovered peer.
        /// </param>
        /// <param name="ignoreRestrictionLists">
        ///   Ignores the blacklist or whitelist restrictions.
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
        ///   If peer does not already exist, then the <see cref="SwarmService.PeerDiscovered"/> event
        ///   is raised.
        ///   </para>
        /// </remarks>
        /// <exception cref="Exception">
        ///   The <see cref="SwarmService.BlackList"/> or <see cref="SwarmService.WhiteList"/> policies forbid it.
        /// </exception>
        Peer RegisterPeer(Peer peer, bool ignoreRestrictionLists);

        /// <summary>
        ///   Deregister a peer.
        /// </summary>
        /// <param name="peer">
        ///   The peer to remove..
        /// </param>
        /// <remarks>
        ///   Remove all knowledge of the peer. The <see cref="SwarmService.PeerRemoved"/> event
        ///   is raised.
        /// </remarks>
        void DeregisterPeer(Peer peer);

        /// <summary>
        ///   Determines if a connection is being made to the peer.
        /// </summary>
        /// <param name="peer">
        ///   A <see cref="Peer"/>.
        /// </param>
        /// <returns>
        ///   <b>true</b> is the <paramref name="peer"/> has a pending connection.
        /// </returns>
        bool HasPendingConnection(Peer peer);

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
        Task<PeerConnection> ConnectAsync(MultiAddress address,
            CancellationToken cancel = default);

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
        Task<PeerConnection> ConnectAsync(Peer peer, CancellationToken cancel = default);

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
        Task<Stream> DialAsync(Peer peer,
            string protocol,
            CancellationToken cancel = default);

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
        Task DisconnectAsync(MultiAddress address, CancellationToken cancel = default);

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
        ///   Allows other peers to <see cref="SwarmService.ConnectAsync(MultiFormats.MultiAddress,System.Threading.CancellationToken)">connect</see>
        ///   to the <paramref name="address"/>.
        ///   <para>
        ///   The <see cref="Peer.Addresses"/> of the <see cref="SwarmService.LocalPeer"/> are updated.  If the <paramref name="address"/> refers to
        ///   any IP address ("/ip4/0.0.0.0" or "/ip6/::") then all network interfaces addresses
        ///   are added.  If the port is zero (as in "/ip6/::/tcp/0"), then the peer addresses contains the actual port number
        ///   that was assigned.
        ///   </para>
        /// </remarks>
        Task<MultiAddress> StartListeningAsync(MultiAddress address);

        /// <summary>
        ///   Add a protocol that is supported by the swarm.
        /// </summary>
        /// <param name="protocol">
        ///   The protocol to add.
        /// </param>
        void AddProtocol(IPeerProtocol protocol);

        /// <summary>
        ///   Remove a protocol from the swarm.
        /// </summary>
        /// <param name="protocol">
        ///   The protocol to remove.
        /// </param>
        void RemoveProtocol(IPeerProtocol protocol);

        /// <summary>
        ///   Stop listening on the specified <see cref="MultiAddress"/>.
        /// </summary>
        /// <param name="address"></param>
        /// <returns>
        ///   A task that represents the asynchronous operation.
        /// </returns>
        /// <remarks>
        ///   Allows other peers to <see cref="SwarmService.ConnectAsync(MultiFormats.MultiAddress,System.Threading.CancellationToken)">connect</see>
        ///   to the <paramref name="address"/>.
        ///   <para>
        ///   The addresses of the <see cref="SwarmService.LocalPeer"/> are updated.
        ///   </para>
        /// </remarks>
        Task StopListeningAsync(MultiAddress address);
    }
}
