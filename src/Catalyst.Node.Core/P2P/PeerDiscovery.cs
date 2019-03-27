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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Catalyst.Node.Common;
using Catalyst.Node.Common.Helpers.Extensions;
using Catalyst.Node.Common.Helpers.Network;
using Catalyst.Node.Common.Interfaces;
using DnsClient.Protocol;
using Microsoft.Extensions.Configuration;
using Serilog;
using SharpRepository.Repository;

namespace Catalyst.Node.Core.P2P
{
    public class PeerDiscovery : IPeerDiscovery
    {
        public IDns Dns { get; }
        public ILogger Logger { get; }
        public List<string> SeedNodes { get; }
        public List<IPEndPoint> Peers { get; }
        public IRepository<Peer> PeerRepository { get; }

        private IPeer LastPeer { get; }

        /// <summary>
        /// </summary>
        /// <param name="dns"></param>
        /// <param name="repository"></param>
        /// <param name="rootSection"></param>
        /// <param name="logger"></param>
        public PeerDiscovery(IDns dns, IRepository<Peer> repository, IConfigurationRoot rootSection, ILogger logger)
        {
            Dns = dns;
            Logger = logger;
            PeerRepository = repository;
            SeedNodes = new List<string>();
            Peers = new List<IPEndPoint>();

            ParseDnsServersFromConfig(rootSection);
            
            var longRunningTasks = new [] {PeerCrawler()};
            Task.WaitAll(longRunningTasks);
        }

        public void ParseDnsServersFromConfig(IConfigurationRoot rootSection)
        {
            try
            {
                foreach (var seedNode in rootSection.GetSection("CatalystNodeConfiguration")
                   .GetSection("Peer")
                   .GetSection("SeedServers")
                   .GetChildren()
                   .Select(p => p.Value).ToList())
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
        public async Task GetSeedNodesFromDns(List<string> seedServers)
        {
            foreach (var seedNode in seedServers)
            {
                var dnsQueryAnswer = await Dns.GetTxtRecords(seedNode).ConfigureAwait(false);

                var answerSection = (TxtRecord) dnsQueryAnswer.Answers.FirstOrDefault();
                if (answerSection != null)
                {
                    // need to ping these seed nodes here.
                    Peers.Add(EndpointBuilder.BuildNewEndPoint(answerSection.EscapedText.FirstOrDefault()));
                }
            }
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
    }
}