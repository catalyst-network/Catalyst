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
using System.Reactive;
using System.Threading.Tasks;
using Catalyst.Common.Interfaces.Network;
using Google.Protobuf;

namespace Catalyst.Common.Interfaces.P2P
{
    public interface IHastingWalkDiscovery : IPeerDiscovery
    {
        IDns Dns { get; }

        int TotalPotentialCandidates { get; set; }
        int DiscoveredPeerInCurrentWalk { get; set; }
        
        /// <summary>
        ///     The previous degree of walk
        /// </summary>
        IPeerIdentifier PreviousPeer { get; }

        IPeerIdentifier NextCandidate { get; }

        /// <summary>
        ///     Current degree of walk
        /// </summary>
        IPeerIdentifier CurrentPeer { get; }

        /// <summary>
        ///     A dict of current peers neighbours, key is a hashcode int of the IPeerIdentifier,
        ///     with the value a single key => value struct of the IPeerIdentifier and a bool to indicate if it's been pinged
        /// </summary>
        IDictionary<int, KeyValuePair<IPeerIdentifier, ByteString>> PreviousPeerNeighbours { get; }

        /// <summary>
        ///     A dict of previous peers neighbours, key is a hashcode int of the IPeerIdentifier,
        ///     with the value a single key => value struct of the IPeerIdentifier and a bool to indicate if it's been pinged
        /// </summary>
        IDictionary<int, KeyValuePair<IPeerIdentifier, ByteString>> CurrentPeerNeighbours { get; }

        IDisposable PingResponseMessageStream { get; set; }
        IDisposable GetNeighbourResponseStream { get; set; }
        IDisposable P2PCorrelationCacheEvictionSubscription { get; set; }
        IObservable<IList<Unit>> PeerDiscoveryMessagesEventStream { get; set; }
        
        /// <summary>
        ///     Crawls nodes in network according to the protocol blueprint spec
        /// </summary>
        /// <returns></returns>
        Task PeerCrawler();

        /// <summary>
        ///     Called when we see evictionEvents + pingResponse == TotalPotentialCandidates
        /// </summary>
        /// <param name="_">We don't care about the param sent to this method.</param>
        void ProposeNextStep(IList<Unit> _);

        /// <summary>
        ///     Transitions to the next step in walk
        /// </summary>
        void WalkForward();

        /// <summary>
        ///     Returns back to the previous peer in the walk.
        /// </summary>
        void WalkBack();
    }
}
