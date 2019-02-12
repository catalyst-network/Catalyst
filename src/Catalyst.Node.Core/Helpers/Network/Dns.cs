using System;
using System.Collections.Generic;
using System.Net;
using Dawn;
using DnsClient;

namespace Catalyst.Node.Core.Helpers.Network
{
    public class Dns
    {
        private IPEndPoint DnsServer { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dnsServer"></param>
        public Dns(IPEndPoint dnsServer)
        {
            DnsServer = dnsServer;
        }

        /// <summary>
        /// </summary>
        /// <param name="hostnames"></param>
        /// <returns></returns>
        public IList<IDnsQueryResponse> GetTxtRecords(List<string> hostnames)
        {
            Guard.Argument(hostnames, nameof(hostnames)).NotEmpty().NotNull().DoesNotContainNull();
            var recordList = new List<IDnsQueryResponse>();
            foreach (var hostname in hostnames) recordList.Add(GetTxtRecords(hostname));

            return recordList;
        }

        /// <summary>
        /// </summary>
        /// <param name="hostname"></param>
        /// <returns></returns>
        public IDnsQueryResponse GetTxtRecords(Uri hostname)
        {
            Guard.Argument(hostname, nameof(hostname)).NotNull().Http();
            return GetTxtRecords(hostname.Host);
        }

        /// <summary>
        /// </summary>
        /// <param name="hostname"></param>
        /// <returns></returns>
        public IDnsQueryResponse GetTxtRecords(string hostname)
        {
            Guard.Argument(hostname, nameof(hostname)).NotNull().NotEmpty().NotWhiteSpace();
            return Query(hostname, QueryType.TXT);
        }

        /// <summary>
        /// </summary>
        /// <param name="hostname"></param>
        /// <returns></returns>
        public IDnsQueryResponse GetARecords(string hostname)
        {
            Guard.Argument(hostname, nameof(hostname)).NotNull().NotEmpty().NotWhiteSpace();
            return Query(hostname, QueryType.A);
        }

        /// <summary>
        /// </summary>
        /// <param name="hostname"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private IDnsQueryResponse Query(string hostname, QueryType type)
        {
            Guard.Argument(hostname, nameof(hostname)).NotNull().NotEmpty().NotWhiteSpace();
            var client = new LookupClient(DnsServer.Address, DnsServer.Port);
            return client.Query(hostname, type);
        }
    }
}
