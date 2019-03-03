using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Dawn;
using DnsClient;

namespace Catalyst.Node.Core.Helpers.Network
{
    public sealed class Dns : IDns
    {
        private IPEndPoint DnsServer { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dnsServer"></param>
        public Dns(IPEndPoint dnsServer)
        {
            DnsServer = dnsServer;
        }

        public async Task<IList<IDnsQueryResponse>> GetTxtRecords(List<string> hostnames)
        {
            Guard.Argument(hostnames, nameof(hostnames))
               .NotEmpty()
               .NotNull()
               .DoesNotContainNull();
            
            var recordList = new List<IDnsQueryResponse>();
            foreach (var hostname in hostnames)
            {
                recordList.Add(await GetTxtRecords(hostname));
            }
            return recordList;
        }

        public async Task<IDnsQueryResponse> GetTxtRecords(string hostname)
        {
            Guard.Argument(hostname, nameof(hostname))
               .NotNull()
               .NotEmpty()
               .NotWhiteSpace();
               
            return await Query(hostname, QueryType.TXT);
        }

        private async Task<IDnsQueryResponse> Query(string hostname, QueryType type)
        {
            Guard.Argument(hostname, nameof(hostname))
               .NotNull()
               .NotEmpty()
               .NotWhiteSpace();
            
            var client = new LookupClient(DnsServer.Address, DnsServer.Port);
            return await client.QueryAsync(hostname, type);
        }
    }
}
