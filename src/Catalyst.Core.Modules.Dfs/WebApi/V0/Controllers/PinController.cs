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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs;
using Microsoft.AspNetCore.Mvc;

namespace Catalyst.Core.Modules.Dfs.WebApi.V0.Controllers
{
    /// <summary>
    ///     A list of pins.
    /// </summary>
    public class PinsDto
    {
        /// <summary>
        ///     The CIDs.
        /// </summary>
        public IEnumerable<string> Pins;
    }

    /// <summary>
    ///     Detailed information on a pin.
    /// </summary>
    public class PinDetailDto
    {
        /// <summary>
        ///     "recursive", "indirect", ...
        /// </summary>
        public string Type = "unknown";
    }

    /// <summary>
    ///     A map of pins.
    /// </summary>
    public class PinDetailsDto
    {
        /// <summary>
        ///     The pins.
        /// </summary>
        public Dictionary<string, PinDetailDto> Keys;
    }

    /// <summary>
    ///     Manage pinned objects (locally stored and permanent).
    /// </summary>
    public class PinController : IpfsController
    {
        /// <summary>
        ///     Creates a new controller.
        /// </summary>
        public PinController(IDfsService dfs) : base(dfs) { }

        /// <summary>
        ///     List the pins.
        /// </summary>
        [HttpGet] [HttpPost] [Route("pin/ls")]
        public async Task<PinDetailsDto> List()
        {
            var cids = await IpfsCore.PinApi.ListAsync(Cancel);
            return new PinDetailsDto
            {
                Keys = cids.ToDictionary(cid => cid.Encode(), cid => new PinDetailDto())
            };
        }

        /// <summary>
        ///     Pin the content.
        /// </summary>
        /// <param name="arg">
        ///     The CID of the content.
        /// </param>
        /// <param name="recursive">
        ///     Recursively pin links of the content.
        /// </param>
        [HttpGet] [HttpPost] [Route("pin/add")]
        public async Task<PinsDto> Add(string arg,
            bool recursive = true)
        {
            var cids = await IpfsCore.PinApi.AddAsync(arg, recursive, Cancel);
            return new PinsDto
            {
                Pins = cids.Select(cid => cid.Encode())
            };
        }

        /// <summary>
        ///     Remove a pin.
        /// </summary>
        /// <param name="arg">
        ///     The CID of the content.
        /// </param>
        /// <param name="recursive">
        ///     Recursively unpin links of the content.
        /// </param>
        [HttpGet] [HttpPost] [Route("pin/rm")]
        public async Task<PinsDto> Remove(string arg,
            bool recursive = true)
        {
            var cids = await IpfsCore.PinApi.RemoveAsync(arg, recursive, Cancel);
            return new PinsDto
            {
                Pins = cids.Select(cid => cid.Encode())
            };
        }
    }
}
