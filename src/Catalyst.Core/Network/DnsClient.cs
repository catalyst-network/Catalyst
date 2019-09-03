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
using Catalyst.Abstractions.P2P;
using Catalyst.Core.P2P;
using Dawn;
using DnsClient;
using DnsClient.Protocol;
using Nethereum.Hex.HexConvertors.Extensions;

namespace Catalyst.Core.Network
{
    public sealed class DnsClient : IDns
    {
        private readonly ILookupClient _client;
        private readonly IPeerIdValidator _peerIdValidator;

        /// <summary>
        /// </summary>
        /// <param name="client"></param>
        /// <param name="peerIdValidator"></param>
        public DnsClient(ILookupClient client, IPeerIdValidator peerIdValidator)
        {
            Guard.Argument(client, nameof(client)).NotNull();
            _client = client;
            _peerIdValidator = peerIdValidator;
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
        public IEnumerable<IPeerIdentifier> GetSeedNodesFromDns(IEnumerable<string> seedServers)
        {
            var peers = new List<IPeerIdentifier>();
            seedServers.ToList().ForEach(async seedServer =>
            {
                var dnsQueryAnswer = await GetTxtRecordsAsync(seedServer).ConfigureAwait(false);
                var answerSection = (TxtRecord) dnsQueryAnswer.Answers.FirstOrDefault();
        
                Guard.Argument(answerSection.EscapedText).NotNull().Count(1);        
                answerSection.EscapedText.ToList().ForEach(hexPid =>
                {
                    var peerChunks = hexPid.HexToUTF8String().Split(PeerIdentifier.PidDelimiter);
                    _peerIdValidator.ValidateRawPidChunks(peerChunks);

                    var peerIdentifier = PeerIdentifier.ParseHexPeerIdentifier(peerChunks);
                    peers.Add(peerIdentifier);
                });
        
                Guard.Argument(peers).MinCount(1);
            });
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
