using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs.CoreApi;
using Lib.P2P;
using Makaretu.Dns;

namespace Catalyst.Core.Modules.Dfs.CoreApi
{
    internal sealed class DnsApi : IDnsApi
    {
        private readonly IDnsClient _dnsClient;

        public DnsApi(DotClient dnsClient)
        {
            _dnsClient = dnsClient;
        }

        public async Task<string> ResolveNameAsync(string name,
            bool recursive = false,
            bool nocache = false,
            CancellationToken cancel = default)
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
                    name = await ResolveAsync(parts[0], recursive, cancel).ConfigureAwait(false);
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

        public async Task<string> ResolveAsync(string name, bool recursive = false, CancellationToken cancel = default(CancellationToken))
        {
            // Find the TXT dnslink in either <name> or _dnslink.<name>.
            string link = null;
            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancel))
            {
                try
                {
                    var attempts = new Task<string>[]
                    {
                        FindAsync(name, cts.Token),
                        FindAsync("_dnslink." + name, cts.Token)
                    };
                    link = await TaskHelper.WhenAnyResultAsync(attempts, cancel).ConfigureAwait(false);
                    cts.Cancel();
                }
                catch (Exception e)
                {
                    throw new NotSupportedException($"Cannot resolve '{name}'.", e);
                }
            }

            if (!recursive || link.StartsWith("/ipfs/"))
                return link;

            if (link.StartsWith("/ipns/"))
            {
                return await ResolveNameAsync(link, recursive, false, cancel).ConfigureAwait(false);
            }

            throw new NotSupportedException($"Cannot resolve '{link}'.");
        }

        //public async Task<string> ResolveAsync(string name,
        //    bool recursive = false,
        //    CancellationToken cancel = default(CancellationToken))
        //{
        //    // Find the TXT dnslink in either <name> or _dnslink.<name>.
        //    string link = null;
        //    using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancel))
        //    {
        //        try
        //        {
        //            var attempts = new Task<string>[]
        //            {
        //                FindAsync(name, cts.Token),
        //                FindAsync("_dnslink." + name, cts.Token)
        //            };
        //            link = await TaskHelper.WhenAnyResultAsync(attempts, cancel).ConfigureAwait(false);
        //            cts.Cancel();
        //        }
        //        catch (Exception e)
        //        {
        //            throw new NotSupportedException($"Cannot resolve '{name}'.", e);
        //        }
        //    }

        //    if (!recursive || link.StartsWith("/ipfs/"))
        //    {
        //        return link;
        //    }

        //    // if (link.StartsWith("/ipns/"))
        //    // {
        //    //     return await _nameApi.ResolveAsync(link, recursive, false, cancel).ConfigureAwait(false);
        //    // }

        //    throw new NotSupportedException($"Cannot resolve '{link}'.");
        //}

        async Task<string> FindAsync(string name, CancellationToken cancel)
        {
            var response = await _dnsClient.QueryAsync(name, DnsType.TXT, cancel).ConfigureAwait(false);
            var link = response.Answers.OfType<TXTRecord>().SelectMany(txt => txt.Strings)
               .Where(s => s.StartsWith("dnslink=")).Select(s => s.Substring(8)).FirstOrDefault();

            if (link == null)
            {
                throw new Exception($"'{name}' is missing a TXT record with a dnslink.");
            }

            return link;
        }

        private static bool IsDomainName(string name) { return name.IndexOf('.') > 0; }
    }
}