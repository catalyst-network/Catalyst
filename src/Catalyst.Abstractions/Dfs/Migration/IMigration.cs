using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Options;

namespace Catalyst.Abstractions.Dfs.Migration
{
    /// <summary>
    ///   Provides a migration path to the repository.
    /// </summary>
    public interface IMigration
    {
        /// <summary>
        ///   The repository version that is created.
        /// </summary>
        int Version { get; }

        /// <summary>
        ///   Indicates that an upgrade can be performed.
        /// </summary>
        bool CanUpgrade { get; }

        /// <summary>
        ///   Indicates that an downgrade can be performed.
        /// </summary>
        bool CanDowngrade { get; }

        /// <summary>
        ///   Upgrade the repository.
        /// </summary>
        /// <param name="ipfs">
        ///   The IPFS system to upgrade.
        /// </param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        Task UpgradeAsync(RepositoryOptions options, CancellationToken cancel = default(CancellationToken));

        /// <summary>
        ///   Downgrade the repository.
        /// </summary>
        /// <param name="ipfs">
        ///   The IPFS system to downgrade.
        /// </param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        Task DowngradeAsync(RepositoryOptions options, CancellationToken cancel = default(CancellationToken));
    }
}
