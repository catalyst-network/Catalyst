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
using System.Linq;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Modules.Cryptography.BulletProofs;
using Catalyst.Core.Modules.Ledger;
using Catalyst.Protocol.Transaction;
using Google.Protobuf;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Core.Extensions;
using Nethermind.Core.Specs;
using Nethermind.Dirichlet.Numerics;
using Nethermind.Evm;
using Nethermind.Evm.Precompiles;
using Nethermind.Evm.Tracing;
using Nethermind.Store;
using Serilog;
using Serilog.Events;

namespace Catalyst.Core.Modules.Kvm
{
    public class ContractEntryExecutor : IContractEntryExecutor
    {
        private readonly IntrinsicGasCalculator _intrinsicGasCalculator = new IntrinsicGasCalculator();
        private readonly ILogger _logger;
        private readonly IStateProvider _stateProvider;
        private readonly IStorageProvider _storageProvider;
        private readonly ISpecProvider _specProvider;
        private readonly IVirtualMachine _virtualMachine;
        private readonly ICryptoContext _cryptoContext = new FfiWrapper();

        public ContractEntryExecutor(ISpecProvider specProvider, IStateProvider stateProvider, IStorageProvider storageProvider, IVirtualMachine virtualMachine, ILogger logger)
        {
            _logger = logger;
            _specProvider = specProvider;
            _virtualMachine = virtualMachine;
            _stateProvider = stateProvider;
            _storageProvider = storageProvider;
        }

        [Todo("Wider work needed to split calls and execution properly")]
        public void CallAndRestore(ContractEntry transaction, StateUpdate stateUpdate, ITxTracer txTracer) { Execute(transaction, stateUpdate, txTracer, true); }

        public void Execute(ContractEntry transaction, StateUpdate stateUpdate, ITxTracer txTracer) { Execute(transaction, stateUpdate, txTracer, false); }

        Address GetAccountAddress(ByteString publicKeyByteString)
        {
            if (publicKeyByteString == null)
            {
                return null;
            }

            var publicKey = _cryptoContext.GetPublicKeyFromBytes(publicKeyByteString.ToByteArray());
            return publicKey.ToKvmAddress();
        }

        private void QuickFail(ContractEntry entry, StateUpdate stateUpdate, ITxTracer txTracer, bool readOnly)
        {
            long gasLimit = 1_000_000L;
            var receiver = GetAccountAddress(entry.Base.ReceiverPublicKey);
            var sender = GetAccountAddress(entry.Base.SenderPublicKey);
            var value = entry.Amount.ToUInt256();
            stateUpdate.GasUsed += gasLimit;
            Address recipient = receiver ?? Address.OfContract(sender, _stateProvider.GetNonce(sender));
            if (txTracer.IsTracingReceipt) txTracer.MarkAsFailed(recipient, gasLimit, Bytes.Empty, "invalid");
        }

