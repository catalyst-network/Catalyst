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

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lib.P2P.Routing;

namespace Lib.P2P.Tests.Routing
{
    public class DistributedQueryTest
    {
        [Test]
        public async Task Cancelling()
        {
            var dquery = new DistributedQuery<Peer>
            {
                Dht = new DhtService()
            };
            var cts = new CancellationTokenSource();
            cts.Cancel();
            await dquery.RunAsync(cts.Token);
            Assert.That(dquery.Answers.Count(), Is.EqualTo(0));
        }

        [Test]
        public void UniqueId()
        {
            var q1 = new DistributedQuery<Peer>();
            var q2 = new DistributedQuery<Peer>();
            Assert.That(q1.Id, Is.Not.EqualTo(q2.Id));
        }
    }
}
