#region LICENSE

/**
* Copyright (c) 2019 Catalyst Network
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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs;
using Catalyst.Core.Modules.Dfs.WebApi.V0.Dto;
using Lib.P2P;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MultiFormats;

namespace Catalyst.Core.Modules.Dfs.WebApi.V0.Controllers
{
    /// <summary>
    ///   Manages IPFS blocks.
    /// </summary>
    /// <remarks>
    ///   An IPFS Block is a byte sequence that represents an IPFS Object 
    ///   (i.e. serialized byte buffers). It is useful to talk about them as "blocks" in Bitswap 
    ///   and other things that do not care about what is being stored. 
    ///   <para>
    ///   It is also possible to store arbitrary stuff using ipfs block put/get as the API 
    ///   does not check for proper IPFS Object formatting.
    ///   </para>
    ///   <note>
    ///   This may be very good or bad, we haven't decided yet 😄
    ///   </note>
    /// </remarks>
    public sealed class BlockController : DfsController
    {
        /// <summary>
        ///   Creates a new controller.
        /// </summary>
        public BlockController(IDfsService dfs) : base(dfs) { }

        /// <summary>
        ///   Get the data of a block.
        /// </summary>
        /// <param name="arg">
        ///   The CID of the block.
        /// </param>
        [HttpGet, HttpPost, Route("block/get")]
        [Produces("application/octet-stream")]
        public async Task<IActionResult> Get(string arg)
        {
            var block = await DfsService.BlockApi.GetAsync(arg, Cancel);
            Immutable();
            return File(block.DataStream, "application/octet-stream", arg, null, ETag(block.Id));
        }

        /// <summary>
        ///   Get the stats of a block.
        /// </summary>
        /// <param name="arg">
        ///   The CID of the block.
        /// </param>
        [HttpGet, HttpPost, Route("block/stat")]
        public async Task<BlockStatsDto> Stats(string arg)
        {
            var info = await DfsService.BlockApi.StatAsync(arg, Cancel);
            if (info == null)
            {
                throw new KeyNotFoundException($"Block '{arg}' does not exist.");
            }

            Immutable();
            return new BlockStatsDto
            {
                Key = info.Id, Size = info.Size
            };
        }

        /// <summary>
        ///   Add a block to the local store.
        /// </summary>
        /// <param name="file">
        ///   multipart/form-data.
        /// </param>
        /// <param name="cidBase">
        ///   The base encoding algorithm.
        /// </param>
        /// <param name="format">
        ///   The content type.
        /// </param>
        /// <param name="mhtype">
        ///   The hashing algorithm.
        /// </param>
        [HttpPost("block/put")]
        public async Task<KeyDto> Put(IFormFile file,
            string format = Cid.DefaultContentType,
            string mhtype = MultiHash.DefaultAlgorithmName,
            [ModelBinder(Name = "cid-base")] string cidBase = MultiBase.DefaultAlgorithmName)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            await using (var data = file.OpenReadStream())
            {
                var cid = await DfsService.BlockApi.PutAsync(
                    data,
                    format,
                    encoding: cidBase,
                    pin: false,
                    cancel: Cancel);
                return new KeyDto {Key = cid};
            }
        }

        /// <summary>
        ///   Remove a block from the local store.
        /// </summary>
        /// <param name="arg">
        ///   The CID of the block.
        /// </param>
        /// <param name="force">
        ///   If true, do not return an error when the block does
        ///   not exist.
        /// </param>
        [HttpGet, HttpPost, Route("block/rm")]
        public async Task<HashDto> Remove(string arg,
            bool force = false)
        {
            var cid = await DfsService.BlockApi.RemoveAsync(arg, true, Cancel);
            var dto = new HashDto();
            if (cid == null && !force)
            {
                dto.Hash = arg;
                dto.Error = "block not found";
            }
            else if (cid == null)
            {
                return null;
            }
            else
            {
                dto.Hash = cid;
            }

            return dto;
        }
    }
}
