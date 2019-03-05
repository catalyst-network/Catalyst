using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Catalyst.Node.Common.Helpers.Network;
using Catalyst.Node.Common.UnitTests.TestUtils;
using FluentAssertions;
using Xunit;

namespace Catalyst.Node.Common.UnitTests.Helpers.Network
{
    public class IpTests
    {
        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public async Task GetPublicIp_should_usually_return_a_valid_ip()
        {
            var myIp = await Ip.GetPublicIpAsync();
            myIp.Should().NotBe(default(IPAddress));
        }

        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public async Task GetPublicIp_should_tolerate_echo_failure()
        {
            var echoUrlWithFailure = new []{"https://this.will.fail.for.sure"}
               .Concat(Ip.DefaultIpEchoUrls).ToObservable();
            var myIp = await Ip.GetPublicIpAsync(echoUrlWithFailure);
            myIp.Should().NotBe(default(IPAddress));
        }
        
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
    }
}