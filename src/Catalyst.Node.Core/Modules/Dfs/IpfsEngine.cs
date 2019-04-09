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
using System.Security;
using System.Threading.Tasks;
using Catalyst.Node.Common.Interfaces;
using Dawn;
using Ipfs;
using Ipfs.CoreApi;
using Ipfs.Engine;
using Serilog;

namespace Catalyst.Node.Core.Modules.Dfs {
    /// <summary>
    /// Simply a wrapper around the Ipfs.Engine.IpfsEngine to allow us coding
    /// and testing against an interface.
    /// </summary>
    public class IpfsEngine : IIpfsEngine, IDisposable {

        public static readonly string KeyChainDefaultKeyType = "ed25519";

        private readonly ILogger _logger;
        private readonly Ipfs.Engine.IpfsEngine _ipfsEngine;
        private readonly SecureString _passphrase;

        public IpfsEngine(IPasswordReader passwordReader, IPeerSettings peerSettings, IFileSystem fileSystem, ILogger logger)
        {
            Guard.Argument(peerSettings, nameof(peerSettings)).NotNull()
               .Require(p => p.SeedServers != null && p.SeedServers.Count > 0,
                    p => $"{nameof(peerSettings)} needs to specify at least one seed server.");

            _logger = logger;
            _passphrase = passwordReader.ReadSecurePassword("Please provide your IPFS password");
            _ipfsEngine = new Ipfs.Engine.IpfsEngine(_passphrase);
            //_ipfsEngine.Options.KeyChain.DefaultKeyType = KeyChainDefaultKeyType;
            //_ipfsEngine.Options.Repository.Folder = Path.Combine(
            //    fileSystem.GetCatalystHomeDir().FullName,
            //    Core.Config.Constants.IpfsSubFolder);
            //_ipfsEngine.Options.Discovery.BootstrapPeers = peerSettings
            //   .SeedServers
            //   .Select(s => $"/dns/{s}/tcp/4001")
            //   .Select(ma => new MultiAddress(ma))
            //   .ToArray();

            //_ipfsEngine.StartAsync().GetAwaiter().GetResult();
        
            //_logger.Information("IPFS engine started.");
        }
        public IBitswapApi Bitswap => _ipfsEngine.Bitswap;

        public IBlockApi Block => _ipfsEngine.Block;

        public IBootstrapApi Bootstrap => _ipfsEngine.Bootstrap;

        public IConfigApi Config => _ipfsEngine.Config;

        public IDagApi Dag => _ipfsEngine.Dag;

        public IDhtApi Dht => _ipfsEngine.Dht;

        public IDnsApi Dns => _ipfsEngine.Dns;

        public IFileSystemApi FileSystem => _ipfsEngine.FileSystem;

        public IGenericApi Generic => _ipfsEngine.Generic;

        public IKeyApi Key => _ipfsEngine.Key;

        public INameApi Name => _ipfsEngine.Name;

        public IObjectApi Object => _ipfsEngine.Object;

        public IPinApi Pin => _ipfsEngine.Pin;

        public IPubSubApi PubSub => _ipfsEngine.PubSub;

        public IStatsApi Stats => _ipfsEngine.Stats;

        public ISwarmApi Swarm => _ipfsEngine.Swarm;

        public async Task StartAsync() { await _ipfsEngine.StartAsync(); }
        public async Task StopAsync() { await _ipfsEngine.StopAsync(); }
        public IpfsEngineOptions Options => _ipfsEngine.Options;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) {return;}

            _ipfsEngine?.Dispose();
            _passphrase?.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
