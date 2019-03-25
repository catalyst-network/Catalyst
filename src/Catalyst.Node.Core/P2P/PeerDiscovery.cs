/*
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

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Catalyst.Node.Common.Helpers.Network;
using Catalyst.Node.Common.Interfaces;
using DnsClient.Protocol;
using Microsoft.Extensions.Configuration;
using SharpRepository.Repository;

namespace Catalyst.Node.Core.P2P
{
    public class PeerDiscovery : IPeerDiscovery
    {
        private readonly IDns _dns;
        private readonly IRepository<Peer> _peerRepository;
        private readonly List<IEnumerable<string>> _seedNodes;
        
        private List<IPEndPoint> Peers { get; set; }

        /// <summary>
        /// </summary>
        /// <param name="dns"></param>
        /// <param name="repository"></param>
        /// <param name="rootSection"></param>
        public PeerDiscovery(IDns dns, IRepository<Peer> repository, IConfigurationRoot rootSection)
        {
            _dns = dns;
            _seedNodes = new List<IEnumerable<string>>();
            
            _seedNodes.Add(rootSection.GetSection("CatalystNodeConfiguration")
               .GetSection("Peer")
               .GetSection("SeedServers")
               .GetChildren()
               .Select(p => p.Value)
            );
            
            _peerRepository = repository;
            var longRunningTasks = new [] {PeerCrawler()};
            Task.WaitAll(longRunningTasks);
        }

        /// <summary>
        /// </summary>
        /// <param name="seedServers"></param>
        private async Task GetSeedNodes(List<IEnumerable<string>> seedServers)
        {
            foreach (var seedNode in seedServers)
            {
                var dnsQueryAnswer = await _dns.GetTxtRecords(seedNode.ToString());

                var answerSection = (TxtRecord) dnsQueryAnswer.Answers.FirstOrDefault();
                if (answerSection != null)
                {
                    Peers.Add(EndpointBuilder.BuildNewEndPoint(answerSection.EscapedText.FirstOrDefault()));
                }
            }
        }

        private async Task PeerCrawler()
        {
            await GetSeedNodes(_seedNodes);

        }
    }
}