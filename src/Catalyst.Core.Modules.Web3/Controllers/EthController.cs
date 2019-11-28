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
using Catalyst.Abstractions.Kvm;
using Catalyst.Abstractions.Kvm.Models;
using Catalyst.Abstractions.Ledger;
using Catalyst.Core.Modules.Web3.Controllers.Handlers;
using Microsoft.AspNetCore.Mvc;
using Nethermind.Core;
using Serilog;

namespace Catalyst.Core.Modules.Web3.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class EthController : Controller
    {
        private readonly IWeb3EthApi _web3EthApi;
        private readonly IWeb3HandlerResolver _handlerResolver;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ILogger _logger = Log.Logger.ForContext(typeof(EthController));

        public EthController(IWeb3EthApi web3EthApi, IWeb3HandlerResolver handlerResolver, IJsonSerializer jsonSerializer)
        {
            _web3EthApi = web3EthApi ?? throw new ArgumentNullException(nameof(web3EthApi));
            _handlerResolver = handlerResolver ?? throw new ArgumentNullException(nameof(handlerResolver));
            _jsonSerializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));
        }

        [HttpPost]
        public JsonRpcResponse Request([FromBody] JsonRpcRequest request)
        {
            _logger.Information("ETH JSON RPC request {id} {method} {params}", request.Id, request.Method, request.Params);
            EthWeb3RequestHandlerBase handler = _handlerResolver.Resolve(request.Method, request.Params.Length);
            if (handler == null)
            {
                return new JsonRpcErrorResponse {Result = null, Error = new Error {Code = (int) ErrorType.MethodNotFound, Data = $"{request.Method}", Message = "Method not found"}};
            }

            object result = handler.Handle(request.Params, _web3EthApi, _jsonSerializer);
            return new JsonRpcResponse(request, result);
        }
    }
}