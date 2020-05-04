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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.Dfs.CoreApi;
using Catalyst.Core.Lib.Dag;
using Lib.P2P;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Catalyst.Core.Modules.Dfs.WebApi.V0.Controllers
{
    /// <summary>
    ///     Stats for an object.
    /// </summary>
    public class ObjectStatDto
    {
        /// <summary>
        ///     The CID of the object.
        /// </summary>
        public string Hash { set; get; }

        /// <summary>
        ///     Number of links.
        /// </summary>
        public int NumLinks { get; set; }

        /// <summary>
        ///     Size of the links segment.
        /// </summary>
        public long LinksSize { get; set; }

        /// <summary>
        ///     Size of the raw, encoded data.
        /// </summary>
        public long BlockSize { get; set; }

        /// <summary>
        ///     Siz of the data segment.
        /// </summary>
        public long DataSize { get; set; }

        /// <summary>
        ///     Size of object and its references
        /// </summary>
        public long CumulativeSize { get; set; }
    }

    /// <summary>
    ///     A link to a file.
    /// </summary>
    public class ObjectLinkDto
    {
        /// <summary>
        ///     The object name.
        /// </summary>
        public string Name { set; get; }

        /// <summary>
        ///     The CID of the object.
        /// </summary>
        public string Hash { set; get; }

        /// <summary>
        ///     The object size.
        /// </summary>
        public long Size { set; get; }
    }

    /// <summary>
    ///     Link details on an object.
    /// </summary>
    public class ObjectLinkDetailDto
    {
        /// <summary>
        ///     The CID of the object.
        /// </summary>
        public string Hash { set; get; }

        /// <summary>
        ///     Links to other objects.
        /// </summary>
        public IEnumerable<ObjectLinkDto> Links { set; get; }
    }

    /// <summary>
    ///     Data and link details on an object.
    /// </summary>
    public class ObjectDataDetailDto : ObjectLinkDetailDto
    {
        /// <summary>
        ///     The object data encoded as UTF-8.
        /// </summary>
        public string Data { set; get; }
    }

    /// <summary>
    ///     Manages the IPFS Merkle Directed Acrylic Graph.
    /// </summary>
    /// <remarks>
    ///     <note>
    ///         This is being obsoleted by <see cref="IDagApi" />.
    ///     </note>
    /// </remarks>
    public sealed class ObjectController : DfsController
    {
        /// <summary>
        ///     Creates a new controller.
        /// </summary>
        public ObjectController(ICoreApi dfs) : base(dfs) { }

        /// <summary>
        ///     Create an object from a template.
        /// </summary>
        /// <param name="arg">
        ///     Template name. Must be "unixfs-dir".
        /// </param>
        [HttpGet] [HttpPost] [Route("object/new")]
        private async Task<ObjectLinkDetailDto> Create(string arg)
        {
            var node = await DfsService.ObjectApi.NewAsync(arg, Cancel);
            Immutable();
            return new ObjectLinkDetailDto
            {
                Hash = node.Id,
                Links = node.Links.Select(link => new ObjectLinkDto
                {
                    Hash = link.Id,
                    Name = link.Name,
                    Size = link.Size
                })
            };
        }

        /// <summary>
        ///     Store a MerkleDAG node.
        /// </summary>
        /// <param name="file">
        ///     multipart/form-data.
        /// </param>
        /// <param name="inputenc">
        ///     "protobuf" or "json"
        /// </param>
        /// <param name="datafieldenc">
        ///     "text" or "base64"
        /// </param>
        /// <param name="pin">
        ///     Pin the object.
        /// </param>
        /// <returns></returns>
        [HttpPost("object/put")]
        private async Task<ObjectLinkDetailDto> Put(IFormFile file,
            string inputenc = "json",
            string datafieldenc = "text",
            bool pin = false)
        {
            if (datafieldenc != "text") // TODO
            {
                throw new NotImplementedException("Only datafieldenc = `text` is allowed.");
            }

            IDagNode node;
            switch (inputenc)
            {
                case "protobuf":
                    await using (var stream = file.OpenReadStream())
                    {
                        var dag = new DagNode(stream);
                        node = await DfsService.ObjectApi.PutAsync(dag, Cancel);
                    }

                    break;

                case "json": // TODO
                default:
                    throw new ArgumentException("inputenc", $"Input encoding '{inputenc}' is not supported.");
            }

            if (pin)
            {
                await DfsService.PinApi.AddAsync(node.Id, false, Cancel);
            }

            return new ObjectLinkDetailDto
            {
                Hash = node.Id,
                Links = node.Links.Select(link => new ObjectLinkDto
                {
                    Hash = link.Id,
                    Name = link.Name,
                    Size = link.Size
                })
            };
        }

        /// <summary>
        ///     Get the data and links of an object.
        /// </summary>
        /// <param name="arg">
        ///     The object's CID.
        /// </param>
        /// <param name="dataEncoding">
        ///     The encoding of the object's data; "text" (default) or "base64".
        /// </param>
        [
            HttpGet] [HttpPost] [Route("object/get")]
        private async Task<ObjectDataDetailDto> Get(string arg,
            [ModelBinder(Name = "data-encoding")] string dataEncoding)
        {
            var node = await DfsService.ObjectApi.GetAsync(arg, Cancel);
            Immutable();
            var dto = new ObjectDataDetailDto
            {
                Hash = arg,
                Links = node.Links.Select(
                    link => new ObjectLinkDto {Hash = link.Id, Name = link.Name, Size = link.Size}
                ),
                Data = dataEncoding switch
                {
                    "base64" => Convert.ToBase64String(node.DataBytes),
                    "text" => Encoding.UTF8.GetString(node.DataBytes),
                    _ => Encoding.UTF8.GetString(node.DataBytes)
                }
            };

            return dto;
        }

        /// <summary>
        ///     Get the links of an object.
        /// </summary>
        /// <param name="arg">
        ///     The object's CID.
        /// </param>
        [
            HttpGet] [HttpPost] [Route("object/links")]
        private async Task<ObjectLinkDetailDto> Links(string arg)
        {
            var links = await DfsService.ObjectApi.LinksAsync(arg, Cancel);
            Immutable();
            return new ObjectLinkDetailDto
            {
                Hash = arg,
                Links = links.Select(link => new ObjectLinkDto
                {
                    Hash = link.Id,
                    Name = link.Name,
                    Size = link.Size
                })
            };
        }

        /// <summary>
        ///     Get the object's data.
        /// </summary>
        /// <param name="arg">
        ///     The object's CID or a path.
        /// </param>
        [
            HttpGet] [HttpPost] [Route("object/data")]
        [Produces("text/plain")]
        private async Task<IActionResult> Data(string arg)
        {
            var r = await DfsService.NameApi.ResolveAsync(arg, true, false, Cancel);
            var cid = Cid.Decode(r.Remove(0, 6)); // strip '/ipfs/'.
            var stream = await DfsService.ObjectApi.DataAsync(cid, Cancel);

            return File(stream, "text/plain");
        }

        /// <summary>
        ///     Get the stats of an object.
        /// </summary>
        /// <param name="arg">
        ///     The object's CID.
        /// </param>
        [
            HttpGet] [HttpPost] [Route("object/stat")]
        private async Task<ObjectStatDto> Stat(string arg)
        {
            var info = await DfsService.ObjectApi.StatAsync(arg, Cancel);
            Immutable();
            return new ObjectStatDto
            {
                Hash = arg,
                BlockSize = info.BlockSize,
                CumulativeSize = info.CumulativeSize,
                DataSize = info.DataSize,
                LinksSize = info.LinkSize,
                NumLinks = info.LinkCount
            };
        }
    }
}
