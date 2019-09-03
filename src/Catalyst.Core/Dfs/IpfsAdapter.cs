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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.FileSystem;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Config;
using Common.Logging;
using Common.Logging.Serilog;
using Ipfs;
using Ipfs.CoreApi;
using Ipfs.Engine;
using PeerTalk.Cryptography;
using Serilog;

namespace Catalyst.Core.Dfs
{
    /// <summary>
    ///   Modifies the IPFS behaviour to meet the Catalyst requirements.
    /// </summary>
    /// <remarks>
    ///   The IPFS engine is lazy, it is only started when needed.
    /// </remarks>
    public sealed class IpfsAdapter : IIpfsAdapter
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
            LogManager.Adapter = new SerilogFactoryAdapter(Log.Logger);
        }

        public IpfsAdapter(IPasswordManager passwordReader,
            IFileSystem fileSystem,
            ILogger logger,
            string swarmKey = "07a8e9d0c43400927ab274b7fa443596b71e609bacae47bd958e5cd9f59d6ca3",
            IEnumerable<MultiAddress> seedServers = null)
        {
            if (seedServers == null || seedServers.Count() == 0)
            {
                seedServers = new[]
                {
                    new MultiAddress("/ip4/165.22.209.154/tcp/4001/ipfs/18n3naE9kBZoVvgYMV6saMZdzVq4jUMZsJKddJPrwWjkcwtf23ZcGFW2xUCmSE29ABRs"),
                    new MultiAddress("/ip4/165.22.226.50/tcp/4001/ipfs/18n3naE9kBZoVvgYMV6saMZe2jBdLcqbqE6qLfApXJLPr855vycKygWXnRsMVXuW8o1E"),
                    new MultiAddress("/ip4/167.71.129.154/tcp/4001/ipfs/18n3naE9kBZoVvgYMV6saMZe79QPeKgXsvTvVTPV732TnSya3MqQ5YUMcGHZFxW1VAEC"),
                    new MultiAddress("/ip4/167.71.79.54/tcp/4001/ipfs/18n3naE9kBZoVvgYMV6saMZe4BowwZZyjRazPAMd5VhWBfvM88Qcyy3viWGQ8fzEhcvc"),
                    new MultiAddress("/ip4/167.71.79.77/tcp/4001/ipfs/18n3naE9kBZoVvgYMV6saMZe64FqVyvgWoLYBktgShFe6oEYeDWox8o2WhA8brQu5dBA"),
                    new MultiAddress("/ip4/165.22.175.71/tcp/4001/ipfs/18n3naE9kBZoVvgYMV6saMZdwHHKV44GJfKE9ecyRXHcXY4yNtEExgPSvvwsDtvZspZj"),
                    new MultiAddress("/ip4/165.22.234.49/tcp/4001/ipfs/18n3naE9kBZoVvgYMV6saMZdxSv6GpHsHAVYb51xvH3y4xvGVztM1h1GEsUjEzqc8Wh4"),
                    new MultiAddress("/ip4/167.71.134.31/tcp/4001/ipfs/18n3naE9kBZoVvgYMV6saMZe4SkF2tXtZbBMUSsmbosM5SAJaU3oxUAbReFDBaKeHqR3")
                };
            }
            
            _logger = logger;

            // The password is used to access the private keys.
            var password = passwordReader.RetrieveOrPromptAndAddPasswordToRegistry(PasswordRegistryTypes.IpfsPassword, "Please provide your IPFS password");
            _ipfs = new IpfsEngine(password);
            _ipfs.Options.KeyChain.DefaultKeyType = Constants.KeyChainDefaultKeyType;

            // The IPFS repository is inside the catalyst home folder.
            _ipfs.Options.Repository.Folder = Path.Combine(
                fileSystem.GetCatalystDataDir().FullName,
                Constants.DfsDataSubDir);

            // The seed nodes for the catalyst network.
            _ipfs.Options.Discovery.BootstrapPeers = seedServers;

            // Do not use the public IPFS network, use a private network
            // of catalyst only nodes.
            _ipfs.Options.Swarm.PrivateNetworkKey = new PreSharedKey
            {
                Value = swarmKey.ToHexBuffer()
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
            if (_isStarted)
            {
                return _ipfs;
            }
            
            lock (_startingLock)
            {
                if (_isStarted)
                {
                    return _ipfs;
                }
                
                _ipfs.Start();
                _isStarted = true;
                _logger.Information("IPFS started.");
            }

            return _ipfs;
        }

        public IBitswapApi Bitswap => Start().Bitswap;

        public IBlockApi Block => Start().Block;

        public IBlockRepositoryApi BlockRepository => Start().BlockRepository;

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
