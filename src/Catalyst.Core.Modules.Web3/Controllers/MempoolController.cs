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
using Catalyst.Abstractions.Mempool.Repositories;
using Catalyst.Core.Lib.DAO;
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
        private readonly MempoolDocumentRepository _mempoolRepository;

        public MempoolController(IMempoolRepository<TransactionBroadcastDao> mempoolRepository)
        {
            _mempoolRepository = (MempoolDocumentRepository) mempoolRepository;
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
