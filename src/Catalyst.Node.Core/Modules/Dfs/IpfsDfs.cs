using System;
using System.Threading.Tasks;
using Catalyst.Node.Common.Modules.Dfs;

namespace Catalyst.Node.Core.Modules.Dfs
{
    public class IpfsDfs : IDisposable, IDfs
    {
        private readonly object _ipfs;

        /// <summary>
        /// </summary>
        /// <param name="ipfs"></param>
        /// <param name="settings"></param>
        public IpfsDfs()
        {
        }

        public void Start()
        {
        }

        /// <summary>
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public string AddFile(string filename) { return null; }

        /// <summary>
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public Task<string> ReadAllTextAsync(string filename) { return Task.FromResult(null as string); }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {

            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}