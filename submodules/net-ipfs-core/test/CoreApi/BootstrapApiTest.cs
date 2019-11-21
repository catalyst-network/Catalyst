using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiFormats;

namespace Ipfs.Core.Tests.CoreApi
{
    [TestClass]
    public class BootstapApiTest
    {
        IpfsEngine _ipfs = TestFixture.Ipfs;
        MultiAddress _somewhere = "/ip4/127.0.0.1/tcp/4009/ipfs/QmPv52ekjS75L4JmHpXVeuJ5uX2ecSfSZo88NSyxwA3rAQ";

        [TestMethod]
        public async Task Add_Remove()
        {
            var addr = await _ipfs.Bootstrap.AddAsync(_somewhere);
            Assert.IsNotNull(addr);
            Assert.AreEqual(_somewhere, addr);
            var addrs = await _ipfs.Bootstrap.ListAsync();
            Assert.IsTrue(addrs.Any(a => a == _somewhere));

            addr = await _ipfs.Bootstrap.RemoveAsync(_somewhere);
            Assert.IsNotNull(addr);
            Assert.AreEqual(_somewhere, addr);
            addrs = await _ipfs.Bootstrap.ListAsync();
            Assert.IsFalse(addrs.Any(a => a == _somewhere));
        }

        [TestMethod]
        public async Task List()
        {
            var addrs = await _ipfs.Bootstrap.ListAsync();
            Assert.IsNotNull(addrs);
            Assert.AreNotEqual(0, addrs.Count());
        }

        [TestMethod]
        public async Task Remove_All()
        {
            var original = await _ipfs.Bootstrap.ListAsync();
            await _ipfs.Bootstrap.RemoveAllAsync();
            var addrs = await _ipfs.Bootstrap.ListAsync();
            Assert.AreEqual(0, addrs.Count());
            foreach (var addr in original)
            {
                await _ipfs.Bootstrap.AddAsync(addr);
            }
        }

        [TestMethod]
        public async Task Add_Defaults()
        {
            var original = await _ipfs.Bootstrap.ListAsync();
            await _ipfs.Bootstrap.RemoveAllAsync();
            try
            {
                await _ipfs.Bootstrap.AddDefaultsAsync();
                var addrs = await _ipfs.Bootstrap.ListAsync();
                Assert.AreNotEqual(0, addrs.Count());
            }
            finally
            {
                await _ipfs.Bootstrap.RemoveAllAsync();
                foreach (var addr in original)
                {
                    await _ipfs.Bootstrap.AddAsync(addr);
                }
            }
        }

        [TestMethod]
        public async Task Override_FactoryDefaults()
        {
            var original = _ipfs.Options.Discovery.BootstrapPeers;
            try
            {
                _ipfs.Options.Discovery.BootstrapPeers = new MultiAddress[0];
                var addrs = await _ipfs.Bootstrap.ListAsync();
                Assert.AreEqual(0, addrs.Count());

                _ipfs.Options.Discovery.BootstrapPeers = new MultiAddress[1]
                    {_somewhere};
                addrs = await _ipfs.Bootstrap.ListAsync();
                Assert.AreEqual(1, addrs.Count());
                Assert.AreEqual(_somewhere, addrs.First());
            }
            finally
            {
                _ipfs.Options.Discovery.BootstrapPeers = original;
            }
        }
    }
}
