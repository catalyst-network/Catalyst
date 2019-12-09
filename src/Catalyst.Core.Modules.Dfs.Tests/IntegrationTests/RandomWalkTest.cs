using System;
using System.Threading.Tasks;
using Xunit;

namespace Catalyst.Core.Modules.Dfs.Tests.IntegrationTests
{
    public class RandomWalkTest
    {
        [Fact]
        public async Task CanStartAndStop()
        {
            var walk = new RandomWalk();
            await walk.StartAsync();
            await walk.StopAsync();

            await walk.StartAsync();
            await walk.StopAsync();
        }

        [Fact]
        public void CannotStartTwice()
        {
            var walk = new RandomWalk();
            walk.StartAsync().Wait();
            ExceptionAssert.Throws<Exception>(() => { walk.StartAsync().Wait(); });
        }

        [Fact]
        public async Task CanStopMultipletimes()
        {
            var walk = new RandomWalk();
            await walk.StartAsync();
            await walk.StopAsync();
            await walk.StopAsync();
            await walk.StartAsync();
            await walk.StopAsync();
        }
    }
}
