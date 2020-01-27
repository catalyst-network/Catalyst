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
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs;
using Microsoft.AspNetCore.Mvc;

namespace Catalyst.Core.Modules.Dfs.WebApi.V0.Controllers
{
    /// <summary>
    ///   A cryptographic key.
    /// </summary>
    public class CryptoKeyDto
    {
        /// <summary>
        ///   The key's local name.
        /// </summary>
        public string Name { set; get; }

        /// <summary>
        ///   The key's global unique ID.
        /// </summary>
        public string Id { set; get; }
    }

    /// <summary>
    ///   A list of cryptographic keys.
    /// </summary>
    public class CryptoKeysDto
    {
        /// <summary>
        ///   A list of cryptographic keys.
        /// </summary>
        public IEnumerable<CryptoKeyDto> Keys { set; get; }
    }

    /// <summary>
    ///   A cryptographic key.
    /// </summary>
    public class CryptoKeyRenameDto
    {
        /// <summary>
        ///   The key's local name.
        /// </summary>
        public string Was { set; get; }

        /// <summary>
        ///   The key's global unique ID.
        /// </summary>
        public string Now { set; get; }

        /// <summary>
        ///   The key's global unique ID.
        /// </summary>
        public string Id { set; get; }

        /// <summary>
        ///   Indicates that a existing key was overwritten.
        /// </summary>
        public bool Overwrite { set; get; }
    }

    /// <summary>
    ///   Manages the cryptographic keys.
    /// </summary>
    public class KeyController : IpfsController
    {
        /// <summary>
        ///   Creates a new controller.
        /// </summary>
        public KeyController(IDfsService dfs) : base(dfs) { }

        /// <summary>
        ///   List all the keys.
        /// </summary>
        [HttpGet, HttpPost, Route("key/list")]
        public async Task<CryptoKeysDto> List()
        {
            var keys = await IpfsCore.KeyApi.ListAsync(Cancel);
            return new CryptoKeysDto
            {
                Keys = keys.Select(key => new CryptoKeyDto
                {
                    Name = key.Name,
                    Id = key.Id.ToString()
                })
            };
        }

        /// <summary>
        ///   Create a new key.
        /// </summary>
        /// <param name="arg">
        ///   The name of the key.
        /// </param>
        /// <param name="type">
        ///   "rsa"
        /// </param>
        /// <param name="size">
        ///   The key size in bits, if the type requires it.
        /// </param>
        [HttpGet, HttpPost, Route("key/gen")]
        public async Task<CryptoKeyDto> Create(string arg,
            string type,
            int size)
        {
            if (String.IsNullOrWhiteSpace(arg))
                throw new ArgumentNullException(nameof(arg), "The key name is required.");
            if (String.IsNullOrWhiteSpace(type))
                throw new ArgumentNullException(nameof(type), "The key type is required.");

            var key = await IpfsCore.KeyApi.CreateAsync(arg, type, size, Cancel);
            return new CryptoKeyDto
            {
                Name = key.Name,
                Id = key.Id.ToString()
            };
        }

        /// <summary>
        ///   Remove a key.
        /// </summary>
        /// <param name="arg">
        ///   The name of the key.
        /// </param>
        [HttpGet, HttpPost, Route("key/rm")]
        public async Task<CryptoKeysDto> Remove(string arg)
        {
            if (String.IsNullOrWhiteSpace(arg))
                throw new ArgumentNullException(nameof(arg), "The key name is required.");

            var key = await IpfsCore.KeyApi.RemoveAsync(arg, Cancel);
            var dto = new CryptoKeysDto();
            if (key != null)
            {
                dto.Keys = new[]
                {
                    new CryptoKeyDto
                    {
                        Name = key.Name,
                        Id = key.Id.ToString()
                    }
                };
            }

            return dto;
        }

        /// <summary>
        ///   Rename a key.
        /// </summary>
        /// <param name="arg">
        ///   The old and new key name.
        /// </param>
        [HttpGet, HttpPost, Route("key/rename")]
        public async Task<CryptoKeyRenameDto> Rename(string[] arg)
        {
            if (arg.Length != 2)
                throw new ArgumentException("Missing the old and/or new key name.");

            var key = await IpfsCore.KeyApi.RenameAsync(arg[0], arg[1], Cancel);
            var dto = new CryptoKeyRenameDto
            {
                Was = arg[0],
                Now = arg[1],
                Id = key.Id.ToString()

                // TODO: Overwrite
            };
            return dto;
        }

        // TODO: import
        // TODO: export
    }
}
