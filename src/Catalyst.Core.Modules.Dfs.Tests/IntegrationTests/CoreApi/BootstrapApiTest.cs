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
using Catalyst.Core.Modules.Dfs.CoreApi;
using Catalyst.Core.Modules.Dfs.Tests.Utils;
using MultiFormats;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.Modules.Dfs.Tests.IntegrationTests.CoreApi
{
    public class BootstapApiTest
    {
        private readonly IDfsService ipfs;

        private readonly MultiAddress somewhere =
            "/ip4/127.0.0.1/tcp/4009/ipfs/QmPv52ekjS75L4JmHpXVeuJ5uX2ecSfSZo88NSyxwA3rAQ";

        public BootstapApiTest(ITestOutputHelper output)
        {
            BootstrapApi.Defaults = new MultiAddress[]
            {
                "/ip4/104.131.131.82/tcp/4001/ipfs/QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ",            // mars.i.ipfs.io
                "/ip4/104.236.179.241/tcp/4001/ipfs/QmSoLPppuBtQSGwKDZT2M73ULpjvfd3aZ6ha4oFGL1KrGM",           // pluto.i.ipfs.io
                "/ip4/128.199.219.111/tcp/4001/ipfs/QmSoLSafTMBsPKadTEgaXctDQVcqN88CNLHXMkTNwMKPnu",           // saturn.i.ipfs.io
                "/ip4/104.236.76.40/tcp/4001/ipfs/QmSoLV4Bbm51jM9C4gDYZQ9Cy3U6aXMJDAbzgu2fzaDs64",             // venus.i.ipfs.io
                "/ip4/178.62.158.247/tcp/4001/ipfs/QmSoLer265NRgSp2LA3dPaeykiS1J6DifTC88f5uVQKNAd",            // earth.i.ipfs.io
                "/ip6/2604:a880:1:20::203:d001/tcp/4001/ipfs/QmSoLPppuBtQSGwKDZT2M73ULpjvfd3aZ6ha4oFGL1KrGM",  // pluto.i.ipfs.io
                "/ip6/2400:6180:0:d0::151:6001/tcp/4001/ipfs/QmSoLSafTMBsPKadTEgaXctDQVcqN88CNLHXMkTNwMKPnu",  // saturn.i.ipfs.io
                "/ip6/2604:a880:800:10::4a:5001/tcp/4001/ipfs/QmSoLV4Bbm51jM9C4gDYZQ9Cy3U6aXMJDAbzgu2fzaDs64", // venus.i.ipfs.io
                "/ip6/2a03:b0c0:0:1010::23:1001/tcp/4001/ipfs/QmSoLer265NRgSp2LA3dPaeykiS1J6DifTC88f5uVQKNAd"  // earth.i.ipfs.io
            };

            ipfs = TestDfs.GetTestDfs(output);
        }

        [Fact]
        public async Task Add_Remove()
        {
            var addr = await ipfs.BootstrapApi.AddAsync(somewhere);
            Assert.NotNull(addr);
            Assert.Equal(somewhere, addr);
            var addrs = await ipfs.BootstrapApi.ListAsync();
            Assert.True(addrs.Any(a => a == somewhere));

            addr = await ipfs.BootstrapApi.RemoveAsync(somewhere);
            Assert.NotNull(addr);
            Assert.Equal(somewhere, addr);
            addrs = await ipfs.BootstrapApi.ListAsync();
            Assert.False(addrs.Any(a => a == somewhere));
        }

        [Fact]
        public async Task List()
        {
            var addrs = await ipfs.BootstrapApi.ListAsync();
            Assert.NotNull(addrs);
            Assert.NotEqual(0, addrs.Count());
        }

        [Fact]
        public async Task Remove_All()
        {
            var original = await ipfs.BootstrapApi.ListAsync();
            await ipfs.BootstrapApi.RemoveAllAsync();
            var addrs = await ipfs.BootstrapApi.ListAsync();
            Assert.Equal(0, addrs.Count());
            foreach (var addr in original)
            {
                await ipfs.BootstrapApi.AddAsync(addr);
            }
        }

        [Fact]
        public async Task Add_Defaults()
        {
            var original = await ipfs.BootstrapApi.ListAsync();
            await ipfs.BootstrapApi.RemoveAllAsync();
            try
            {
                await ipfs.BootstrapApi.AddDefaultsAsync();
                var addrs = await ipfs.BootstrapApi.ListAsync();
                Assert.NotEqual(0, addrs.Count());
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

        [Fact]
        public async Task Override_FactoryDefaults()
        {
            var original = ipfs.Options.Discovery.BootstrapPeers;
            try
            {
                ipfs.Options.Discovery.BootstrapPeers = new MultiAddress[0];
                var addrs = await ipfs.BootstrapApi.ListAsync();
                Assert.Equal(0, addrs.Count());

                ipfs.Options.Discovery.BootstrapPeers = new[]
                    {somewhere};
                addrs = await ipfs.BootstrapApi.ListAsync();
                Assert.Equal(1, addrs.Count());
                Assert.Equal(somewhere, addrs.First());
            }
            finally
            {
                ipfs.Options.Discovery.BootstrapPeers = original;
            }
        }
    }
}
