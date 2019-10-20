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

namespace Catalyst.Core.Modules.Kvm
{
    // public interface IEthModule : IModule
    // {
    //     long eth_chainId();
    //     string eth_protocolVersion();
    //     SyncingResult eth_syncing();
    //     Address eth_coinbase();
    //     bool? eth_mining();
    //     ResultWrapper<byte[]> eth_snapshot();
    //     UInt256? eth_hashrate();
    //     UInt256? eth_gasPrice();
    //     IEnumerable<Address> eth_accounts();
    //     long? eth_blockNumber();
    //     UInt256? eth_getBalance(Address address, BlockParameter blockParameter);
    //     ResultWrapper<byte[]> eth_getStorageAt(Address address, UInt256 positionIndex, BlockParameter blockParameter);
    //     UInt256? eth_getTransactionCount(Address address, BlockParameter blockParameter);
    //     UInt256? eth_getBlockTransactionCountByHash(Keccak blockHash);
    //     UInt256? eth_getBlockTransactionCountByNumber(BlockParameter blockParameter);
    //     UInt256? eth_getUncleCountByBlockHash(Keccak blockHash);
    //     UInt256? eth_getUncleCountByBlockNumber(BlockParameter blockParameter);
    //     ResultWrapper<byte[]> eth_getCode(Address address, BlockParameter blockParameter);
    //     ResultWrapper<byte[]> eth_sign(Address addressData, byte[] message);
    //     Keccak eth_sendTransaction(TransactionForRpc transactionForRpc);
    //     Keccak eth_sendRawTransaction(byte[] transaction);
    //     ResultWrapper<byte[]> eth_call(TransactionForRpc transactionCall, BlockParameter blockParameter = null);
    //     UInt256? eth_estimateGas(TransactionForRpc transactionCall);
    //     BlockForRpc eth_getBlockByHash(Keccak blockHash, bool returnFullTransactionObjects);
    //     BlockForRpc eth_getBlockByNumber(BlockParameter blockParameter, bool returnFullTransactionObjects);
    //     TransactionForRpc eth_getTransactionByHash(Keccak transactionHash);
    //     TransactionForRpc eth_getTransactionByBlockHashAndIndex(Keccak blockHash, UInt256 positionIndex);
    //     TransactionForRpc eth_getTransactionByBlockNumberAndIndex(BlockParameter blockParameter, UInt256 positionIndex);
    //     ReceiptForRpc eth_getTransactionReceipt(Keccak txHashData);
    //     BlockForRpc eth_getUncleByBlockHashAndIndex(Keccak blockHashData, UInt256 positionIndex);
    //     BlockForRpc eth_getUncleByBlockNumberAndIndex(BlockParameter blockParameter, UInt256 positionIndex);
    //     UInt256? eth_newFilter(Filter filter);
    //     UInt256? eth_newBlockFilter();
    //     UInt256? eth_newPendingTransactionFilter();
    //     bool? eth_uninstallFilter(UInt256 filterId);
    //     IEnumerable<object> eth_getFilterChanges(UInt256 filterId);
    //     IEnumerable<FilterLog> eth_getFilterLogs(UInt256 filterId);
    //     IEnumerable<FilterLog> eth_getLogs(Filter filter);
    //     IEnumerable<byte[]> eth_getWork();
    //     bool? eth_submitWork(byte[] nonce, Keccak headerPowHash, byte[] mixDigest);
    //     bool? eth_submitHashrate(string hashRate, string id);
    //     AccountProof eth_getProof(Address accountAddress, byte[][] hashRate, BlockParameter blockParameter);
    // }
}
