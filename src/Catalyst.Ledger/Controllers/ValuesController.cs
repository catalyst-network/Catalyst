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

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.NodeServices;
using Serilog;

namespace Catalyst.Ledger.Controllers
{
    /// <summary>
    /// Use this to interact with OrbitDb js libraries
    /// </summary>
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class OrbitDbController : Controller
    {
        private const string JsPath = "./node/index.js";

        private readonly INodeServices _nodeServices;
        private readonly string _dbName;
        private readonly ILogger _logger;

        public OrbitDbController(INodeServices nodeServices, ILogger logger = null)
        {
            _nodeServices = nodeServices;
            _logger = logger ?? Log.Logger.ForContext<OrbitDbController>();
        }

        [HttpPost]
        public async Task<JsonResult> Create()
        {
            var result = await _nodeServices.InvokeExportAsync<dynamic>(
                moduleName: JsPath,
                "createLogDb");

            return new JsonResult(result);
        }

        [HttpGet]
        public async Task<JsonResult> GetLog(string hash)
        {
            _logger.Debug("Retrieving db log entry for ");
            var result = await _nodeServices.InvokeExportAsync<dynamic>(
                JsPath,
                "getLog",
                hash);

            return new JsonResult(result);
        }

        [HttpPost]
        public async Task<JsonResult> AddLog(object content)
        {
            _logger.Debug("Adding db log entry");
            var result = await _nodeServices.InvokeExportAsync<dynamic>(
                JsPath,
                "addLog",
                content);

            return new JsonResult(result);
        }
    }
}
