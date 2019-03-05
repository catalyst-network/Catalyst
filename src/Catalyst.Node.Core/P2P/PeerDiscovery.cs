using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Catalyst.Node.Common.Helpers.Network;
using Catalyst.Node.Common.Interfaces;
using DnsClient.Protocol;
using SharpRepository.Repository;

namespace Catalyst.Node.Core.P2P
{
    public class PeerDiscovery : IPeerDiscovery
    {
        private readonly IDns _dns;

        private readonly IRepository<Peer> _peerRepository;

        /// <summary>
        /// </summary>
        /// <param name="dns"></param>
        /// <param name="repository"></param>
        public PeerDiscovery(IDns dns, IRepository<Peer> repository)
        {
            _dns = dns;
            SeedNodes = new List<IPEndPoint>();
            _peerRepository = repository;
        }

        private List<IPEndPoint> SeedNodes { get; }

        /// <summary>
        /// </summary>
        /// <param name="seedServers"></param>
        internal async Task GetSeedNodes(List<string> seedServers)
        {
            var dnsQueryAnswers = await _dns.GetTxtRecords(seedServers);
            foreach (var dnsQueryAnswer in dnsQueryAnswers)
            {
                var answerSection = (TxtRecord) dnsQueryAnswer.Answers.FirstOrDefault();
                if (answerSection != null)
                    SeedNodes.Add(EndpointBuilder.BuildNewEndPoint(answerSection.EscapedText.FirstOrDefault()));
            }
        }
    }
}