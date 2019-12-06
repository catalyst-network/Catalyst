using System.IO;
using Catalyst.Abstractions.Dfs;
using Newtonsoft.Json.Linq;

namespace Catalyst.Core.Modules.Dfs.Tests
{
    /// <summary>
    ///   Creates a temporary node.
    /// </summary>
    /// <remarks>
    ///   A temporary node has its own repository and listening address.
    ///   When it is disposed, the repository is deleted.
    /// </remarks>
    class TempNode : Dfs, IDfs
    {
        static int nodeNumber;

        public TempNode()
            : base()
        {
            Options.Repository.Folder = Path.Combine(Path.GetTempPath(), $"ipfs-{nodeNumber++}");
            Options.KeyChain.DefaultKeyType = "ed25519";
            Config.SetAsync(
                "Addresses.Swarm",
                JToken.FromObject(new string[] {"/ip4/0.0.0.0/tcp/0"})
            ).Wait();
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (Directory.Exists(Options.Repository.Folder))
            {
                Directory.Delete(Options.Repository.Folder, true);
            }
        }
    }
}
