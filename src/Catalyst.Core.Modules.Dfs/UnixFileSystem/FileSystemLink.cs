using Catalyst.Abstractions.Dfs;
using Lib.P2P;

namespace Catalyst.Core.Modules.Dfs.UnixFileSystem
{
    /// <summary>
    ///   A link to another <see cref="FileSystemNode"/> in the IPFS Unix File System.
    /// </summary>
    public class FileSystemLink : IFileSystemLink
    {
        /// <summary>
        ///  An empty set of links.
        /// </summary>
        public static readonly FileSystemLink[] None = new FileSystemLink[0];

        /// <inheritdoc />
        public string Name { get; set; }

        /// <inheritdoc />
        public Cid Id { get; set; }

        /// <inheritdoc />
        public long Size { get; set; }
    }
}
