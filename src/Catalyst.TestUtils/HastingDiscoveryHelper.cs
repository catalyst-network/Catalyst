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
using System.Linq;
using Catalyst.Common.Interfaces.IO.Messaging.Correlation;
using Catalyst.Common.Interfaces.Network;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.P2P.Discovery;
using Catalyst.Common.Interfaces.P2P.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.Util;
using Catalyst.Common.IO.Messaging.Correlation;
using Catalyst.Common.Util;
using DnsClient;
using NSubstitute;

namespace Catalyst.TestUtils
{
    public static class HastingDiscoveryHelper
    {
        public static IEnumerable<IPeerIdentifier> GenerateNeighbours(int amount = 5)
        {
            var neighbours = new List<IPeerIdentifier>();
            while (amount > 0)
            {
                neighbours.Add(PeerIdentifierHelper.GetPeerIdentifier($@"neighbour-{neighbours.Count.ToString()}"));
                amount -= 1;
            }

            return neighbours;
        }

        public static IList<KeyValuePair<ICorrelationId, IPeerIdentifier>> MockContactedNeighboursValuePairs(IEnumerable<IPeerIdentifier> neighbours = default)
        {
            if (neighbours == null || neighbours.Equals(default))
            {
                neighbours = GenerateNeighbours();
            }
            
            var mockContactedNeighboursList = new List<KeyValuePair<ICorrelationId, IPeerIdentifier>>();
            
            (neighbours ?? throw new ArgumentNullException(nameof(neighbours))).ToList().ForEach(i =>
            {
                mockContactedNeighboursList.Add(new KeyValuePair<ICorrelationId, IPeerIdentifier>(CorrelationId.GenerateCorrelationId(), i));
            });

            return mockContactedNeighboursList;
        }
        
        public static IHastingMemento SubMemento(IPeerIdentifier identifier = default, IEnumerable<IPeerIdentifier> neighbours = default)
        {
            var subbedMemento = Substitute.For<IHastingMemento>();
            subbedMemento.Peer.Returns(identifier ?? PeerIdentifierHelper.GetPeerIdentifier(ByteUtil.GenerateRandomByteArray(32).ToString()));
            subbedMemento.Neighbours.Returns(neighbours ?? GenerateNeighbours());

            return subbedMemento;
        }
        
        public static IDns SubDnsClient(List<string> domains,
            IPeerSettings settings,
            string seedPid = "0x41437c30317c39322e3230372e3137382e3139387c34323036397c3031323334353637383930313233343536373839")
        {
            domains.ForEach(domain =>
            {
                MockQueryResponse.CreateFakeLookupResult(domain, seedPid, Substitute.For<ILookupClient>());
            });
        
            return new Common.Network.DevDnsClient(settings);
        }
        
        public static IPeerClientMessageDto SubDto(Type discoveryMessage, ICorrelationId correlationId = default, IPeerIdentifier sender = default)
        {
            var dto = Substitute.For<IPeerClientMessageDto>();
            dto.Sender.Returns(sender ?? Substitute.For<IPeerIdentifier>());
            dto.CorrelationId.Returns(correlationId ?? Substitute.For<ICorrelationId>());
            dto.Message.Returns(Activator.CreateInstance(discoveryMessage));

            return dto;
        }
        
        public static ICancellationTokenProvider SubCancellationProvider(bool result = false)
        {
            var provider = Substitute.For<ICancellationTokenProvider>();
            provider.HasTokenCancelled().Returns(result);
            return provider;
        }
    }
}
