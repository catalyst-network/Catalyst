using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lib.P2P.Tests
{
    [TestClass]
    public class MessageTrackerTest
    {
        [TestMethod]
        public void Tracking()
        {
            var tracker = new MessageTracker();
            var now = DateTime.Now;
            Assert.IsFalse(tracker.RecentlySeen("a", now));
            Assert.IsTrue(tracker.RecentlySeen("a", now));
            Assert.IsFalse(tracker.RecentlySeen("a", now + tracker.Recent));
        }
    }
}
