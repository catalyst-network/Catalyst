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
using System.Net;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Network;
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.Interfaces.Network;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.P2P;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.IPPN;
using Dawn;
using DnsClient.Protocol;
using Microsoft.Extensions.Configuration;
using Nethereum.RLP;
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
        public IList<string> SeedNodes { get; }
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
            SeedNodes = new List<string>();
            Peers = new ConcurrentQueue<IPeerIdentifier>();

            ParseDnsServersFromConfig(rootSection);

            var longRunningTasks = new[] {PeerCrawler()};
            Task.WaitAll(longRunningTasks);
        }

        public void ParseDnsServersFromConfig(IConfigurationRoot rootSection)
        {
            try
            {
                foreach (var seedNode in ConfigValueParser.GetStringArrValues(rootSection, "SeedServers").ToList())
                {
                    SeedNodes.Add(seedNode);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                throw;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="seedServers"></param>
        public void GetSeedNodesFromDns(IEnumerable<string> seedServers)
        {
            seedServers.ToList().ForEach(async seedServer =>
            {
                var dnsQueryAnswer = await Dns.GetTxtRecords(seedServer).ConfigureAwait(false);
                var answerSection = (TxtRecord) dnsQueryAnswer.Answers.FirstOrDefault();

                Guard.Argument(answerSection).NotNull();
                
                IList<IPeerIdentifier> seedPeerIdentifiers = new List<IPeerIdentifier>();
                
                answerSection?.EscapedText.ToList().ForEach(rawPid =>
                {
                    var peerChunks = rawPid.Split("|");
                    Guard.Argument(peerChunks).Count(5);
                    seedPeerIdentifiers.Add(new PeerIdentifier(peerChunks));
                });
            });
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

        public void PingSubscriptionHandler(IChanneledMessage<AnySigned> message)
        {
            Logger.Information("processing ping message stream");
            var pingResponse = message.Payload.FromAnySigned<PingResponse>();
            PeerRepository.Add(new Peer
            {
                LastSeen = DateTime.Now,
                PeerIdentifier = new PeerIdentifier(message.Payload.PeerId),
                Reputation = 0
            });

            Logger.Information(message.Payload.TypeUrl);
        }
        
        public void PeerNeighbourSubscriptionHandler(IChanneledMessage<AnySigned> message)
        {
            Logger.Information("processing peer neighbour message stream");
            var peerNeighborsResponse = message.Payload.FromAnySigned<PeerNeighborsResponse>();
        }

        private async Task PeerCrawler()
        {
            if (Peers.Count == 0)
            {
                try
                {
                    await GetSeedNodesFromDns(SeedNodes).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Logger.Error(e.Message);
                    throw;
                }
            }
        }
        
        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                Logger.Debug($"Disposing {GetType().Name}");
                PingResponseMessageStream?.Dispose();
            }
        }
    }
}
