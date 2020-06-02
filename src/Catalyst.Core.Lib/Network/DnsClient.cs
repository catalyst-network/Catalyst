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
using Catalyst.Abstractions.Network;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Protocol.Peer;
using Dawn;
using DnsClient;
using DnsClient.Protocol;
using MultiFormats;

namespace Catalyst.Core.Lib.Network
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

        public async Task<IList<IDnsQueryResponse>> GetTxtRecordsAsync(IList<string> hostnames)
        {
            Guard.Argument(hostnames, nameof(hostnames))
               .NotNull()
               .NotEmpty()
               .DoesNotContainNull();

            var queries = hostnames.Select(GetTxtRecordsAsync).ToArray();
            var responses = await Task.WhenAll(queries);

            return responses.Where(c => c != null).ToList();
        }

        public async Task<IDnsQueryResponse> GetTxtRecordsAsync(string hostname)
        {
            Guard.Argument(hostname, nameof(hostname))
               .NotNull()
               .NotEmpty()
               .NotWhiteSpace();

            return await QueryAsync(hostname, QueryType.TXT).ConfigureAwait(false);
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="seedServers"></param>
        public async Task<IEnumerable<MultiAddress>> GetSeedNodesFromDnsAsync(IEnumerable<string> seedServers)
        {
            var peers = new List<MultiAddress>();

            async Task Action(string seedServer)
            {
                var dnsQueryAnswer = await GetTxtRecordsAsync(seedServer).ConfigureAwait(false);
                var answerSection = (TxtRecord) dnsQueryAnswer.Answers.FirstOrDefault();

                Guard.Argument(answerSection?.EscapedText).NotNull().Count(1);
                answerSection?.EscapedText.ToList()
                   .ForEach(stringPid => peers.Add(new MultiAddress(stringPid)));

                Guard.Argument(peers).MinCount(1);
            }

            var tasks = seedServers.Select(Action).ToList();
            await Task.WhenAll(tasks);
            return peers;
        }

        private async Task<IDnsQueryResponse> QueryAsync(string hostname, QueryType type)
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
