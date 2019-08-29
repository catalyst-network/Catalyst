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
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Catalyst.Core.Network;
using Catalyst.TestUtils;
using FluentAssertions;
using Xunit;

namespace Catalyst.Core.IntegrationTests.Network
{
    public sealed class IpIntegrationTests
    {
        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public async Task GetPublicIp_should_not_wait_for_longest_response()
        {
            var delayedObservable = Ip.DefaultIpEchoUrls
               .Select((o, i) =>
                {
                    return i != 2
                        ? Observable.Timer(TimeSpan.FromSeconds(5)).Select(_ => o)
                        : Observable.Return(o);
                }).Merge();

            var stopWatch = new Stopwatch();

            stopWatch.Start();
            var myIp = await Ip.GetPublicIpAsync(delayedObservable);
            stopWatch.Stop();

            myIp.Should().NotBe(default(IPAddress));
            stopWatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(3));
        }

        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public async Task GetPublicIp_should_tolerate_echo_failure()
        {
            var echoUrlWithFailure = new[] {"https://this.will.fail.for.sure"}
               .Concat(Ip.DefaultIpEchoUrls).ToObservable();
            var myIp = await Ip.GetPublicIpAsync(echoUrlWithFailure);
            myIp.Should().NotBe(default(IPAddress));
        }

        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public async Task GetPublicIp_should_usually_return_a_valid_ip()
        {
            var myIp = await Ip.GetPublicIpAsync();
            myIp.Should().NotBe(default(IPAddress));
        }
    }
}
