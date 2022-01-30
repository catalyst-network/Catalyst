#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
*
* This file is part of Catalyst.Node <https://github.com/catalyst-network/Catalyst.Node>
*
* Catalyst.Node is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 2 of the License, or
* (at your option) any later version.
*
* Catalyst.Node is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with Catalyst.Node. If not, see <https://www.gnu.org/licenses/>.
*/

#endregion

using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.Dfs.CoreApi;
using Catalyst.Core.Modules.Dfs.WebApi.V0.Dto;
using Microsoft.AspNetCore.Mvc;

namespace Catalyst.Core.Modules.Dfs.WebApi.V0.Controllers
{
    /// <summary>
    ///    Manages all the blocks in teh repository.
    /// </summary>
    public sealed class BlockRepositoryController : DfsController
    {
        /// <summary>
        ///   Creates a new controller.
        /// </summary>
        public BlockRepositoryController(IDfsService dfs) : base(dfs) { }

        /// <summary>
        ///   Garbage collection.
        /// </summary>
        [HttpGet, HttpPost, Route("repo/gc")]
        public Task GarbageCollection() { return DfsService.BlockRepositoryApi.RemoveGarbageAsync(Cancel); }

        /// <summary>
        ///   Get repository information.
        /// </summary>
        [HttpGet, HttpPost, Route("repo/stat")]
        public Task<RepositoryData> Statistics() { return DfsService.BlockRepositoryApi.StatisticsAsync(Cancel); }

        /// <summary>
        ///   Verify that the blocks are not corrupt.
        /// </summary>
        [HttpGet, HttpPost, Route("repo/verify")]
        public Task Verify() { return DfsService.BlockRepositoryApi.VerifyAsync(Cancel); }

        /// <summary>
        ///   Get repository information.
        /// </summary>
        [HttpGet, HttpPost, Route("repo/version")]
        public async Task<VersionBlockRepositoryDto> Version()
        {
            return new VersionBlockRepositoryDto
            {
                Version = await DfsService.BlockRepositoryApi.VersionAsync(Cancel)
            };
        }
    }
}
