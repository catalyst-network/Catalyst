using System.IO;
using Catalyst.Abstractions.FileSystem;
using Lib.P2P.Cryptography;
using Makaretu.Dns;
using MultiFormats;

namespace Catalyst.Abstractions.Options
{
    /// <summary>
    ///     Configuration options for the <see cref="Catalyst.Core.Modules.Dfs.Dfs" />.
    /// </summary>
    /// <seealso cref="Microsoft.Extensions.Options.Options" />
    public class DfsOptions
    {
        /// <summary>
        ///     Repository options.
        /// </summary>
        public RepositoryOptions Repository { get; set; }

        /// <summary>
        ///     KeyChain options.
        /// </summary>
        public KeyChainOptions KeyChain { get; set; } = new KeyChainOptions();

        /// <summary>
        ///     Provides access to the Domain Name System.
        /// </summary>
        /// <value>
        ///     Defaults to <see cref="Makaretu.Dns.DotClient" />, DNS over TLS.
        /// </value>
        public IDnsClient Dns { get; set; } = new DotClient();

        /// <summary>
        ///     Block options.
        /// </summary>
        public BlockOptions Block { get; set; }

        /// <summary>
        ///     Discovery options.
        /// </summary>
        public DiscoveryOptions Discovery { get; set; }

        /// <summary>
        ///     Swarm (network) options.
        /// </summary>
        public SwarmOptions Swarm { get; set; } = new SwarmOptions();

        public DfsOptions(BlockOptions blockOptions, DiscoveryOptions discoveryOptions, RepositoryOptions repositoryOptions)
        {
            Block = blockOptions;
            Discovery = discoveryOptions;
            Repository = repositoryOptions;

            var swarmKey = "07a8e9d0c43400927ab274b7fa443596b71e609bacae47bd958e5cd9f59d6ca3";

            var seedServers = new[]
            {
                new MultiAddress(
                    "/ip4/46.101.132.61/tcp/4001/ipfs/18n3naE9kBZoVvgYMV6saMZdtAkDHgs8MDwwhtyLu8JpYitY4Nk8jmyGgQ4Gt3VKNson"),
                new MultiAddress(
                    "/ip4/188.166.13.135/tcp/4001/ipfs/18n3naE9kBZoVvgYMV6saMZe2AAPTCoujCxhJHECaySDEsPrEz9W2u7uo6hAbJhYzhPg"),
                new MultiAddress(
                    "/ip4/167.172.73.132/tcp/4001/ipfs/18n3naE9kBZoVvgYMV6saMZe1E9wXdykR6h3Q9EaQcQc6hdNAXyCTEzoGfcA2wQgCRyg")
            };

            KeyChain.DefaultKeyType = "ed25519";

            //Constants.KeyChainDefaultKeyType;

            // The seed nodes for the catalyst network.
            //Options.Discovery.BootstrapPeers = seedServers;

            // Do not use the public IPFS network, use a private network
            // of catalyst only nodes.
            Swarm.PrivateNetworkKey = new PreSharedKey
            {
                Value = swarmKey.ToHexBuffer()
            };
        }
    }
}
