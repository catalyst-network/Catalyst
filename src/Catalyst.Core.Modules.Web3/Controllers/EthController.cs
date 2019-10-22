#region LICENSE
// 
// Copyright (c) 2019 Catalyst Network
// 
// This file is part of Catalyst.Node <https://github.com/catalyst-network/Catalyst.Node>
// 
// Catalyst.Node is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// Catalyst.Node is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Catalyst.Node. If not, see <https://www.gnu.org/licenses/>.
#endregion

using Catalyst.Abstractions.Kvm;
using Catalyst.Abstractions.Kvm.Models;
using Microsoft.AspNetCore.Mvc;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Dirichlet.Numerics;

namespace Catalyst.Core.Modules.Web3.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class EthController : Controller, IEthRpcService
    {
        private readonly IEthRpcService _ethRpcService;

        public EthController(IEthRpcService ethRpcService) { _ethRpcService = ethRpcService; }

        [HttpGet]
        public long? eth_blockNumber()
        {
            return _ethRpcService.eth_blockNumber();
        }

        [HttpGet]
        public UInt256? eth_getBalance(Address address, BlockParameter blockParameter)
        {
            return _ethRpcService.eth_getBalance(address, blockParameter);
        }

        [HttpGet]
        public ResultWrapper<byte[]> eth_getStorageAt(Address address,
            UInt256 positionIndex,
            BlockParameter blockParameter)
        {
            return _ethRpcService.eth_getStorageAt(address, positionIndex, blockParameter);
        }

        [HttpGet]
        public UInt256? eth_getTransactionCount(Address address, BlockParameter blockParameter)
        {
            return _ethRpcService.eth_getTransactionCount(address, blockParameter);
        }

        [HttpGet]
        public ResultWrapper<byte[]> eth_getCode(Address address, BlockParameter blockParameter)
        {
            return _ethRpcService.eth_getCode(address, blockParameter);
        }

        [HttpGet]
        public Keccak eth_sendRawTransaction(byte[] transaction)
        {
            return _ethRpcService.eth_sendRawTransaction(transaction);
        }

        [HttpGet]
        public ResultWrapper<byte[]> eth_call(TransactionForRpc transactionCall, BlockParameter blockParameter = null)
        {
            return _ethRpcService.eth_call(transactionCall, blockParameter);
        }

        [HttpGet]
        public UInt256? eth_estimateGas(TransactionForRpc transactionCall)
        {
            return _ethRpcService.eth_estimateGas(transactionCall);
        }

        [HttpGet]
        public BlockForRpc eth_getBlockByNumber(BlockParameter blockParameter, bool returnFullTransactionObjects)
        {
            return _ethRpcService.eth_getBlockByNumber(blockParameter, returnFullTransactionObjects);
        }

        [HttpGet]
        public TransactionForRpc eth_getTransactionByHash(Keccak transactionHash)
        {
            return _ethRpcService.eth_getTransactionByHash(transactionHash);
        }

        [HttpGet]
        public TransactionForRpc eth_getTransactionByBlockNumberAndIndex(BlockParameter blockParameter,
            UInt256 positionIndex)
        {
            return _ethRpcService.eth_getTransactionByBlockNumberAndIndex(blockParameter, positionIndex);
        }

        [HttpGet]
        public ReceiptForRpc eth_getTransactionReceipt(Keccak txHashData)
        {
            return _ethRpcService.eth_getTransactionReceipt(txHashData);
        }

        [HttpGet]
        public BlockForRpc eth_getUncleByBlockHashAndIndex(Keccak blockHashData, UInt256 positionIndex)
        {
            return _ethRpcService.eth_getUncleByBlockHashAndIndex(blockHashData, positionIndex);
        }
    }
}
