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
using System.Threading.Tasks;
using Catalyst.Common.Interfaces.Network;
using Dawn;
using DnsClient;

namespace Catalyst.Common.Network
{
    public sealed class DnsClient : IDns
    {
        private readonly ILookupClient _client;

        /// <summary>
        /// </summary>
        /// <param name="client"></param>
        public DnsClient(ILookupClient client)
        {
            Guard.Argument(client, nameof(client)).NotNull();
            _client = client;
        }

        public async Task<IList<IDnsQueryResponse>> GetTxtRecords(IList<string> hostnames)
        {
            Guard.Argument(hostnames, nameof(hostnames))
               .NotNull()
               .NotEmpty()
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
            catch (Exception)
            {
                return null;
            }
        }
    }
}
