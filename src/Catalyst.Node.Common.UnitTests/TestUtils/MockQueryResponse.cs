/*
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

using System.Collections.Generic;
using System.Threading.Tasks;
using DnsClient;
using DnsClient.Protocol;
using NSubstitute;

namespace Catalyst.Node.Common.UnitTests.TestUtils
{
    public static class MockQueryResponse
    {
        public static void CreateFakeLookupResult(string domainName, string seed, string value, ILookupClient lookupClient)
        {
            var queryResponse = Substitute.For<IDnsQueryResponse>();
            var answers = new List<DnsResourceRecord>
            {
                new TxtRecord(new ResourceRecordInfo(domainName, ResourceRecordType.TXT, QueryClass.CS, 10, 32),
                    new[] {seed}, new[] {value}
                )
            };

            queryResponse.Answers.Returns(answers);
            lookupClient.QueryAsync(Arg.Is(domainName), Arg.Any<QueryType>())
               .Returns(Task.FromResult(queryResponse));
        }
    }
}