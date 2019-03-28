using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Catalyst.Node.Common.Interfaces;
using NSubstitute;
using DnsClient;

namespace Catalyst.Node.Common.UnitTests.TestUtils
{
    public class MockDns : IDns
    {
        private readonly ILookupClient _lookupClient;

        public MockDns()
        {
            _lookupClient = Substitute.For<ILookupClient>();
            MockQueryResponse.CreateFakeLookupResult("seed1.catalystnetwork.io", "192.0.2.2:42069",
                "seed1.catalystnetwork.io", _lookupClient);
        }
        
        public async Task<IList<IDnsQueryResponse>> GetTxtRecords(IList<string> hostnames)
        {
            var queries = hostnames.Select(GetTxtRecords).ToArray();
            var responses = await Task.WhenAll(queries);

            return responses.Where(c => c != null).ToList();
        }

        public Task<IDnsQueryResponse> GetTxtRecords(string hostname)
        {
            return Query(hostname, QueryType.TXT);
        }
        
        private async Task<IDnsQueryResponse> Query(string hostname, QueryType type)
        {

            return await _lookupClient.QueryAsync(hostname, type);
        }
    }
}
