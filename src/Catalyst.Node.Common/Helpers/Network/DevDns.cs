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

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Catalyst.Node.Common.Helpers.Config;
using Catalyst.Node.Common.Interfaces;
using DnsClient;
using Microsoft.Extensions.Configuration;

namespace Catalyst.Node.Common.Helpers.Network
{
    public class DevDns : IDns
    {
        private readonly IList<string> _seedServers;
        private readonly IList<IPEndPoint> _dnsQueryAnswerValues;

        public DevDns(IConfigurationRoot configurationRoot)
        {
            _seedServers = ConfigValueParser.GetStringArrValues(configurationRoot, "SeedServers");
            _dnsQueryAnswerValues = ConfigValueParser.GetIpEndpointArrValues(configurationRoot, "QueryAnswerValues");
        }
        
        public async Task<IList<IDnsQueryResponse>> GetTxtRecords(IList<string> hostnames = null)
        {
            var queries = _seedServers.Select(GetTxtRecords).ToArray();
            var responses = await Task.WhenAll(queries).ConfigureAwait(false);

            return responses.Where(c => c != null).ToList();
        }

        public async Task<IDnsQueryResponse> GetTxtRecords(string hostname = "seed1.catalystnetwork.io")
        {
            var devDnsQueryResponse = new DevDnsQueryResponse
            {
                Answers = DevDnsQueryResponse.BuildDnsResourceRecords(hostname, _dnsQueryAnswerValues.FirstOrDefault()?.ToString())
            };
            return await Task.FromResult<IDnsQueryResponse>(devDnsQueryResponse).ConfigureAwait(false);
        }   
    }
}