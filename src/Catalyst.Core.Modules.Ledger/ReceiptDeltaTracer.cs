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
using System.Collections.Generic;
using Catalyst.Abstractions.Ledger.Models;
using Catalyst.Core.Modules.Kvm;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.Transaction;
using Google.Protobuf;
using Lib.P2P;
using Nethermind.Core;
using Nethermind.Dirichlet.Numerics;
using Nethermind.Evm;
using Nethermind.Evm.Tracing;
using Nethermind.Store;

namespace Catalyst.Core.Modules.Ledger
{
    public sealed class ReceiptDeltaTracer : ITxTracer
    {
        readonly Delta _delta;
        readonly long _deltaNumber;
        readonly Cid _deltaHash;
        readonly List<TransactionReceipt> _txReceipts;
        int _currentIndex;

        public ReceiptDeltaTracer(Delta delta, Cid deltaHash)
        {
            _delta = delta;
            _deltaHash = deltaHash;
            _deltaNumber = delta.DeltaNumber;
            _txReceipts = new List<TransactionReceipt>(delta.PublicEntries.Count);
        }

        public IEnumerable<TransactionReceipt> Receipts => _txReceipts;

        public bool IsTracingReceipt => true;

        public void MarkAsSuccess(Address recipient, long gasSpent, byte[] output, LogEntry[] logs) { _txReceipts.Add(BuildReceipt(recipient, gasSpent, StatusCode.Success, logs)); }

        public void MarkAsFailed(Address recipient, long gasSpent, byte[] output, string error) { _txReceipts.Add(BuildFailedReceipt(recipient, gasSpent)); }

        private TransactionReceipt BuildFailedReceipt(Address recipient, long gasSpent) { return BuildReceipt(recipient, gasSpent, StatusCode.Failure, LogEntry.EmptyLogs); }

        private TransactionReceipt BuildReceipt(Address recipient, long spentGas, byte statusCode, LogEntry[] logEntries)
        {
            PublicEntry entry = _delta.PublicEntries[_currentIndex];

            TransactionReceipt txReceipt = new TransactionReceipt
            {
                Logs = logEntries,
                GasUsedTotal = _delta.GasUsed,
                StatusCode = statusCode,
                Recipient = entry.IsContractDeployment ? null :  recipient.ToString(),
                DeltaHash = _deltaHash,
                DeltaNumber = _deltaNumber,
                Index = _currentIndex,
                GasUsed = spentGas,
                Sender = GetAccountAddress(entry.SenderAddress).ToString(),
                ContractAddress = entry.IsContractDeployment ? recipient.ToString() : null,
            };

            _currentIndex += 1;

            return txReceipt;
        }

        private static Address GetAccountAddress(ByteString publicKeyByteString)
        {
            if (publicKeyByteString == null || publicKeyByteString.IsEmpty)
            {
                return null;
            }

            return publicKeyByteString.ToByteArray().ToKvmAddress();
        }

        public bool IsTracingActions => false;
        public bool IsTracingOpLevelStorage => false;
        public bool IsTracingMemory => false;
        public bool IsTracingInstructions => false;
        public bool IsTracingCode => false;
        public bool IsTracingStack => false;
        public bool IsTracingState => false;
        public void ReportBalanceChange(Address address, UInt256? before, UInt256? after) { throw new NotImplementedException(); }
        public void ReportCodeChange(Address address, byte[] before, byte[] after) { throw new NotImplementedException(); }
        public void ReportNonceChange(Address address, UInt256? before, UInt256? after) { throw new NotImplementedException(); }
        public void ReportStorageChange(StorageAddress storageAddress, byte[] before, byte[] after) { throw new NotImplementedException(); }
        public void StartOperation(int depth, long gas, Instruction opcode, int pc) { throw new NotImplementedException(); }
        public void ReportOperationError(EvmExceptionType error) { throw new NotImplementedException(); }
        public void ReportOperationRemainingGas(long gas) { throw new NotImplementedException(); }
        public void SetOperationStack(List<string> stackTrace) { throw new NotImplementedException(); }
        public void ReportStackPush(Span<byte> stackItem) { throw new NotImplementedException(); }
        public void SetOperationMemory(List<string> memoryTrace) { throw new NotImplementedException(); }
        public void SetOperationMemorySize(ulong newSize) { throw new NotImplementedException(); }
        public void ReportMemoryChange(long offset, Span<byte> data) { throw new NotImplementedException(); }
        public void ReportStorageChange(Span<byte> key, Span<byte> value) { throw new NotImplementedException(); }
        public void SetOperationStorage(Address address, UInt256 storageIndex, byte[] newValue, byte[] currentValue) { throw new NotImplementedException(); }
        public void ReportSelfDestruct(Address address, UInt256 balance, Address refundAddress) { throw new NotImplementedException(); }

        public void ReportAction(long gas,
            UInt256 value,
            Address @from,
            Address to,
            byte[] input,
            ExecutionType callType,
            bool isPrecompileCall = false)
        {
            throw new NotImplementedException();
        }

        public void ReportActionEnd(long gas, byte[] output) { throw new NotImplementedException(); }
        public void ReportActionError(EvmExceptionType evmExceptionType) { throw new NotImplementedException(); }
        public void ReportActionEnd(long gas, Address deploymentAddress, byte[] deployedCode) { throw new NotImplementedException(); }
        public void ReportByteCode(byte[] byteCode) { throw new NotImplementedException(); }
        public void ReportRefund(long gasAvailable) { throw new NotImplementedException(); }
    }
}
