using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Dawn;
using DnsClient;

namespace Catalyst.Node.Core.Helpers.Network
{
    //Allow passing in the ipAddress as a string in DI config files.
    public class InjectableLookupClient : LookupClient
    {
        public InjectableLookupClient(string ipAddress, int port)
            :base(IPAddress.Parse(ipAddress), port) {}
    }

    public sealed class Dns : IDns
    {
        private readonly ILookupClient _client;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dnsServer"></param>
        /// <param name="client"></param>
        public Dns(ILookupClient client)
        {
            Guard.Argument(client, nameof(client)).NotNull();
            _client = client;
        }

        public async Task<IList<IDnsQueryResponse>> GetTxtRecords(List<string> hostnames)
        {
            Guard.Argument(hostnames, nameof(hostnames))
               .NotEmpty()
               .NotNull()
               .DoesNotContainNull();
            
            var queries = hostnames.Select(GetTxtRecords).ToArray();
            var responses = await Task.WhenAll(queries);

            return responses.Where(c => c != null).ToList();
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

            try
            {
                return await _client.QueryAsync(hostname, type);
            }
            catch (System.Exception)
            {
                return null;
            }
        }
    }
}
