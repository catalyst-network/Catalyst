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

using Catalyst.Abstractions.Kvm;
using Catalyst.Abstractions.Kvm.Models;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Catalyst.Core.Modules.Web3.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class EthController : Controller
    {
        private readonly IEthRpcService _ethRpcService;
        private readonly ILogger _logger = Log.Logger.ForContext(typeof(EthController));

        public EthController(IEthRpcService ethRpcService)
        {
            _ethRpcService = ethRpcService;
        }

        public EthController()
        {
        }

        [HttpPost]
        public JsonRpcResponse<int> Request([FromBody] JsonRpcRequest request)
        {
            _logger.Information("ETH JSON RPC request {id} {method}", request.Id, request.Method);
            return new JsonRpcResponse<int> {Id = request.Id, Result = 1, JsonRpc = request.JsonRpc};
        }
    }
}