        private void Execute(ContractEntry entry, StateUpdate stateUpdate, ITxTracer txTracer, bool readOnly)
        {
            long gasLimit = 1_000_000L;
            UInt256 gasPrice = 0L;
            Address recipient = GetAccountAddress(entry.Base.ReceiverPublicKey);
            Address sender = GetAccountAddress(entry.Base.SenderPublicKey);
            var value = entry.Amount.ToUInt256();

            IReleaseSpec spec = _specProvider.GetSpec(stateUpdate.Number);
            byte[] machineCode = entry.Data.ToByteArray(); // to be changed to Init
            byte[] data = entry.Data.ToByteArray();

            // if (_logger.IsTrace) _logger.Trace($"Executing tx {transaction.Hash}");

            if (sender == null)
            {
                // TraceLogInvalidTx(entry, "SENDER_NOT_SPECIFIED");
                QuickFail(entry, stateUpdate, txTracer, readOnly);
                return;
            }

            // we need to review the intrinsic cost calculation
            long intrinsicGas = 0L;

            // long intrinsicGas = _intrinsicGasCalculator.Calculate(transaction, spec);
            //if (_logger.IsTrace) _logger.Trace($"Intrinsic gas calculated for {entry}: " + intrinsicGas);

            if (gasLimit < intrinsicGas)
            {
                // TraceLogInvalidTx(transaction, $"GAS_LIMIT_BELOW_INTRINSIC_GAS {gasLimit} < {intrinsicGas}");
                QuickFail(entry, stateUpdate, txTracer, readOnly);
                return;
            }

            if (gasLimit > stateUpdate.GasLimit - stateUpdate.GasUsed)
            {
                // TraceLogInvalidTx(transaction,
                // $"BLOCK_GAS_LIMIT_EXCEEDED {gasLimit} > {stateUpdate.GasLimit} - {stateUpdate.GasUsed}");
                QuickFail(entry, stateUpdate, txTracer, readOnly);
                return;
            }

            if (!_stateProvider.AccountExists(sender))
            {
                // TraceLogInvalidTx(transaction, $"SENDER_ACCOUNT_DOES_NOT_EXIST {sender}");
                if (gasPrice == UInt256.Zero)
                {
                    _stateProvider.CreateAccount(sender, UInt256.Zero);
                }
            }

            UInt256 senderBalance = _stateProvider.GetBalance(sender);
            if ((ulong) intrinsicGas * gasPrice + value > senderBalance)
            {
                // TraceLogInvalidTx(transaction, $"INSUFFICIENT_SENDER_BALANCE: ({sender})_BALANCE = {senderBalance}");
                QuickFail(entry, stateUpdate, txTracer, readOnly);
                return;
            }

            if (entry.Base.Nonce != _stateProvider.GetNonce(sender))
            {
                // TraceLogInvalidTx(transaction, $"WRONG_TRANSACTION_NONCE: {transaction.Nonce} (expected {_stateProvider.GetNonce(sender)})");
                QuickFail(entry, stateUpdate, txTracer, readOnly);
                return;
            }

            _stateProvider.IncrementNonce(sender);

            _stateProvider.SubtractFromBalance(sender, (ulong) gasLimit * gasPrice, spec);

            // TODO: I think we can skip this commit and decrease the tree operations this way
            _stateProvider.Commit(_specProvider.GetSpec(stateUpdate.Number), txTracer.IsTracingState ? txTracer : null);

            long unspentGas = gasLimit - intrinsicGas;
            long spentGas = gasLimit;

            int stateSnapshot = _stateProvider.TakeSnapshot();
            int storageSnapshot = _storageProvider.TakeSnapshot();

            _stateProvider.SubtractFromBalance(sender, value, spec);
            byte statusCode = StatusCode.Failure;
            TransactionSubstate substate = null;

            try
            {
                if (entry.IsValidDeploymentEntry)
                {
                    recipient = Address.OfContract(sender, _stateProvider.GetNonce(sender) - 1);
                    if (_stateProvider.AccountExists(recipient))
                    {
                        if ((_virtualMachine.GetCachedCodeInfo(recipient)?.MachineCode?.Length ?? 0) != 0 || _stateProvider.GetNonce(recipient) != 0)
                        {
                            if (_logger.IsEnabled(LogEventLevel.Verbose))
                            {
                                _logger.Verbose($"Contract collision at {recipient}"); // the account already owns the contract with the code
                            }

                            throw new TransactionCollisionException();
                        }

                        _stateProvider.UpdateStorageRoot(recipient, Keccak.EmptyTreeHash);
                    }
                }

                bool isPrecompile = recipient.IsPrecompiled(spec);

                ExecutionEnvironment env = new ExecutionEnvironment();
                env.Value = value;
                env.TransferValue = value;
                env.Sender = sender;
                env.CodeSource = recipient;
                env.ExecutingAccount = recipient;
                env.CurrentBlock = stateUpdate;
                env.GasPrice = gasPrice;
                env.InputData = data ?? new byte[0];
                env.CodeInfo = isPrecompile ? new CodeInfo(recipient) : machineCode == null ? _virtualMachine.GetCachedCodeInfo(recipient) : new CodeInfo(machineCode);
                env.Originator = sender;

                ExecutionType executionType = entry.IsValidDeploymentEntry ? ExecutionType.Create : ExecutionType.Call;
                using (VmState state = new VmState(unspentGas, env, executionType, isPrecompile, true, false))
                {
                    substate = _virtualMachine.Run(state, txTracer);
                    unspentGas = state.GasAvailable;
                }

                if (substate.ShouldRevert || substate.IsError)
                {
                    if (_logger.IsEnabled(LogEventLevel.Verbose)) _logger.Verbose("Restoring state from before transaction");
                    _stateProvider.Restore(stateSnapshot);
                    _storageProvider.Restore(storageSnapshot);
                }
                else
                {
                    // tks: there is similar code fo contract creation from init and from CREATE
                    // this may lead to inconsistencies (however it is tested extensively in blockchain tests)
                    if (entry.IsValidDeploymentEntry)
                    {
                        long codeDepositGasCost = CodeDepositHandler.CalculateCost(substate.Output.Length, spec);
                        if (unspentGas < codeDepositGasCost && spec.IsEip2Enabled)
                        {
                            throw new OutOfGasException();
                        }

                        if (unspentGas >= codeDepositGasCost)
                        {
                            Keccak codeHash = _stateProvider.UpdateCode(substate.Output);
                            _stateProvider.UpdateCodeHash(recipient, codeHash, spec);
                            unspentGas -= codeDepositGasCost;
                        }
                    }

                    foreach (Address toBeDestroyed in substate.DestroyList)
                    {
                        if (_logger.IsEnabled(LogEventLevel.Verbose)) _logger.Verbose($"Destroying account {toBeDestroyed}");
                        _stateProvider.DeleteAccount(toBeDestroyed);
                    }

                    statusCode = StatusCode.Success;
                }

                spentGas = Refund(gasLimit, unspentGas, substate, sender, gasPrice, spec);
            }
            catch (Exception ex) when (ex is EvmException || ex is OverflowException) // TODO: OverflowException? still needed? hope not
            {
                if (_logger.IsEnabled(LogEventLevel.Verbose)) _logger.Verbose($"EVM EXCEPTION: {ex.GetType().Name}");
                _stateProvider.Restore(stateSnapshot);
                _storageProvider.Restore(storageSnapshot);
            }

            if (_logger.IsEnabled(LogEventLevel.Verbose)) _logger.Verbose("Gas spent: " + spentGas);

            Address gasBeneficiary = stateUpdate.GasBeneficiary;
            if (statusCode == StatusCode.Failure || !(substate?.DestroyList.Contains(gasBeneficiary) ?? false))
            {
                if (!_stateProvider.AccountExists(gasBeneficiary))
                {
                    _stateProvider.CreateAccount(gasBeneficiary, (ulong) spentGas * gasPrice);
                }
                else
                {
                    _stateProvider.AddToBalance(gasBeneficiary, (ulong) spentGas * gasPrice, spec);
                }
            }

            if (!readOnly)
            {
                _storageProvider.Commit(txTracer.IsTracingState ? txTracer : null);
                _stateProvider.Commit(spec, txTracer.IsTracingState ? txTracer : null);
            }
            else
            {
                _storageProvider.Reset();
                _stateProvider.Reset();
            }

            if (!readOnly)
            {
                stateUpdate.GasUsed += spentGas;
            }

            if (txTracer.IsTracingReceipt)
            {
                if (statusCode == StatusCode.Failure)
                {
                    txTracer.MarkAsFailed(recipient, spentGas, (substate?.ShouldRevert ?? false) ? substate.Output : Bytes.Empty, substate?.Error);
                }
                else
                {
                    txTracer.MarkAsSuccess(recipient, spentGas, substate.Output, substate.Logs.Any() ? substate.Logs.ToArray() : LogEntry.EmptyLogs);
                }
            }
        }

        private long Refund(long gasLimit, long unspentGas, TransactionSubstate substate, Address sender, UInt256 gasPrice, IReleaseSpec spec)
        {
            long spentGas = gasLimit;
            if (!substate.IsError)
            {
                spentGas -= unspentGas;
                long refund = substate.ShouldRevert ? 0 : Math.Min(spentGas / 2L, substate.Refund + substate.DestroyList.Count * RefundOf.Destroy);

                if (_logger.IsEnabled(LogEventLevel.Verbose)) _logger.Verbose("Refunding unused gas of " + unspentGas + " and refund of " + refund);
                _stateProvider.AddToBalance(sender, (ulong) (unspentGas + refund) * gasPrice, spec);
                spentGas -= refund;
            }

            return spentGas;
        }
    }
}
