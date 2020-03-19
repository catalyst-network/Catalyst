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
using Catalyst.Core.Modules.Dfs.Tests.Utils;
using MultiFormats;
using NUnit.Framework;

namespace Catalyst.Core.Modules.Dfs.Tests.IntegrationTests.CoreApi
{
    public class BootstapApiTest
    {
        private IDfsService ipfs;
        private readonly MultiAddress somewhere = "/ip4/127.0.0.1/tcp/4009/ipfs/QmPv52ekjS75L4JmHpXVeuJ5uX2ecSfSZo88NSyxwA3rAQ";

        public BootstapApiTest()
        {
            ipfs = TestDfs.GetTestDfs();    
        }
        
        [Test]
        public async Task Add_Remove()
        {
            var addr = await ipfs.BootstrapApi.AddAsync(somewhere);
            Assert.NotNull(addr);
            Assert.AreEqual(somewhere, addr);
            var addrs = await ipfs.BootstrapApi.ListAsync();
            Assert.True(addrs.Any(a => a == somewhere));

            addr = await ipfs.BootstrapApi.RemoveAsync(somewhere);
            Assert.NotNull(addr);
            Assert.AreEqual(somewhere, addr);
            addrs = await ipfs.BootstrapApi.ListAsync();
            Assert.False(addrs.Any(a => a == somewhere));
        }

        [Test]
        public async Task List()
        {
            var addrs = await ipfs.BootstrapApi.ListAsync();
            Assert.NotNull(addrs);
            Assert.AreNotEqual(0, addrs.Count());
        }

        [Test]
        public async Task Remove_All()
        {
            var original = await ipfs.BootstrapApi.ListAsync();
            await ipfs.BootstrapApi.RemoveAllAsync();
            var addrs = await ipfs.BootstrapApi.ListAsync();
            Assert.AreEqual(0, addrs.Count());
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
                Assert.AreNotEqual(0, addrs.Count());
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
                Assert.AreEqual(0, addrs.Count());

                ipfs.Options.Discovery.BootstrapPeers = new[]
                    {somewhere};
                addrs = await ipfs.BootstrapApi.ListAsync();
                Assert.AreEqual(1, addrs.Count());
                Assert.AreEqual(somewhere, addrs.First());
            }
            finally
            {
                ipfs.Options.Discovery.BootstrapPeers = original;
            }
        }
    }
}
