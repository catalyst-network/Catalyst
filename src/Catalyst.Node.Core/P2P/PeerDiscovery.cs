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
using System.Threading.Tasks;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.Network;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.P2P;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.IPPN;
using Microsoft.Extensions.Configuration;
using Serilog;
using SharpRepository.Repository;
using Peer = Catalyst.Common.P2P.Peer;

namespace Catalyst.Node.Core.P2P
{
    public sealed class PeerDiscovery
        : IPeerDiscovery, IDisposable
    {
        public IDns Dns { get; }
        public ILogger Logger { get; }
        public IProducerConsumerCollection<IPeerIdentifier> Peers { get; }
        public IRepository<Peer> PeerRepository { get; }
        public IDisposable PingResponseMessageStream { get; private set; }
        public IDisposable GetNeighbourResponseStream { get; private set; }

        /// <summary>
        /// </summary>
        /// <param name="dns"></param>
        /// <param name="repository"></param>
        /// <param name="rootSection"></param>
        /// <param name="logger"></param>
        public PeerDiscovery(IDns dns,
            IRepository<Peer> repository,
            IConfigurationRoot rootSection,
            ILogger logger)
        {
            Dns = dns;
            Logger = logger;
            PeerRepository = repository;
            Peers = new ConcurrentQueue<IPeerIdentifier>();

            Peers.TryAdd(Dns.GetSeedNodesFromDns(ParseDnsServersFromConfig(rootSection)).RandomElement());

            var longRunningTasks = new[] {PeerCrawlerAsync()};
            Task.WaitAll(longRunningTasks);
        }

        public IList<string> ParseDnsServersFromConfig(IConfigurationRoot rootSection)
        {
            var seedDnsUrls = new List<string>();
            try
            {
                ConfigValueParser.GetStringArrValues(rootSection, "SeedServers").ToList().ForEach(seedUrl =>
                {
                    seedDnsUrls.Add(seedUrl); 
                });
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                throw;
            }

            return seedDnsUrls;
        }

        public void StartObserving(IObservable<IObserverDto<ProtocolMessage>> observer)
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

        private void PingSubscriptionHandler(IObserverDto<ProtocolMessage> messageDto)
        {
            Logger.Information("processing ping message stream");
            var pingResponse = messageDto.Payload.FromProtocolMessage<PingResponse>();
            PeerRepository.Add(new Peer
            {
                LastSeen = DateTime.Now,
                PeerIdentifier = new PeerIdentifier(messageDto.Payload.PeerId),
                Reputation = 0
            });

            Logger.Information(messageDto.Payload.TypeUrl);
        }
        
        public void PeerNeighbourSubscriptionHandler(IObserverDto<ProtocolMessage> messageDto)
        {
            Logger.Information("processing peer neighbour message stream");
            var peerNeighborsResponse = messageDto.Payload.FromProtocolMessage<PeerNeighborsResponse>();
        }

        private async Task PeerCrawlerAsync()
        {
            if (Peers.Count != 0) return;
            try { }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                throw;
            }

            await Task.Delay(5000);
        }
        
        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (!disposing) return;
            Logger.Debug($"Disposing {GetType().Name}");
            PingResponseMessageStream?.Dispose();
        }
    }
}
