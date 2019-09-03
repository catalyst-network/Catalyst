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

using System.Threading.Tasks;
using Catalyst.Core.Network;
using DnsClient;
using NSubstitute;

namespace Catalyst.TestUtils
{
    public static class MockQueryResponse
    {
        /// <summary>
        ///     Method mocks the response from a DNS server when querying for TXT records.
        /// </summary>
        /// <param name="domainName"></param>
        /// <param name="value"></param>
        /// <param name="lookupClient"></param>
        public static void CreateFakeLookupResult(string domainName, string value, ILookupClient lookupClient)
        {
            var queryResponse = Substitute.For<IDnsQueryResponse>();
            var answers = DevDnsQueryResponse.BuildDnsResourceRecords(domainName, value);

            queryResponse.Answers.Returns(answers);
            lookupClient.QueryAsync(Arg.Is(domainName), Arg.Any<QueryType>())
               .Returns(Task.FromResult(queryResponse));
        }
    }
}
