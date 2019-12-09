using Catalyst.Abstractions.Dfs;
using Lib.P2P;

namespace Catalyst.Core.Modules.Dfs.UnixFileSystem
{
    /// <summary>
    ///   A link to another <see cref="UnixFsNode"/> in the IPFS Unix File System.
    /// </summary>
    public class UnixFsLink : IFileSystemLink
    {
        /// <summary>
        ///  An empty set of links.
        /// </summary>
        public static readonly UnixFsLink[] None = new UnixFsLink[0];

        /// <inheritdoc />
        public string Name { get; set; }

        /// <inheritdoc />
        public Cid Id { get; set; }

        /// <inheritdoc />
        public long Size { get; set; }
    }
}
