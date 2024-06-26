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
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs;
using Catalyst.TestUtils;
using MultiFormats;
using NUnit.Framework;

namespace Catalyst.Core.Modules.Dfs.Tests.IntegrationTests.CoreApi
{
    public class BootstapApiTest
    {
        private readonly IDfsService ipfs;

        private readonly MultiAddress somewhere =
            "/ip4/127.0.0.1/tcp/4009/ipfs/QmPv52ekjS75L4JmHpXVeuJ5uX2ecSfSZo88NSyxwA3rAQ";

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            ipfs.Dispose();
        }

        public BootstapApiTest()
        {
            ipfs = TestDfs.GetTestDfs();    
        }
        
        [Test]
        public async Task Add_Remove()
        {
            var addr = await ipfs.BootstrapApi.AddAsync(somewhere);
            Assert.That(addr, Is.Not.Null);
            Assert.That(somewhere, Is.EqualTo(addr));
            var addrs = await ipfs.BootstrapApi.ListAsync();
            Assert.That(addrs.Any(a => a == somewhere), Is.True);

            addr = await ipfs.BootstrapApi.RemoveAsync(somewhere);
            Assert.That(addr, Is.Not.Null);
            Assert.That(somewhere, Is.EqualTo(addr));
            addrs = await ipfs.BootstrapApi.ListAsync();
            Assert.That(addrs.Any(a => a == somewhere), Is.False);
        }

        [Test]
        public async Task List()
        {
            var addrs = await ipfs.BootstrapApi.ListAsync();
            Assert.That(addrs, Is.Not.Null);
            Assert.That(addrs.Count(), Is.Not.EqualTo(0));
        }

        [Test]
        public async Task Remove_All()
        {
            var original = await ipfs.BootstrapApi.ListAsync();
            await ipfs.BootstrapApi.RemoveAllAsync();
            var addrs = await ipfs.BootstrapApi.ListAsync();
            Assert.That(addrs.Count(), Is.EqualTo(0));
            foreach (var addr in original)
            {
                await ipfs.BootstrapApi.AddAsync(addr);
            }
        }

        [Test]
        public async Task Add_Defaults()
        {
            var original = await ipfs.BootstrapApi.ListAsync();
            await ipfs.BootstrapApi.RemoveAllAsync();
            try
            {
                await ipfs.BootstrapApi.AddDefaultsAsync();
                var addrs = await ipfs.BootstrapApi.ListAsync();
                Assert.That(addrs.Count(), Is.Not.EqualTo(0));
            }
            finally
            {
                await ipfs.BootstrapApi.RemoveAllAsync();
                foreach (var addr in original)
                {
                    await ipfs.BootstrapApi.AddAsync(addr);
                }
            }
        }

        [Test]
        public async Task Override_FactoryDefaults()
        {
            var original = ipfs.Options.Discovery.BootstrapPeers;
            try
            {
                ipfs.Options.Discovery.BootstrapPeers = new MultiAddress[0];
                var addrs = await ipfs.BootstrapApi.ListAsync();
                Assert.That(addrs.Count(), Is.EqualTo(0));

                ipfs.Options.Discovery.BootstrapPeers = new[]
                    {somewhere};
                addrs = await ipfs.BootstrapApi.ListAsync();
                Assert.That(addrs.Count(), Is.EqualTo(1));
                Assert.That(somewhere, Is.EqualTo(addrs.First()));
            }
            finally
            {
                ipfs.Options.Discovery.BootstrapPeers = original;
            }
        }
    }
}
