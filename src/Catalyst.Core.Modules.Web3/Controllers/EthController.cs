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
using Microsoft.AspNetCore.Mvc;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Dirichlet.Numerics;
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
        public JsonRpcResponse Request([FromBody] JsonRpcRequest request)
        {
            _logger.Information("ETH JSON RPC request {id} {method} {params}", request.Id, request.Method, request.Params);
            switch (request.Method)
            {
                case "eth_blockNubmer":
                {
                    object result = BlockNumber();
                    return new JsonRpcResponse {Id = request.Id, Result = result, JsonRpc = request.JsonRpc};
                }
                case "eth_getBlockByNumber":
                {
                    object result = GetBlockByNumber(1);
                    return new JsonRpcResponse {Id = request.Id, Result = result, JsonRpc = request.JsonRpc};
                }
                default:
                {
                    return new JsonRpcResponse {Id = request.Id, Result = 1, JsonRpc = request.JsonRpc};
                }
            }
        }

        private BlockForRpc GetBlockByNumber(long number)
        {
            BlockForRpc blockForRpc = new BlockForRpc();
            blockForRpc.Miner = Address.Zero;
            blockForRpc.Difficulty = 1000000;
            blockForRpc.Hash = Keccak.Compute(number.ToString());
            blockForRpc.Number = number;
            blockForRpc.GasLimit = 10_000_000;
            blockForRpc.GasUsed = 0;
            blockForRpc.Timestamp = (UInt256)number;
            blockForRpc.ParentHash = number == 0 ? Keccak.Zero : Keccak.Compute((number - 1).ToString());
            blockForRpc.StateRoot = Keccak.EmptyTreeHash;
            blockForRpc.ReceiptsRoot = Keccak.EmptyTreeHash;
            blockForRpc.TransactionsRoot = Keccak.EmptyTreeHash;
            blockForRpc.LogsBloom = Bloom.Empty;
            blockForRpc.TotalDifficulty = (UInt256)((long)blockForRpc.Difficulty * number);
            blockForRpc.Sha3Uncles = Keccak.OfAnEmptySequenceRlp;
            return blockForRpc;
        }

        private long BlockNumber()
        {
            return 0L;
        }
    }
}