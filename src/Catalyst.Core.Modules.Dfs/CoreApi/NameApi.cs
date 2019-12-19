using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.Dfs.CoreApi;
using Lib.P2P;

namespace Catalyst.Core.Modules.Dfs.CoreApi
{
    internal sealed class NameApi : INameApi
    {
        private readonly IDnsApi _dnsApi;
        private readonly IObjectApi _objectApi;

        public NameApi(IDnsApi dnsApi, IObjectApi objectApi)
        {
            _dnsApi = dnsApi;
            _objectApi = objectApi;
        }

        public Task<NamedContent> PublishAsync(string path,
            bool resolve = true,
            string key = "self",
            TimeSpan? lifetime = null,
            CancellationToken cancel = default)
        {
            throw new NotImplementedException();
        }

        public Task<NamedContent> PublishAsync(Cid id,
            string key = "self",
            TimeSpan? lifetime = null,
            CancellationToken cancel = default)
        {
            throw new NotImplementedException();
        }

        public async Task<string> ResolveAsync(string name,
            bool recursive = false,
            bool nocache = false,
            CancellationToken cancel = default)
        {
            var path = name;

            //if (path.StartsWith("/ipns/"))
            //{
            //    path = await ipfs.Name.ResolveAsync(path, recursive, false, cancel).ConfigureAwait(false);
            //    if (!recursive)
            //        return path;
            //}

            //if (path.StartsWith("/ipfs/"))
            //{
            //    path = path.Remove(0, 6);
            //}

            var parts = path.Split('/').Where(p => p.Length > 0).ToArray();
            if (parts.Length == 0)
                throw new ArgumentException($"Cannot resolve '{name}'.");

            //do
            //{
            //if (name.StartsWith("/ipns/"))
            //{
            //    name = name.Substring(6);
            //}

            //var parts = name.Split('/').Where(p => p.Length > 0).ToArray();
            //if (parts.Length == 0)
            //{
            //    throw new ArgumentException($"Cannot resolve '{name}'.");
            //}

            //if (IsDomainName(parts[0]))
            //{
            //    name = await _dnsApi.ResolveAsync(parts[0], recursive, cancel).ConfigureAwait(false);
            //}
            //else
            //{
            var id = Cid.Decode(parts[0]);
            foreach (var child in parts.Skip(1))
            {
                var container = await _objectApi.GetAsync(id, cancel).ConfigureAwait(false);
                var link = container.Links.FirstOrDefault(l => l.Name == child);
                if (link == null)
                {
                    throw new ArgumentException($"Cannot resolve '{child}' in '{name}'.");
                }

                id = link.Id;
            }

            return "/ipfs/" + id.Encode();

            //throw new NotImplementedException("Resolving IPNS is not yet implemented.");

            //if (parts.Length > 1)
            //{
            //    name = name + "/" + string.Join("/", parts, 1, parts.Length - 1);
            //}

            //} while (recursive && !name.StartsWith("/ipfs/"));

            //return name;
        }

        /// <summary>
        ///     Determines if the supplied string is a valid domain name.
        /// </summary>
        /// <param name="name">
        ///     An domain name, such as "ipfs.io".
        /// </param>
        /// <returns>
        ///     <b>true</b> if <paramref name="name" /> is a domain name;
        ///     otherwise, <b>false</b>.
        /// </returns>
        /// <remarks>
        ///     A domain must contain at least one '.'.
        /// </remarks>
        private static bool IsDomainName(string name) { return name.IndexOf('.') > 0; }
    }
}
