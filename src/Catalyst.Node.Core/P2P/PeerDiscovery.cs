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
using System.Net;
using System.Threading.Tasks;
using Catalyst.Node.Common.Helpers.Config;
using Catalyst.Node.Common.Helpers.Network;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Common.Interfaces.P2P;
using DnsClient.Protocol;
using Microsoft.Extensions.Configuration;
using Serilog;
using SharpRepository.Repository;
using Peer = Catalyst.Node.Common.Peer;

namespace Catalyst.Node.Core.P2P
{
    public sealed class PeerDiscovery : IPeerDiscovery
    {
        public IDns Dns { get; }
        public ILogger Logger { get; }
        public IList<string> SeedNodes { get; }
        public IList<IPEndPoint> Peers { get; }
        public IRepository<Peer> PeerRepository { get; }

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
        public async Task GetSeedNodesFromDns(IList<string> seedServers)
        {
            foreach (var seedServer in seedServers)
            {
                var dnsQueryAnswer = await Dns.GetTxtRecords(seedServer).ConfigureAwait(false);

                var answerSection = (TxtRecord) dnsQueryAnswer.Answers.FirstOrDefault();
                if (answerSection != null)
                {
                    foreach (var seedNode in answerSection.EscapedText)
                    {
                        var pingResponse = true;
                        if (pingResponse == true) // pointless but place holder until we have a ping system
                        {
                            Peers.Add(EndpointBuilder.BuildNewEndPoint(seedNode));
                        }
                    }
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
