using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.Dfs.CoreApi;
using Catalyst.Core.Modules.Dfs.Controllers.V0;
using Catalyst.Core.Modules.Dfs.WebApi.V0.Dto;
using Microsoft.AspNetCore.Mvc;

namespace Catalyst.Core.Modules.Dfs.WebApi.V0.Controllers
{
    /// <summary>
    ///   DNS mapping to IPFS.
    /// </summary>
    /// <remarks>
    ///   Multihashes are hard to remember, but domain names are usually easy to
    ///   remember. To create memorable aliases for multihashes, DNS TXT
    ///   records can point to other DNS links, IPFS objects, IPNS keys, etc.
    /// </remarks>
    public class DnsController : IpfsController
    {
        /// <summary>
        ///   Creates a new controller.
        /// </summary>
        public DnsController(IDfsService dfs) : base(dfs) { }

        /// <summary>
        ///   Resolve a domain name to an IPFS path.
        /// </summary>
        /// <param name="arg">
        ///   A domain name, such as "ipfs.io".
        /// </param>
        /// <param name="recursive">
        ///   Resolve until the result is not a DNS link. Defaults to <b>false</b>.
        /// </param>
        /// <returns>
        ///   The resolved IPFS path, such as 
        ///   <c>/ipfs/QmYNQJoKGNHTpPxCBPh9KkDpaExgd2duMa3aF6ytMpHdao</c>.
        /// </returns>
        [HttpGet, HttpPost, Route("dns")]
        public async Task<PathDto> Get(string arg, bool recursive = false)
        {
            var path = await IpfsCore.DnsApi.ResolveAsync(arg, recursive, Cancel);
            return new PathDto(path);
        }
    }
}
