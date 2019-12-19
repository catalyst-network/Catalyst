using Makaretu.Dns;

namespace Catalyst.Abstractions.Options
{
    /// <summary>
    ///   Configuration options for the <see cref="Catalyst.Core.Modules.Dfs.Dfs"/>.
    /// </summary>
    /// <seealso cref="Microsoft.Extensions.Options.Options"/>
    public class DfsOptions
    {
        /// <summary>
        ///   Repository options.
        /// </summary>
        public RepositoryOptions Repository { get; set; } = new RepositoryOptions();

        /// <summary>
        ///   KeyChain options.
        /// </summary>
        public KeyChainOptions KeyChain { get; set; } = new KeyChainOptions();

        /// <summary>
        ///   Provides access to the Domain Name System.
        /// </summary>
        /// <value>
        ///   Defaults to <see cref="Makaretu.Dns.DotClient"/>, DNS over TLS.
        /// </value>
        public IDnsClient Dns { get; set; } = new DotClient();

        /// <summary>
        ///   Block options.
        /// </summary>
        public BlockOptions Block { get; set; }

        /// <summary>
        ///    Discovery options.
        /// </summary>
        public DiscoveryOptions Discovery { get; set; } = new DiscoveryOptions();

        /// <summary>
        ///   Swarm (network) options.
        /// </summary>
        public SwarmOptions Swarm { get; set; } = new SwarmOptions();

        public DfsOptions(BlockOptions blockOptions) { Block = blockOptions; }
    }
}
