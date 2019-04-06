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
using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Common.P2P;

namespace Catalyst.Node.Core.P2P
{
    public class P2PService : IP2P
    {
        public P2PService(IPeerSettings settings, IPeerDiscovery peerDiscovery, IP2PMessaging messaging)
        {
            Messaging = messaging;
            Discovery = peerDiscovery;
            Settings = settings;

            Identifier = new PeerIdentifier(Settings);
        }

        public IPeerDiscovery Discovery { get; }
        public IPeerIdentifier Identifier { get; }
        public IPeerSettings Settings { get; }
        public IP2PMessaging Messaging { get; }

        public List<IPeerIdentifier> FindNode(IPeerIdentifier queryingNode, IPeerIdentifier targetNode)
        {
            throw new NotImplementedException();
        }

        public List<IPeerIdentifier> GetPeers(IPeerIdentifier queryingNode)
        {
            throw new NotImplementedException();
        }

        public bool Store(string k, byte[] v)
        {
            throw new NotImplementedException();
        }

        public dynamic FindValue(string k)
        {
            throw new NotImplementedException();
        }

        public List<IPeerIdentifier> PeerExchange(IPeerIdentifier queryingNode)
        {
            throw new NotImplementedException();
        }
    }
}
