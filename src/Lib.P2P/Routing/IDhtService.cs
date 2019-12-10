using System;
using Lib.P2P.Protocols;

namespace Lib.P2P.Routing 
{
    /// <summary>
    /// 
    /// </summary>
    public interface IDhtService : IPeerProtocol, IService, IPeerRouting, IContentRouting
    {
        /// <summary>
        ///   Provides access to other peers.
        /// </summary>
        SwarmService SwarmService { get; set; }

        /// <summary>
        ///   The number of closer peers to return.
        /// </summary>
        /// <value>
        ///   Defaults to 20.
        /// </value>
        int CloserPeerCount { get; set; }

        /// <summary>
        ///   Raised when the DHT is stopped.
        /// </summary>
        /// <seealso cref="DhtService.StopAsync"/>
        event EventHandler Stopped;

        /// <inheritdoc />
        string ToString();

        /// <summary>
        ///   Advertise that we can provide the CID to the X closest peers
        ///   of the CID.
        /// </summary>
        /// <param name="cid">
        ///   The CID to advertise.ipfs
        /// </param>
        /// <remarks>
        ///   This starts a background process to send the AddProvider message
        ///   to the 4 closest peers to the <paramref name="cid"/>.
        /// </remarks>
        void Advertise(Cid cid);

        /// <summary>
        ///   Process a find node request.
        /// </summary>
        DhtMessage ProcessFindNode(DhtMessage request, DhtMessage response);

        /// <summary>
        ///   Process a get provider request.
        /// </summary>
        DhtMessage ProcessGetProviders(DhtMessage request, DhtMessage response);

        /// <summary>
        ///   Process an add provider request.
        /// </summary>
        DhtMessage ProcessAddProvider(Peer remotePeer, DhtMessage request, DhtMessage response);
    }
}
