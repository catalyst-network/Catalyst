using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.Dfs.CoreApi;
using Microsoft.AspNetCore.Mvc;

namespace Catalyst.Core.Modules.Dfs.WebApi.V0.Controllers
{
    /// <summary>
    ///   A wrapped version number.
    /// </summary>
    public class VersionBlockRepositoryDto
    {
        /// <summary>
        ///   The version number.
        /// </summary>
        public string Version;
    }

    /// <summary>
    ///    Manages all the blocks in teh repository.
    /// </summary>
    public class BlockRepositoryController : IpfsController
    {
        /// <summary>
        ///   Creates a new controller.
        /// </summary>
        public BlockRepositoryController(IDfsService dfs) : base(dfs) { }

        /// <summary>
        ///   Garbage collection.
        /// </summary>
        [HttpGet, HttpPost, Route("repo/gc")]
        public Task GarbageCollection() { return IpfsCore.BlockRepositoryApi.RemoveGarbageAsync(Cancel); }

        /// <summary>
        ///   Get repository information.
        /// </summary>
        [HttpGet, HttpPost, Route("repo/stat")]
        public Task<RepositoryData> Statistics() { return IpfsCore.BlockRepositoryApi.StatisticsAsync(Cancel); }

        /// <summary>
        ///   Verify that the blocks are not corrupt.
        /// </summary>
        [HttpGet, HttpPost, Route("repo/verify")]
        public Task Verify() { return IpfsCore.BlockRepositoryApi.VerifyAsync(Cancel); }

        /// <summary>
        ///   Get repository information.
        /// </summary>
        [HttpGet, HttpPost, Route("repo/version")]
        public async Task<VersionBlockRepositoryDto> Version()
        {
            return new VersionBlockRepositoryDto
            {
                Version = await IpfsCore.BlockRepositoryApi.VersionAsync(Cancel)
            };
        }
    }
}
