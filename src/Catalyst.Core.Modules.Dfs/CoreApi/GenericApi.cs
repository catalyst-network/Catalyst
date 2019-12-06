using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.Dfs.CoreApi;
using Lib.P2P;
using Lib.P2P.Protocols;
using MultiFormats;

namespace Catalyst.Core.Modules.Dfs.CoreApi
{
    class GenericApi : IGenericApi
    {
        IDfs ipfs;

        public GenericApi(IDfs ipfs) { this.ipfs = ipfs; }

        public async Task<Peer> IdAsync(MultiHash peer = null, CancellationToken cancel = default(CancellationToken))
        {
            if (peer == null)
            {
                return await ipfs.LocalPeer.ConfigureAwait(false);
            }

            return await ipfs.Dht.FindPeerAsync(peer, cancel).ConfigureAwait(false);
        }

        public async Task<IEnumerable<PingResult>> PingAsync(MultiHash peer,
            int count = 10,
            CancellationToken cancel = default(CancellationToken))
        {
            var ping = await ipfs.PingService;
            return await ping.PingAsync(peer, count, cancel);
        }

        public async Task<IEnumerable<PingResult>> PingAsync(MultiAddress address,
            int count = 10,
            CancellationToken cancel = default(CancellationToken))
        {
            var ping = await ipfs.PingService;
            return await ping.PingAsync(address, count, cancel);
        }

        public async Task<string> ResolveAsync(string name,
            bool recursive = true,
            CancellationToken cancel = default(CancellationToken))
        {
            var path = name;
            if (path.StartsWith("/ipns/"))
            {
                path = await ipfs.Name.ResolveAsync(path, recursive, false, cancel).ConfigureAwait(false);
                if (!recursive)
                    return path;
            }

            if (path.StartsWith("/ipfs/"))
            {
                path = path.Remove(0, 6);
            }

            var parts = path.Split('/').Where(p => p.Length > 0).ToArray();
            if (parts.Length == 0)
                throw new ArgumentException($"Cannot resolve '{name}'.");

            var id = Cid.Decode(parts[0]);
            foreach (var child in parts.Skip(1))
            {
                var container = await ipfs.Object.GetAsync(id, cancel).ConfigureAwait(false);
                var link = container.Links.FirstOrDefault(l => l.Name == child);
                if (link == null)
                    throw new ArgumentException($"Cannot resolve '{child}' in '{name}'.");
                id = link.Id;
            }

            return "/ipfs/" + id.Encode();
        }

        public Task ShutdownAsync() { return ipfs.StopAsync(); }

        public async Task<Dictionary<string, string>> VersionAsync(CancellationToken cancel =
            default(CancellationToken))
        {
            var version = typeof(GenericApi).GetTypeInfo().Assembly.GetName().Version;
            return new Dictionary<string, string>
            {
                {
                    "Version", $"{version.Major}.{version.Minor}.{version.Revision}"
                },
                {
                    "Repo", await ipfs.BlockRepository.VersionAsync()
                }
            };
        }
    }
}
