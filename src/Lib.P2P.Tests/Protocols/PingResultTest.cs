using System;
using Lib.P2P.Protocols;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lib.P2P.Tests.Protocols
{
    [TestClass]
    public class PingResultTest
    {
        [TestMethod]
        public void Properties()
        {
            var time = TimeSpan.FromSeconds(3);
            var r = new PingResult
            {
                Success = true,
                Text = "ping",
                Time = time
            };
            Assert.AreEqual(true, r.Success);
            Assert.AreEqual("ping", r.Text);
            Assert.AreEqual(time, r.Time);
        }
    }
}
