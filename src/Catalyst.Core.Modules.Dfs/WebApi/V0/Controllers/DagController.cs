#region LICENSE

/**
* Copyright (c) 2024 Catalyst Network
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
using System.IO;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs;
using Catalyst.Core.Modules.Dfs.WebApi.V0.Dto;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MultiFormats;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Catalyst.Core.Modules.Dfs.WebApi.V0.Controllers
{
    /// <summary>
    ///   Manages the IPLD (linked data) Directed Acrylic Graph.
    /// </summary>
    public class DagController : DfsController
    {
        /// <summary>
        ///   Creates a new controller.
        /// </summary>
        public DagController(IDfsService dfs) : base(dfs) { }

        /// <summary>
        ///  Resolve a reference (NYI).
        /// </summary>
        [HttpGet, HttpPost, Route("dag/resolve")] // TODO
        public Task Resolve(string arg)
        {
            throw new NotImplementedException("Resolving a dag reference is not implemented.");
        }

        /// <summary>
        ///   Gets the content of some linked data.
        /// </summary>
        /// <param name="arg">
        ///   A path, such as "cid", "/ipfs/cid/" or "cid/a".
        /// </param>
        [HttpGet, HttpPost, Route("dag/get")]
        public async Task<JToken> Get(string arg) { return await DfsService.DagApi.GetAsync(arg, Cancel); }

        /// <summary>
        ///   Add some linked data.
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
        /// <param name="hash">
        ///   The hashing algorithm.
        /// </param>
        /// <param name="pin">
        ///   Pin the linked data.
        /// </param>
        [HttpPost("dag/put")]
        public async Task<LinkedDataCidDto> Put(IFormFile file,
            string format = "dag-cbor",
            string hash = MultiHash.DefaultAlgorithmName,
            bool pin = true,
            [ModelBinder(Name = "cid-base")] string cidBase = MultiBase.DefaultAlgorithmName)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            await using (var stream = file.OpenReadStream())
            {
                using (var sr = new StreamReader(stream))
                {
                    using (var tr = new JsonTextReader(sr))
                    {
                        var serializer = new JsonSerializer();
                        JObject json = (JObject) serializer.Deserialize(tr);

                        var cid = await DfsService.DagApi.PutAsync(
                            json,
                            contentType: format,
                            multiHash: hash,
                            encoding: cidBase,
                            pin: false,
                            cancel: Cancel);
                        return new LinkedDataCidDto {Cid = new LinkedDataDto {Link = cid}};
                    }
                }
            }
        }
    }
}
