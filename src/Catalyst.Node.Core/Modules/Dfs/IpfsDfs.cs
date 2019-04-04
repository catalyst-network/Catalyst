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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Common.Interfaces.Modules.Dfs;
using Ipfs;
using Ipfs.CoreApi;
using Ipfs.Engine;
using PeerTalk;

namespace Catalyst.Node.Core.Modules.Dfs
{
    public class IpfsDfs : IIpfsDfs
    {

        private readonly IpfsEngine _ipfsDfs;

        public IpfsDfs(
            IPasswordReader passwordReader,
            IPeerSettings peerSettings)
        {
            var password = passwordReader.ReadSecurePassword("Please provide your IPFS password");
            _ipfsDfs = new IpfsEngine(password);
            _ipfsDfs.Options.KeyChain.DefaultKeyType = "ed25519";
            _ipfsDfs.Options.Repository.Folder = Path.Combine(
                new Common.Helpers.FileSystem.FileSystem().GetCatalystHomeDir().FullName,
                "Ipfs");
            _ipfsDfs.Options.Discovery.BootstrapPeers = peerSettings
                .SeedServers
                .Select(s => $"/dns/{s}/tcp/4001")
                .Select(ma => new MultiAddress(ma))
                .ToArray();
        }

        Task IService.StartAsync() { return this.StartAsync(); }

        Task IDfs.StartAsync(CancellationToken cancellationToken)
        {
            return Task.Run(() => _ipfsDfs.StartAsync(), cancellationToken);
        }

        public Task<IFileSystemNode> AddFileAsync(string filename, CancellationToken cancellationToken = default)
        {
            return _ipfsDfs.FileSystem.AddFileAsync(filename, cancel: cancellationToken);
        }

        public Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default)
        {
            return _ipfsDfs.FileSystem.ReadAllTextAsync(path, cancellationToken);
        }

        public IBitswapApi Bitswap => _ipfsDfs.Bitswap;

        public IBlockApi Block => _ipfsDfs.Block;

        public IBootstrapApi Bootstrap => _ipfsDfs.Bootstrap;

        public IConfigApi Config => _ipfsDfs.Config;

        public IDagApi Dag => _ipfsDfs.Dag;

        public IDhtApi Dht => _ipfsDfs.Dht;

        public IDnsApi Dns => _ipfsDfs.Dns;

        public IFileSystemApi FileSystem => _ipfsDfs.FileSystem;

        public IGenericApi Generic => _ipfsDfs.Generic;

        public IKeyApi Key => _ipfsDfs.Key;

        public INameApi Name => _ipfsDfs.Name;

        public IObjectApi Object => _ipfsDfs.Object;

        public IPinApi Pin => _ipfsDfs.Pin;

        public IPubSubApi PubSub => _ipfsDfs.PubSub;

        public IStatsApi Stats => _ipfsDfs.Stats;

        public ISwarmApi Swarm => _ipfsDfs.Swarm;

        public Task StartAsync() { return _ipfsDfs.StartAsync(); }
        public Task StopAsync() { return _ipfsDfs.StopAsync(); }

        void IDisposable.Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _ipfsDfs.Dispose();
            }
        }
    }
}