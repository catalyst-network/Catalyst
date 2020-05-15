using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Mono.Nat;

namespace Catalyst.UPnP.Tests.TestUtils
{
    public class TestNatDeviceWithInternalMappings : INatDevice
    {
        private readonly List<Mapping> _mappings;
        
        public TestNatDeviceWithInternalMappings(IEnumerable<Mapping> existingMappings)
        {
            _mappings = existingMappings.ToList();
        }
        
        public Task<Mapping> CreatePortMapAsync(Mapping mapping)
        {
            _mappings.Add(mapping);
            return Task.FromResult(mapping);
        }

        public Task<Mapping> DeletePortMapAsync(Mapping mapping)
        {
            if (!_mappings.Contains(mapping)) return null;
            _mappings.Remove(mapping);
            return Task.FromResult(mapping);
        }

        public Task<Mapping[]> GetAllMappingsAsync()
        {
            return Task.FromResult(_mappings.ToArray());
        }

        public Task<IPAddress> GetExternalIPAsync()
        {
            throw new NotImplementedException();
        }

        public Task<Mapping> GetSpecificMappingAsync(Protocol protocol, int publicPort)
        {
            throw new NotImplementedException();
        }

        public IPEndPoint DeviceEndpoint { get; }
        public DateTime LastSeen { get; }
        public NatProtocol NatProtocol { get; }
    }
}
