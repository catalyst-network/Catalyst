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
using Mono.Nat;

namespace Catalyst.UPnP.Tests.Utils
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

        public Task<Mapping> GetSpecificMappingAsync(Mono.Nat.Protocol protocol, int publicPort)
        {
            throw new NotImplementedException();
        }

        public IPEndPoint DeviceEndpoint { get; }
        public DateTime LastSeen { get; }
        public NatProtocol NatProtocol { get; }
    }
}
