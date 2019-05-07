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
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.Network;
using Catalyst.Common.P2P;
using Catalyst.Protocol.Common;
using Microsoft.Extensions.Configuration;
using Serilog;
using SharpRepository.Repository;

namespace Catalyst.Common.Interfaces.P2P
{
    public interface IPeerDiscovery : IMessageHandler
    {
        IDns Dns { get; }
        IRepository<Peer> PeerRepository { get; }
        IDisposable PingResponseMessageStream { get; }
        IDisposable GetNeighbourResponseStream { get; }
        
        /// <summary>
        ///     Current degree of walk
        /// </summary>
        IPeerIdentifier CurrentPeer { get; }
        
        /// <summary>
        ///     A thread safe dict of current peers neighbours, key is a hashcode int of the IPeerIdentifier,
        ///     with the value a single key => value struct of the IPeerIdentifier and a bool to indicate if it's been pinged
        /// </summary>
        ConcurrentDictionary<int, KeyValuePair<IPeerIdentifier, bool>> CurrentPeerNeighbours { get; }
        
        /// <summary>
        ///     The previous degree of walk we was at
        /// </summary>
        IPeerIdentifier PreviousPeer { get; } 
        
        /// <summary>
        ///     A thread safe dict of previous peers neighbours, key is a hashcode int of the IPeerIdentifier,
        ///     with the value a single key => value struct of the IPeerIdentifier and a bool to indicate if it's been pinged
        /// </summary>
        ConcurrentDictionary<int, KeyValuePair<IPeerIdentifier, bool>> PreviousPeerNeighbours { get; }
        
        /// <summary>
        ///     Method called to handle the GetNeighbourResponseStream
        /// </summary>
        /// <param name="message"></param>
        void PeerNeighbourSubscriptionHandler(IChanneledMessage<AnySigned> message);
        
        /// <summary>
        ///     Helper function to store a peer in the PeerRepository
        /// </summary>
        /// <param name="peerId"></param>
        /// <returns></returns>
        int StorePeer(PeerId peerId);
    }
}
