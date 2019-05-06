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
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO;
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.Interfaces.Network;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.P2P;
using Catalyst.Common.Util;
using Catalyst.Node.Core.P2P.Messaging;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.IPPN;
using DotNetty.Buffers;
using Microsoft.Extensions.Configuration;
using Nethereum.RLP;
using Serilog;
using SharpRepository.Repository;
using Peer = Catalyst.Common.P2P.Peer;

namespace Catalyst.Node.Core.P2P
{
    public sealed class PeerDiscovery
        : IPeerDiscovery,
            IDisposable
    {
        public IDns Dns { get; }
        public IRepository<Peer> PeerRepository { get; }
        public IDisposable PingResponseMessageStream { get; private set; }
        public IDisposable GetNeighbourResponseStream { get; private set; }

        private IPeerIdentifier _currentPeer;
        private readonly object _currentPeerLock = new object();        
        
        public IPeerIdentifier CurrentPeer 
        {
            get
            {
                lock (_currentPeerLock)
                {
                    return _currentPeer;
                }
            }
            private set
            {
                lock (_currentPeerLock)
                {
                    _currentPeer = value;
                }
            }
        }

        public IProducerConsumerCollection<IPeerIdentifier> CurrentPeerNeighbours { get; private set; }

        private IPeerIdentifier _previousPeer;
        private readonly object _previousPeerLock = new object();

        public IPeerIdentifier PreviousPeer 
        {
            get
            {
                lock (_previousPeerLock)
                {
                    return _previousPeer;
                }
            }
            private set
            {
                lock (_previousPeerLock)
                {
                    _previousPeer = value;
                }
            }
        }

        public IProducerConsumerCollection<IPeerIdentifier> PreviousPeerNeighbours { get; private set; }
        
        private bool _neighbourRequested;
        private readonly object _neighbourRequestedLock = new object();

        public bool NeighbourRequested 
        {
            get
            {
                lock (_neighbourRequestedLock)
                {
                    return _neighbourRequested;
                }
            }
            private set
            {
                lock (_neighbourRequestedLock)
                {
                    _neighbourRequested = value;
                }
            }
        }

        private readonly ILogger _logger;
        private readonly IPeerIdentifier _ownNode;
        private readonly IPeerSettings _peerSettings;
        private readonly IPeerClientFactory _peerClientFactory;        
        private readonly CancellationTokenSource _cancellationSource;

        /// <summary>
        /// </summary>
        /// <param name="dns"></param>
        /// <param name="repository"></param>
        /// <param name="peerSettings"></param>
        /// <param name="peerClientFactory"></param>
        /// <param name="logger"></param>
        public PeerDiscovery(IDns dns,
            IRepository<Peer> repository,
            IPeerSettings peerSettings,
            IPeerClientFactory peerClientFactory,
            ILogger logger)
        {
            Dns = dns;
            _logger = logger;
            _peerClientFactory = peerClientFactory;
            PeerRepository = repository;
            _peerSettings = peerSettings;
            _cancellationSource = new CancellationTokenSource();
            CurrentPeerNeighbours = new ConcurrentQueue<IPeerIdentifier>();
            PreviousPeerNeighbours = new ConcurrentQueue<IPeerIdentifier>();
            _ownNode = new PeerIdentifier(_peerSettings.PublicKey.ToBytesForRLPEncoding(), _peerSettings.BindAddress,
                _peerSettings.Port);
            
            Task[] longRunningTasks =
            {
                Task.Factory.StartNew(PeerCrawler)
            };
            
            Task.WaitAll(longRunningTasks);
        }

        public void StartObserving(IObservable<IChanneledMessage<AnySigned>> observer)
        {
            PingResponseMessageStream = observer
               .Where(m => m != null && m.Payload.TypeUrl == typeof(PingResponse)
                   .ShortenedProtoFullName()
                ).Subscribe(PingSubscriptionHandler);
            
            GetNeighbourResponseStream = observer
               .Where(m => m != null && m.Payload.TypeUrl == typeof(PeerNeighborsResponse)
                   .ShortenedProtoFullName()
                ).Subscribe(PeerNeighbourSubscriptionHandler);
        }

        private void PingSubscriptionHandler(IChanneledMessage<AnySigned> message)
        {
            _logger.Information("processing ping message stream");
            var pingResponse = message.Payload.FromAnySigned<PingResponse>();
            PeerRepository.Add(new Peer
            {
                LastSeen = DateTime.Now,
                PeerIdentifier = new PeerIdentifier(message.Payload.PeerId),
                Reputation = 0
            });

            _logger.Information(message.Payload.TypeUrl);
        }
        
        public void PeerNeighbourSubscriptionHandler(IChanneledMessage<AnySigned> message)
        {
            _logger.Information("processing peer neighbour message stream");
            message.Payload.FromAnySigned<PeerNeighborsResponse>().Peers.ToList().ForEach(peerId =>
            {
                // don't include yourself, current node or the last node the degree proposal
                if (!peerId.Equals(_ownNode.PeerId) || !peerId.Equals(CurrentPeer.PeerId) || !peerId.Equals(PreviousPeer.PeerId))
                {
                    CurrentPeerNeighbours.TryAdd(new PeerIdentifier(peerId));                    
                }
            });
            NeighbourRequested = false;
        }

        private async Task PeerCrawler()
        {
            try
            {
                do
                {
                    CurrentPeer = Dns.GetSeedNodesFromDns(_peerSettings.SeedServers).RandomElement();

                    await WaitUntil(() => _currentPeer != null);

                    using (var peerClient = _peerClientFactory.GetClient(_currentPeer.IpEndPoint))
                    {
                        peerClient.SendMessage(BuildPeerNeighbourRequestDatagram()).GetAwaiter().GetResult();
                        NeighbourRequested = true;
                    }

                    await WaitUntil(() => CurrentPeerNeighbours.Any() && _neighbourRequested == false);
                    
                    CurrentPeerNeighbours.ToList().ForEach(currentPeerNeighbour =>
                    {
                        using (var peerClient = _peerClientFactory.GetClient(_currentPeer.IpEndPoint))
                        {
                            peerClient.SendMessage(BuildPingRequestDatagram()).GetAwaiter().GetResult();
                            NeighbourRequested = true;
                        }
                    });
                } while (_currentPeer != null && !_cancellationSource.IsCancellationRequested);
            }
            catch (Exception e)
            {
                _logger.Debug(e.Message);
            }
        }

        private IByteBufferHolder BuildPingRequestDatagram()
        {
            return new P2PMessageFactory<PingRequest>().GetMessageInDatagramEnvelope(
                new PingRequest(),
                new PeerIdentifier(CurrentPeer.PublicKey, CurrentPeer.Ip,
                    CurrentPeer.Port),
                new PeerIdentifier(_peerSettings.PublicKey.ToBytesForRLPEncoding(), _peerSettings.BindAddress,
                    _peerSettings.Port),
                MessageTypes.Ask
            );
        }

        private IByteBufferHolder BuildPeerNeighbourRequestDatagram()
        {
            return new P2PMessageFactory<PeerNeighborsRequest>().GetMessageInDatagramEnvelope(
                new PeerNeighborsRequest(),
                new PeerIdentifier(CurrentPeer.PublicKey, CurrentPeer.Ip,
                    CurrentPeer.Port),
                new PeerIdentifier(_peerSettings.PublicKey.ToBytesForRLPEncoding(), _peerSettings.BindAddress,
                    _peerSettings.Port),
                MessageTypes.Ask
            );
        }
        
        /// <summary>
        /// Blocks until condition is true or timeout occurs.
        /// </summary>
        /// <param name="condition">The break condition.</param>
        /// <param name="frequency">The frequency at which the condition will be checked.</param>
        /// <param name="timeout">The timeout in milliseconds.</param>
        /// <returns></returns>
        private static async Task WaitUntil(Func<bool> condition, int frequency = 25, int timeout = -1)
        {
            var waitTask = Task.Run(async () =>
            {
                while (!condition())
                {
                    await Task.Delay(frequency);
                }
            });

            if (waitTask != await Task.WhenAny(waitTask, Task.Delay(timeout)))
            {
                throw new TimeoutException();
            }
        }
        
        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            _logger.Debug($"Disposing {GetType().Name}");
            PingResponseMessageStream?.Dispose();
            GetNeighbourResponseStream?.Dispose();
        }
    }
}
