using System.Collections.Generic;
using System.Linq;
using System.Net;
using Catalyst.Node.Core.Helpers.Network;
using DnsClient.Protocol;
using SharpRepository.Repository;
using SharpRepository.Repository.Configuration;
using Dns = Catalyst.Node.Core.Helpers.Network.Dns;

namespace Catalyst.Node.Core.P2P
{
    public class PeerDiscovery
    {
        private Dns Dns;
        private List<IPEndPoint> SeedNodes { get; }

        private readonly IRepository<Peer, int> _peerRepository;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dns"></param>
        /// <param name="repositoryConfiguration"></param>
        public PeerDiscovery(Dns dns, ISharpRepositoryConfiguration repositoryConfiguration)
        {
            Dns = dns;
            SeedNodes = new List<IPEndPoint>();
            _peerRepository = RepositoryFactory.GetInstance<Peer, int>(repositoryConfiguration, "PeerRepository");
        }
        
        /// <summary>
        /// </summary>
        /// <param name="seedServers"></param>
        internal void GetSeedNodes(List<string> seedServers)
        {
            var dnsQueryAnswers = Dns.GetTxtRecords(seedServers);
            foreach (var dnsQueryAnswer in dnsQueryAnswers)
            {
                var answerSection = (TxtRecord) dnsQueryAnswer.Answers.FirstOrDefault();
                if (answerSection != null)
                {
                    SeedNodes.Add(EndpointBuilder.BuildNewEndPoint(answerSection.EscapedText.FirstOrDefault()));
                }
            }
        }
    }
}