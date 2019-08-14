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

using Catalyst.Common.Interfaces.Repository;
using Catalyst.Common.Util;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Linq;


namespace Catalyst.Modules.Lib.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public sealed class MempoolController : Controller
    {
        private readonly IMempoolRepository _mempoolRepository;

        public MempoolController(IMempoolRepository mempoolRepository)
        {
            _mempoolRepository = mempoolRepository;
        }

        [HttpGet]
        public IActionResult GetBalance(string publicKey)
        {
            return Ok(_mempoolRepository.GetAll().Where(t =>
                t.Transaction.STEntries != null
                    && t.Transaction.STEntries.Count > 0
                    && t.Transaction.STEntries.Any(stEntries => stEntries.PubKey.ToByteArray().SequenceEqual(publicKey.KeyToBytes())))
                        .Sum(t => t.Transaction.STEntries.Sum(entries => entries.Amount)));
        }

        [HttpGet]
        public JsonResult GetMempoolTransaction(string publicKey)
        {
            var result = _mempoolRepository.GetAll().Where(t =>
              t.Transaction.STEntries != null
                  && t.Transaction.STEntries.Count > 0
                  && t.Transaction.STEntries.Any(stEntries => stEntries.PubKey.ToByteArray().SequenceEqual(publicKey.KeyToBytes())));
            return Json(result, new JsonSerializerSettings
            {
                Converters = JsonConverterProviders.Converters
            });
        }

        [HttpGet]
        public JsonResult GetMempool()
        {
            return Json(_mempoolRepository.GetAll(), new JsonSerializerSettings
            {
                Converters = JsonConverterProviders.Converters
            });
        }
    }
}
