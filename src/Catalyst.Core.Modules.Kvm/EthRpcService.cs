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
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Dirichlet.Numerics;

namespace Catalyst.Core.Modules.Kvm
{
    public class EthRpcService : IEthRpcService
    {
        public long? eth_blockNumber() { throw new NotImplementedException(); }

        public UInt256? eth_getBalance(Address address, BlockParameter blockParameter)
        {
            throw new NotImplementedException();
        }

        public ResultWrapper<byte[]> eth_getStorageAt(Address address,
            UInt256 positionIndex,
            BlockParameter blockParameter)
        {
            throw new NotImplementedException();
        }

        public UInt256? eth_getTransactionCount(Address address, BlockParameter blockParameter)
        {
            throw new NotImplementedException();
        }

        public ResultWrapper<byte[]> eth_getCode(Address address, BlockParameter blockParameter)
        {
            throw new NotImplementedException();
        }

        public Keccak eth_sendRawTransaction(byte[] transaction) { throw new NotImplementedException(); }

        public ResultWrapper<byte[]> eth_call(TransactionForRpc transactionCall, BlockParameter blockParameter = null)
        {
            throw new NotImplementedException();
        }

        public UInt256? eth_estimateGas(TransactionForRpc transactionCall) { throw new NotImplementedException(); }

        public BlockForRpc eth_getBlockByNumber(BlockParameter blockParameter, bool returnFullTransactionObjects)
        {
            throw new NotImplementedException();
        }

        public TransactionForRpc eth_getTransactionByHash(Keccak transactionHash)
        {
            throw new NotImplementedException();
        }

        public TransactionForRpc eth_getTransactionByBlockNumberAndIndex(BlockParameter blockParameter,
            UInt256 positionIndex)
        {
            throw new NotImplementedException();
        }

        public ReceiptForRpc eth_getTransactionReceipt(Keccak txHashData) { throw new NotImplementedException(); }

        public BlockForRpc eth_getUncleByBlockHashAndIndex(Keccak blockHashData, UInt256 positionIndex)
        {
            throw new NotImplementedException();
        }
    }
}
