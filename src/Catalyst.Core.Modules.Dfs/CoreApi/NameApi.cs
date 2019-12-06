using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.Dfs.CoreApi;
using Lib.P2P;

namespace Catalyst.Core.Modules.Dfs.CoreApi
{
    class NameApi : INameApi
    {
        IDfs ipfs;

        public NameApi(IDfs ipfs) { this.ipfs = ipfs; }

        public Task<NamedContent> PublishAsync(string path,
            bool resolve = true,
            string key = "self",
            TimeSpan? lifetime = null,
            CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<NamedContent> PublishAsync(Cid id,
            string key = "self",
            TimeSpan? lifetime = null,
            CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public async Task<string> ResolveAsync(string name,
            bool recursive = false,
            bool nocache = false,
            CancellationToken cancel = default(CancellationToken))
        {
            do
            {
                if (name.StartsWith("/ipns/"))
                {
                    name = name.Substring(6);
                }

                var parts = name.Split('/').Where(p => p.Length > 0).ToArray();
                if (parts.Length == 0)
                    throw new ArgumentException($"Cannot resolve '{name}'.");
                if (IsDomainName(parts[0]))
                {
                    name = await ipfs.Dns.ResolveAsync(parts[0], recursive, cancel).ConfigureAwait(false);
                }
                else
                {
                    throw new NotImplementedException("Resolving IPNS is not yet implemented.");
                }

                if (parts.Length > 1)
                {
                    name = name + "/" + string.Join("/", parts, 1, parts.Length - 1);
                }
            } while (recursive && !name.StartsWith("/ipfs/"));

            return name;
        }

        /// <summary>
        ///   Determines if the supplied string is a valid domain name.
        /// </summary>
        /// <param name="name">
        ///   An domain name, such as "ipfs.io".
        /// </param>
        /// <returns>
        ///   <b>true</b> if <paramref name="name"/> is a domain name;
        ///   otherwise, <b>false</b>.
        /// </returns>
        /// <remarks>
        ///    A domain must contain at least one '.'.
        /// </remarks>
        public static bool IsDomainName(string name) { return name.IndexOf('.') > 0; }
    }
}
