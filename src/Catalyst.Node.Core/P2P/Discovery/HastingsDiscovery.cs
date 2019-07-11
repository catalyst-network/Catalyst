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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.Network;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.P2P.Discovery;
using Catalyst.Common.Interfaces.P2P.IO.Messaging.Correlation;
using Catalyst.Common.Interfaces.P2P.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.Util;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Common.P2P;
using Catalyst.Node.Core.P2P.IO.Messaging.Dto;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.IPPN;
using Google.Protobuf;
using Microsoft.Extensions.Caching.Memory;
using Nethereum.KeyStore.Crypto;
using Nethereum.RLP;
using Serilog;
using SharpRepository.Repository;

namespace Catalyst.Node.Core.P2P.Discovery
{
    public sealed class HastingsDiscovery 
        : IHastingsDiscovery, IDisposable
    {
        public IDns Dns { get; }
        public ILogger Logger { get; }
        public IRepository<Peer> PeerRepository { get; }
        private readonly ReplaySubject<IPeerClientMessageDto> _discoveryMessage;

        private IObservable<IPeerClientMessageDto> _discoveryMessageStream;

        public IObservable<IPeerClientMessageDto> DiscoveryMessageStream
        {
            get => _discoveryMessage.AsObservable();
            set => _discoveryMessageStream = value;
        }

        private IPeerIdentifier _ownNode;
        private IHastingsOriginator state;
        private readonly IPeerClient _peerClient;
        private readonly IDtoFactory _dtoFactory;
        private HastingCareTaker _hastingCareTaker;
        private readonly IPeerSettings _peerSettings;
        private readonly ICancellationTokenProvider _cancellationTokenProvider;
        private readonly IPeerMessageCorrelationManager _peerMessageCorrelationManager;
        
        public HastingsDiscovery(ILogger logger,
            IRepository<Peer> peerRepository,
            IDns dns,
            IPeerSettings peerSettings,
            IPeerClient peerClient,
            IDtoFactory dtoFactory,
            ICancellationTokenProvider cancellationTokenProvider)
        {
            Dns = dns;
            Logger = logger;
            PeerRepository = peerRepository;

            _peerClient = peerClient;
            _dtoFactory = dtoFactory;
            _peerSettings = peerSettings;
            _hastingCareTaker = new HastingCareTaker();
            _cancellationTokenProvider = cancellationTokenProvider;
            _discoveryMessage = new ReplaySubject<IPeerClientMessageDto>(1);
            _ownNode = new PeerIdentifier(_peerSettings, new PeerIdClientId("AC")); // this needs to be changed
            
            Task.Run(async () =>
            {
                await DiscoveryAsync();
            });
        }

        public async Task DiscoveryAsync()
        {
            // start walk by getting a seed node from dns
            var seedNodes = GetSeedNodes();
            
            // select random seedNode for first step in walk
            state = new HastingsOriginator
            {
                Peer = seedNodes.RandomElement()
            };

            do
            {
                var peerNeighbourRequestDto = _dtoFactory.GetDto(new PeerNeighborsRequest(),
                    _ownNode,
                    state.Peer
                );
            
                // peerNeighbourRequestDto.CorrelationId
            
                // _peerClient.SendMessage(peerNeighbourRequestDto);
            } while (!_cancellationTokenProvider.HasTokenCancelled());
        }
        
        public void MergeDiscoveryMessageStreams(IObservable<IPeerClientMessageDto> reputationChangeStream)
        {
            DiscoveryMessageStream = DiscoveryMessageStream.Merge(reputationChangeStream);
        }

        private IEnumerable<IPeerIdentifier> GetSeedNodes()
        {
            return Dns.GetSeedNodesFromDns(_peerSettings.SeedServers).ToList();
        }
        
        public void Dispose()
        {
            _peerClient?.Dispose();
            PeerRepository?.Dispose();
        }
    }
}
