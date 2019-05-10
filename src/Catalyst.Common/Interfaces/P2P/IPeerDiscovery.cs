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
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.Network;
using Catalyst.Common.P2P;
using Catalyst.Protocol.Common;
using Google.Protobuf;
using Gstc.Collections.Observable.Interface;
using SharpRepository.Repository;

namespace Catalyst.Common.Interfaces.P2P
{
    public interface IPeerDiscovery : IMessageHandler
    {
        IRepository<Peer> PeerRepository { get; }

        /// <summary>
        ///     Helper function to store a peer in the PeerRepository
        /// </summary>
        /// <param name="peerId"></param>
        /// <returns></returns>
        int StorePeer(PeerId peerId);
    }
}
