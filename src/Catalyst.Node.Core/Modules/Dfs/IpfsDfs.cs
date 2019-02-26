using System;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Node.Common.Modules.Dfs;

namespace Catalyst.Node.Core.Modules.Dfs
{
    public class IpfsDfs : IDisposable, IDfs
    {
        /// <inheritdoc />
        public async Task StartAsync(CancellationToken cancellationToken = default) { await Task.CompletedTask; }

        /// <inheritdoc />
        public async Task<string> AddFileAsync(string filename, CancellationToken cancellationToken = default) { return await Task.FromResult(null as string); }

        /// <inheritdoc />
        public async Task<string> ReadAllTextAsync(string filename, CancellationToken cancellationToken = default) { return await Task.FromResult(null as string); }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {

            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
        }
    }
}