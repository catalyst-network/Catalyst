using System;
using System.IO;
using Catalyst.Abstractions.FileSystem;

namespace Catalyst.Abstractions.Options
{
    /// <summary>
    ///   Configuration options for the repository.
    /// </summary>
    /// <seealso cref="DfsOptions"/>
    public class RepositoryOptions
    {
        /// <summary>
        ///   Creates a new instance of the <see cref="RepositoryOptions"/> class
        ///   with the default values.
        /// </summary>
        public RepositoryOptions(IFileSystem fileSystem = default, string dfsDirectory = default)
        {
            var path = Environment.GetEnvironmentVariable("IPFS_PATH");
            if (path != null)
            {
                Folder = path;
            }
            else
            {
                Folder = Path.Combine(
                    Environment.GetEnvironmentVariable("HOME") ??
                    Environment.GetEnvironmentVariable("HOMEPATH"),
                    ".catalyst");
            }

            if (fileSystem != default && dfsDirectory != default)
            {
                Folder = new DirectoryInfo(Path.Combine(fileSystem.GetCatalystDataDir().FullName, dfsDirectory))
                   .FullName;
                Directory.CreateDirectory(Folder);
            }
        }

        /// <summary>
        ///   The directory of the repository.
        /// </summary>
        /// <value>
        ///   The default value is <c>$IPFS_PATH</c> or <c>$HOME/.csipfs</c> or
        ///   <c>$HOMEPATH/.csipfs</c>.
        /// </value>
        public string Folder { get; set; }

        /// <summary>
        ///   Get the existing directory of the repository.
        /// </summary>
        /// <returns>
        ///   An existing directory.
        /// </returns>
        /// <remarks>
        ///   Creates the <see cref="Folder"/> if it does not exist.
        /// </remarks>
        public string ExistingFolder()
        {
            var path = Folder;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return path;
        }
    }
}
