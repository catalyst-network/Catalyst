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
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.Interfaces.Network;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.P2P;
using Catalyst.Node.Core.P2P.Messaging;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.IPPN;
using DotNetty.Buffers;
using Nethereum.RLP;
using Serilog;
using SharpRepository.Repository;
using System.Collections.Generic;
using System.Reactive;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.P2P.Messaging;
using Google.Protobuf;
using Peer = Catalyst.Common.P2P.Peer;

namespace Catalyst.Node.Core.P2P
{
    public sealed class PeerDiscovery
        : IHastingWalkDiscovery,
            IDisposable
    {
        public IDns Dns { get; }
        private bool NeighbourRequested { get; set; }
        public IRepository<Peer> PeerRepository { get; }
        public int TotalPotentialCandidates { get; set; }
        public int DiscoveredPeerInCurrentWalk { get; set; }

        /// <inheritdoc />
        public IPeerIdentifier PreviousPeer { get; private set; }
        
        public IPeerIdentifier NextCandidate { get; private set; }
        
        /// <inheritdoc />
        public IPeerIdentifier CurrentPeer { get; private set; }

        public IDisposable PingResponseMessageStream { get; set; }
        public IDisposable GetNeighbourResponseStream { get; set; }
        public IDisposable P2PCorrelationCacheEvictionSubscription { get; set; }
        public IObservable<IList<Unit>> PeerDiscoveryMessagesEventStream { get; set; }
        private IObservable<KeyValuePair<IPeerIdentifier, ByteString>> _p2PCorrelationCacheEvictionStream;

        /// <inheritdoc />
        public IDictionary<int, KeyValuePair<IPeerIdentifier, ByteString>> PreviousPeerNeighbours { get; private set; }

        /// <inheritdoc />
        public IDictionary<int, KeyValuePair<IPeerIdentifier, ByteString>> CurrentPeerNeighbours { get; set; }
        
        private readonly ILogger _logger;
        private readonly IPeerIdentifier _ownNode;
        private readonly IPeerSettings _peerSettings;
        private readonly IReputableCache _p2PCorrelationCache;
        private readonly IPeerClientFactory _peerClientFactory;
        private readonly CancellationTokenSource _cancellationSource;

        /// <summary>
        /// </summary>
        /// <param name="dns"></param>
        /// <param name="repository"></param>
        /// <param name="peerSettings"></param>
        /// <param name="peerClientFactory"></param>
        /// <param name="p2PCorrelationCache"></param>
        /// <param name="logger"></param>
        public PeerDiscovery(IDns dns,
            IRepository<Peer> repository,
            IPeerSettings peerSettings,
            IPeerClientFactory peerClientFactory,
            IReputableCache p2PCorrelationCache,
            ILogger logger)
        {
            Dns = dns;
            _logger = logger;
            NextCandidate = null;
            PeerRepository = repository;
            _peerSettings = peerSettings;
            _peerClientFactory = peerClientFactory;
            _p2PCorrelationCache = p2PCorrelationCache;
            _cancellationSource = new CancellationTokenSource();
            CurrentPeerNeighbours = new Dictionary<int, KeyValuePair<IPeerIdentifier, ByteString>>();
            
            PreviousPeerNeighbours = new Dictionary<int, KeyValuePair<IPeerIdentifier, ByteString>>();
            _ownNode = new PeerIdentifier(_peerSettings.PublicKey.ToBytesForRLPEncoding(), _peerSettings.BindAddress,
                _peerSettings.Port);
            
            var longRunningTasks = new[] {PeerCrawler()};
            Task.WaitAll(longRunningTasks);
        }

        /// <inheritdoc />
        public async Task PeerCrawler()
        {
            // Start observing for when messages are evicted.
            StartObservingEvictionEvents(_p2PCorrelationCache.PeerEviction);
            
            // Outer-loop that only stops on cancellation, discovery must always run! 
            do
            {
                // Catch any exception thrown as to not kill the loop.
                try
                {
                    // Start walk by queering dns for some seeds.
                    CurrentPeer = Dns.GetSeedNodesFromDns(_peerSettings.SeedServers).RandomElement();
                    
                    // Start loop to propose the next degree in the walk, and run while we have a current peer.
                    do
                    {
                        // Request from peer their neighbours.
                        using (var peerClient = _peerClientFactory.GetClient(CurrentPeer.IpEndPoint))
                        {
                            await peerClient.AsyncSendMessage(BuildPeerNeighbourRequestDatagram());
                            
                            // Flow condition set while we wait for a response.
                            NeighbourRequested = true;
                        }

                        // Flow control is set when receiving a neighbour response message.
                        await WaitUntil(() => NeighbourRequested == false);

                        // When current peer provides a list of neighbours.
                        if (CurrentPeerNeighbours.Any())
                        {
                            // Count potential proposals and iterate.
                            TotalPotentialCandidates = CurrentPeerNeighbours.Count;
                            CurrentPeerNeighbours.ToList().ForEach(async currentPeerNeighbour =>
                            {
                                // De-construct the <Key => value<key => value>>
                                var (_, (key, _)) = currentPeerNeighbour;
                                
                                // Ping each candidate.
                                using (var peerClient = _peerClientFactory.GetClient(key.IpEndPoint))
                                {
                                    var pingDatagram = BuildPingRequestDatagram(key);
                                    await peerClient.AsyncSendMessage(pingDatagram.ToDatagram(key.IpEndPoint));
                                    UpdateCurrentPeerNeighbourPingStatus(pingDatagram);
                                }

                                // Wait until next step is proposed.
                                await WaitUntil(() => NextCandidate != null);
                                WalkForward();
                            });   
                        }
                        else
                        {
                            // Current peer didn't provide an new degree propositions so transition back to last peer.
                            WalkBack();
                        }
                    } while (CurrentPeer != null && !_cancellationSource.IsCancellationRequested);
                }
                catch (TimeoutException e)
                {
                    // A time-out in the current degree processing means we should go back to the previousPeer and try continue.
                    _logger.Information(e.Message);
                    ResetWalk();
                }
                catch (Exception e)
                {
                    // Some other exception has happened, in this case reset the walk.
                    _logger.Debug(e.Message);
                    ResetWalk();
                }   
            } while (!_cancellationSource.IsCancellationRequested);
        }
        
        public void StartObservingMessageStreams(IObservable<IChanneledMessage<AnySigned>> observer)
        {
            // Filter stream messages to only peers we pinged from within this class.
            var pingResponseObserver = observer
               .Where(message => message != null && QueriedPeer(
                    new KeyValuePair<IPeerIdentifier, ByteString>(new PeerIdentifier(message.Payload.PeerId),
                        message.Payload.CorrelationId),
                    CurrentPeerNeighbours)
                );
               
            PingResponseMessageStream = pingResponseObserver.Subscribe(PingResponseSubscriptionHandler);

            // Observe response from GetNeighbourResponse Handler.
            var getNeighbourResponseObserver = observer
               .Where(message => message != null && message.Payload.PeerId.Equals(CurrentPeer.PeerId) && message.Payload.TypeUrl == typeof(PeerNeighborsResponse)
                   .ShortenedProtoFullName()
                );
               
            GetNeighbourResponseStream = getNeighbourResponseObserver.Subscribe(PeerNeighbourSubscriptionHandler);

            // Here we just want to count the number of events over our pingResponse and getNeighbourResponse until it matches the number of getNeighbourRequest messages sent out.
            // We want to make sure all requests sent out have an associated event so we can propose the next peer in walk.
            PeerDiscoveryMessagesEventStream = pingResponseObserver.Select(_ => Unit.Default)
               .Merge(_p2PCorrelationCacheEvictionStream.Select(_ => Unit.Default))
               .Buffer(TotalPotentialCandidates);
                
            PeerDiscoveryMessagesEventStream.Subscribe(ProposeNextStep);
        }
        
        public void StartObservingEvictionEvents(IObservable<KeyValuePair<IPeerIdentifier, ByteString>> observer)
        {
            _p2PCorrelationCacheEvictionStream = observer
               .Where(message => message.Value != null && QueriedPeer(message, CurrentPeerNeighbours));
            P2PCorrelationCacheEvictionSubscription = _p2PCorrelationCacheEvictionStream
               .Subscribe(PeerMessageEvictionHandler);
        }
        
        /// <summary>
        ///     Called when a pingResponse event is see, we can store the peer at this point.
        /// </summary>
        /// <param name="message"></param>
        public void PingResponseSubscriptionHandler(IChanneledMessage<AnySigned> message)
        {
            _logger.Information("processing ping message stream");
            DiscoveredPeerInCurrentWalk = StorePeer(message.Payload.PeerId);
            _logger.Information(message.Payload.TypeUrl);
        }

        /// <summary>
        ///     Called when a peerNeighbour request event is seen, to try populate CurrentPeerNeighbours.
        /// </summary>
        /// <param name="message"></param>
        private void PeerNeighbourSubscriptionHandler(IChanneledMessage<AnySigned> message)
        {   
            _logger.Information("processing peer neighbour message stream");
            message.Payload.FromAnySigned<PeerNeighborsResponse>().Peers.ToList().ForEach(peerId =>
            {
                // Don't include yourself, current node or the last node in the degree proposal.
                if (peerId.Equals(_ownNode.PeerId) && peerId.Equals(CurrentPeer.PeerId) &&
                    peerId.Equals(PreviousPeer.PeerId))
                {
                    return;
                }
                
                var pid = new PeerIdentifier(peerId);
                CurrentPeerNeighbours.TryAdd(pid.GetHashCode(), new KeyValuePair<IPeerIdentifier, ByteString>(pid, message.Payload.CorrelationId));
            });

            // Ensure we have possible next step candidates.
            if (CurrentPeerNeighbours.Count < 1)
            {
                WalkBack();
            }
                        
            NeighbourRequested = false;
        }

        /// <summary>
        ///     Handler called when event is emitted from the p2pCorrelation OnEviction delegate.
        /// <see cref="P2PCorrelationCache"/>
        /// </summary>
        /// <param name="currentPeerNeighbour"></param>
        private void PeerMessageEvictionHandler(KeyValuePair<IPeerIdentifier, ByteString> currentPeerNeighbour)
        {
            if (!CurrentPeerNeighbours.ContainsKey(currentPeerNeighbour.GetHashCode()))
            {
                return;
            }
            
            CurrentPeerNeighbours.Remove(currentPeerNeighbour.GetHashCode());
        }

        /// <summary>
        ///     Looks through a list of peers we hold to see if we pinged it or not.
        /// </summary>
        /// <param name="peerIdentifier"></param>
        /// <param name="peerList"></param>
        /// <returns></returns>
        private static bool QueriedPeer(KeyValuePair<IPeerIdentifier, ByteString> messageRef, IDictionary<int, KeyValuePair<IPeerIdentifier, ByteString>> peerList)
        {
            var (key, _) = messageRef;
            peerList.TryGetValue(key.GetHashCode(), out var peerKeyValue);
            return peerKeyValue.Value == messageRef.Value;
        }

        /// <inheritdoc />
        public void ProposeNextStep(IList<Unit> _)
        {
            NextCandidate = CurrentPeerNeighbours.RandomElement().Value.Key;
        }

        /// <inheritdoc />
        public void WalkForward()
        {
            if (NextCandidate == null)
            {
                return;
            }

            NextCandidate = null;
            PreviousPeer = CurrentPeer;
            CurrentPeer = NextCandidate;
            PreviousPeerNeighbours.Clear();
            
            CurrentPeerNeighbours.ToList().ForEach(pair =>
            {
                PreviousPeerNeighbours.Add(pair);
            });

            CurrentPeerNeighbours.Clear();
        }
        
        /// <inheritdoc />
        public void WalkBack()
        {
            if (PreviousPeer == null)
            {
                ResetWalk();
            }

            PreviousPeer = null;
            NextCandidate = null;
            NeighbourRequested = false;
            CurrentPeer = PreviousPeer;
            DiscoveredPeerInCurrentWalk = 0;
        }

        /// <summary>
        ///      Reset control flows and peer lists to we can run walk again.
        /// </summary>
        private IPeerDiscovery ResetWalk()
        {
            CurrentPeer = null;
            PreviousPeer = null;
            NextCandidate = null;
            NeighbourRequested = false;
            DiscoveredPeerInCurrentWalk = 0;
            CurrentPeerNeighbours.Clear();
            PreviousPeerNeighbours.Clear();
            return this;
        }
        
        /// <summary>
        ///     When a pingResponse is received we store the correlationId so we know it has responded.
        /// </summary>
        /// <param name="currentPeerNeighbourPingAnySigned"></param>
        private void UpdateCurrentPeerNeighbourPingStatus(AnySigned currentPeerNeighbourPingAnySigned)
        {
            var currentPeerNeighbourPid =
                new PeerIdentifier(currentPeerNeighbourPingAnySigned.PeerId);

            if (!CurrentPeerNeighbours.ContainsKey(currentPeerNeighbourPid.GetHashCode()))
            {
                return;
            }

            CurrentPeerNeighbours[currentPeerNeighbourPid.GetHashCode()] =
                new KeyValuePair<IPeerIdentifier, ByteString>(currentPeerNeighbourPid,
                    currentPeerNeighbourPingAnySigned.CorrelationId);
        }

        /// <summary>
        ///     Helper method to build pingRequest datagram.
        /// </summary>
        /// <param name="currentPeerNeighbour"></param>
        /// <returns></returns>
        private AnySigned BuildPingRequestDatagram(IPeerIdentifier currentPeerNeighbour)
        {
            return new P2PMessageFactory<PingRequest>(_p2PCorrelationCache).GetMessage(
                new PingRequest(),
                new PeerIdentifier(currentPeerNeighbour.PublicKey, currentPeerNeighbour.Ip,
                    currentPeerNeighbour.Port),
                new PeerIdentifier(_peerSettings.PublicKey.ToBytesForRLPEncoding(), _peerSettings.BindAddress,
                    _peerSettings.Port),
                MessageTypes.Ask
            );
        }
        
        /// <summary>
        ///     Helper method to build a peerNeighbourRequest datagram.
        /// </summary>
        /// <returns></returns>
        private IByteBufferHolder BuildPeerNeighbourRequestDatagram()
        {
            return new P2PMessageFactory<PeerNeighborsRequest>(_p2PCorrelationCache).GetMessageInDatagramEnvelope(
                new PeerNeighborsRequest(),
                new PeerIdentifier(CurrentPeer.PublicKey, CurrentPeer.Ip,
                    CurrentPeer.Port),
                new PeerIdentifier(_peerSettings.PublicKey.ToBytesForRLPEncoding(), _peerSettings.BindAddress,
                    _peerSettings.Port),
                MessageTypes.Ask
            );
        }

        /// <inheritdoc />
        /// <summary>
        ///     Stores a peer in the database unless we are in the burn-in phase.
        /// </summary>
        /// <param name="peerId"></param>
        /// <returns></returns>
        public int StorePeer(PeerId peerId)
        {
            if (DiscoveredPeerInCurrentWalk >= Constants.PeerDiscoveryBurnIn)
            {
                PeerRepository.Add(new Peer
                {
                    LastSeen = DateTime.Now,
                    PeerIdentifier = new PeerIdentifier(peerId),
                    Reputation = 0
                });
            }

            return DiscoveredPeerInCurrentWalk++;
        }

        /// <summary>
        ///     Blocks until condition is true or timeout occurs.
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
        
        #region IDisposable Support
        
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
            P2PCorrelationCacheEvictionSubscription?.Dispose();
        }
        
        #endregion
    }
}
