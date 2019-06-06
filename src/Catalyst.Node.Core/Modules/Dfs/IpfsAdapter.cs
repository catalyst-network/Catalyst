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
using Catalyst.Common.Config;
using Catalyst.Common.Interfaces.Cryptography;
using Catalyst.Common.Interfaces.FileSystem;
using Catalyst.Common.Interfaces.P2P;
using Common.Logging.Serilog;
using Dawn;
using Ipfs;
using Ipfs.CoreApi;
using Ipfs.Engine;
using Serilog;

namespace Catalyst.Node.Core.Modules.Dfs
{
    /// <summary>
    ///   Modifies the IPFS behaviour to meet the Catalyst requirements.
    /// </summary>
    /// <remarks>
    ///   The IPFS engine is lazy, it is only started when needed.
    /// </remarks>
    public sealed class IpfsAdapter : ICoreApi, IDisposable
    {
        /// <summary>
        ///   An IPFS implementation, commonly called an IPFS node/
        /// </summary>
        private IpfsEngine _ipfs;

        private bool _isStarted;
        private readonly object _startingLock = new object();
        private readonly ILogger _logger;

        static IpfsAdapter()
        {
            global::Common.Logging.LogManager.Adapter = new SerilogFactoryAdapter(Log.Logger);
        }

        public IpfsAdapter(IPasswordReader passwordReader, 
            IPeerSettings peerSettings, 
            IFileSystem fileSystem, 
            ILogger logger)
        {
            Guard.Argument(peerSettings, nameof(peerSettings)).NotNull()
               .Require(p => p.SeedServers != null && p.SeedServers.Count > 0,
                    p => $"{nameof(peerSettings)} needs to specify at least one seed server.");

            _logger = logger;

            // The passphrase is used to access the private keys.
            var passphrase = passwordReader.ReadSecurePassword("Please provide your IPFS password");
            _ipfs = new IpfsEngine(passphrase);
            _ipfs.Options.KeyChain.DefaultKeyType = Constants.KeyChainDefaultKeyType;

            // The IPFS repository is inside the catalyst home folder.
            _ipfs.Options.Repository.Folder = Path.Combine(
                fileSystem.GetCatalystDataDir().FullName,
                Constants.DfsDataSubDir);

            // The seed nodes for the catalyst network.
            _ipfs.Options.Discovery.BootstrapPeers = peerSettings
               .SeedServers
               .Select(s => $"/dns/{s}/tcp/4001")
               .Select(ma => new MultiAddress(ma))
               .ToArray();

            // Do not use the public IPFS network, use a private network
            // of catalyst only nodes.
            _ipfs.Options.Swarm.PrivateNetworkKey = new PeerTalk.Cryptography.PreSharedKey
            {
                Value = Constants.SwarmKey.ToHexBuffer()
            };

            _logger.Information("IPFS configured.");
        }

        /// <summary>
        ///   Starts the engine if required.
        /// </summary>
        /// <returns>
        ///   The started IPFS Engine.
        /// </returns>
        private IpfsEngine Start()
        {
            if (!_isStarted)
            {
                lock (_startingLock)
                {
                    if (!_isStarted)
                    {
                        _ipfs.Start();
                        _isStarted = true;
                        _logger.Information("IPFS started.");
                    }
                }
            }

            return _ipfs;
        }

        public IBitswapApi Bitswap => Start().Bitswap;

        public IBlockApi Block => Start().Block;

        public IBootstrapApi Bootstrap => Start().Bootstrap;

        public IConfigApi Config => _ipfs.Config;
        public IpfsEngineOptions Options => _ipfs.Options;

        public IDagApi Dag => Start().Dag;

        public IDhtApi Dht => Start().Dht;

        public IDnsApi Dns => Start().Dns;

        public IFileSystemApi FileSystem => Start().FileSystem;

        public IGenericApi Generic => Start().Generic;

        public IKeyApi Key => _ipfs.Key;

        public INameApi Name => Start().Name;

        public IObjectApi Object => Start().Object;

        public IPinApi Pin => Start().Pin;

        public IPubSubApi PubSub => Start().PubSub;

        public IStatsApi Stats => Start().Stats;

        public ISwarmApi Swarm => Start().Swarm;

        private void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            _ipfs?.Dispose();
            _ipfs = null;
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
