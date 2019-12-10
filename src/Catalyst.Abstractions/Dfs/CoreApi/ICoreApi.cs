using Catalyst.Abstractions.Keystore;

namespace Catalyst.Abstractions.Dfs.CoreApi
{
    /// <summary>
    ///   The IPFS Core API.
    /// </summary>
    /// <remarks>
    ///   The Core API defines a set of interfaces to manage IPFS.
    /// </remarks>
    /// <seealso href="https://github.com/ipfs/interface-ipfs-core"/>
    public interface ICoreApi
    {
        /// <summary>
        ///   Provides access to the Bitswap API.
        /// </summary>
        /// <value>
        ///   An object that implements <see cref="IBitSwapApi"/>.
        /// </value>
        IBitSwapApi BitSwapApi { get; }

        /// <summary>
        ///   Provides access to the Block API.
        /// </summary>
        /// <value>
        ///   An object that implements <see cref="IBlockApi"/>.
        /// </value>
        IBlockApi BlockApi { get; }

        /// <summary>
        ///   Provides access to the Block Repository API.
        /// </summary>
        /// <value>
        ///   An object that implements <see cref="IBlockRepositoryApi"/>.
        /// </value>
        IBlockRepositoryApi BlockRepositoryApi { get; }

        /// <summary>
        ///   Provides access to the Bootstrap API.
        /// </summary>
        /// <value>
        ///   An object that implements <see cref="IBootstrapApi"/>.
        /// </value>
        IBootstrapApi BootstrapApi { get; }

        /// <summary>
        ///   Provides access to the Config API.
        /// </summary>
        /// <value>
        ///   An object that implements <see cref="IConfigApi"/>.
        /// </value>
        IConfigApi ConfigApi { get; }

        /// <summary>
        ///   Provides access to the Dag API.
        /// </summary>
        /// <value>
        ///   An object that implements <see cref="IDagApi"/>.
        /// </value>
        IDagApi DagApi { get; }

        /// <summary>
        ///   Provides access to the DHT API.
        /// </summary>
        /// <value>
        ///   An object that implements <see cref="IDhtApi"/>.
        /// </value>
        IDhtApi DhtApi { get; }

        /// <summary>
        ///   Provides access to the DNS API.
        /// </summary>
        /// <value>
        ///   An object that implements <see cref="IDnsApi"/>.
        /// </value>
        IDnsApi DnsApi { get; }

        /// <summary>
        ///   Provides access to the File System API.
        /// </summary>
        /// <value>
        ///   An object that implements <see cref="IUnixFsApi"/>.
        /// </value>
        IUnixFsApi UnixFsApi { get; }

        /// <summary>
        ///   Provides access to the Key API.
        /// </summary>
        /// <value>
        ///   An object that implements <see cref="Catalyst.Abstractions.Keystore.IKeyApi"/>.
        /// </value>
        IKeyApi KeyApi { get; }

        /// <summary>
        ///   Provides access to the Name API.
        /// </summary>
        /// <value>
        ///   An object that implements <see cref="INameApi"/>.
        /// </value>
        INameApi NameApi { get; }

        /// <summary>
        ///   Provides access to the Object API.
        /// </summary>
        /// <value>
        ///   An object that implements <see cref="IObjectApi"/>.
        /// </value>
        IObjectApi ObjectApi { get; }

        /// <summary>
        ///   Provides access to the Pin API.
        /// </summary>
        /// <value>
        ///   An object that implements <see cref="IPinApi"/>.
        /// </value>
        IPinApi PinApi { get; }

        /// <summary>
        ///   Provides access to the PubSub API.
        /// </summary>
        /// <value>
        ///   An object that implements <see cref="IPubSubApi"/>.
        /// </value>
        IPubSubApi PubSubApi { get; }

        /// <summary>
        ///   Provides access to the Stats (statistics) API.
        /// </summary>
        /// <value>
        ///   An object that implements <see cref="IStatsApi"/>.
        /// </value>
        IStatsApi StatsApi { get; }

        /// <summary>
        ///   Provides access to the Swarm API.
        /// </summary>
        /// <value>
        ///   An object that implements <see cref="ISwarmApi"/>.
        /// </value>
        ISwarmApi SwarmApi { get; }
    }
}
