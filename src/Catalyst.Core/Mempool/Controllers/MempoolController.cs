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

using System.Linq;
using Catalyst.Abstractions.Mempool.Models;
using Catalyst.Abstractions.Mempool.Repositories;
using Catalyst.Core.Util;
using Google.Protobuf;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using BaseController = Microsoft.AspNetCore.Mvc.Controller;

namespace Catalyst.Core.Mempool.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public sealed class MempoolController<T> : BaseController where T : class, IMempoolItem
    {
        private readonly IMempoolRepository<T> _mempoolRepository;

        public MempoolController(IMempoolRepository<T> mempoolRepository)
        {
            _mempoolRepository = mempoolRepository;
        }

        [HttpGet]
        public IActionResult GetBalance(string publicKey)
        {
            return Ok(_mempoolRepository.GetAll().Where(t =>
                    t.STEntries != null
                 && t.STEntries.Count > 0
                 && t.STEntries.Any(stEntries => stEntries.PubKey.ToByteArray()
                       .SequenceEqual(ByteString.FromBase64(publicKey).ToByteArray())))
               .Sum(t => t.STEntries.Sum(entries => entries.Amount)));
        }

        [HttpGet]
        public JsonResult GetMempoolTransaction(string publicKey)
        {
            var result = _mempoolRepository.GetAll().Where(t =>
                t.STEntries != null
             && t.STEntries.Count > 0
             && t.STEntries.Any(stEntries => stEntries.PubKey.ToByteArray()
                   .SequenceEqual(ByteString.FromBase64(publicKey).ToByteArray())));

            return Json(result, new JsonSerializerSettings
            {
                Converters = JsonConverterProviders.Converters.ToList()
            });
        }

        [HttpGet]
        public JsonResult GetMempool()
        {
            return Json(_mempoolRepository.GetAll(), new JsonSerializerSettings
            {
                Converters = JsonConverterProviders.Converters.ToList()
            });
        }
    }
}
