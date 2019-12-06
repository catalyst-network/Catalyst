using System;
using Lib.P2P.Protocols;
using Xunit;

namespace Catalyst.Core.Modules.Dfs.Tests.CoreApi
{
    public class PingResultTest
    {
        [Fact]
        public void Properties()
        {
            var time = TimeSpan.FromSeconds(3);
            var r = new PingResult
            {
                Success = true,
                Text = "ping",
                Time = time
            };
            Assert.Equal(true, r.Success);
            Assert.Equal("ping", r.Text);
            Assert.Equal(time, r.Time);
        }
    }
}
