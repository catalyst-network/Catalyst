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

using System.Linq;
using Catalyst.Abstractions.Mempool.Services;
using Catalyst.Core.Lib.DAO.Transaction;
using Catalyst.Core.Lib.Util;
using Catalyst.Core.Modules.Mempool.Repositories;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using BaseController = Microsoft.AspNetCore.Mvc.Controller;

namespace Catalyst.Core.Modules.Web3.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public sealed class MempoolController : BaseController
    {
        private readonly MempoolService _mempoolService;

        public MempoolController(IMempoolService<PublicEntryDao> mempoolService)
        {
            _mempoolService = (MempoolService) mempoolService;
        }

        [HttpGet("{id}")]
        public PublicEntryDao Get(string id)
        {
            id = id.ToLowerInvariant();
            return _mempoolService.ReadItem(id);
        }

        [HttpGet]
        public JsonResult GetMempool()
        {
            return Json(_mempoolService.GetAll(), new JsonSerializerSettings
            {
                Converters = JsonConverterProviders.Converters.ToList()
            });
        }
    }
}
