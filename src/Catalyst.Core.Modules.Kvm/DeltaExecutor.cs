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
using Catalyst.Protocol.Deltas;
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
    /// <summary>
    ///     Delta executor is responsible for executing delta entries code on the VM. Its responsibility is to prepare
    ///     the execution environment and initial state of the VM as well as support some contract deployment operations.
    ///     There is a minor responsibility issue in the way the code deposits are handled - depending on the execution
    ///     level the deposit may happen inside the <see cref="VirtualMachine" /> or <see cref="DeltaExecutor" /> code.
    /// </summary>
    public class DeltaExecutor : IDeltaExecutor
    {
        private readonly ICryptoContext _cryptoContext = new FfiWrapper();
        private readonly ILogger _logger;
        private readonly ISpecProvider _specProvider;
        private readonly IStateProvider _stateProvider;
        private readonly IStorageProvider _storageProvider;
        private readonly IVirtualMachine _virtualMachine;

        /// <summary>
        ///     Note that there is a distinct approach to state and storage even as only together they form the 'state'
        ///     in the business sense. <see cref="IStorageProvider" /> needs to handle storage trees for various accounts
        ///     while <see cref="IStateProvider" /> handles the basic accounts state with storage roots only.
        /// </summary>
        /// <param name="specProvider">The network upgrade spec - defines the virtual machine version.</param>
        /// <param name="stateProvider">Access to accounts.</param>
        /// <param name="storageProvider">Access to accounts' storage.</param>
        /// <param name="virtualMachine">A virtual machine to execute the code on.</param>
        /// <param name="logger">Logger for the execution details.</param>
        public DeltaExecutor(ISpecProvider specProvider,
            IStateProvider stateProvider,
            IStorageProvider storageProvider,
            IVirtualMachine virtualMachine,
            ILogger logger)
        {
            _logger = logger;
            _specProvider = specProvider;
            _virtualMachine = virtualMachine;
            _stateProvider = stateProvider;
            _storageProvider = storageProvider;
        }

        [Todo("Wider work needed to split calls and execution properly")]
        public void CallAndRestore(Delta stateUpdate, ITxTracer txTracer) { Execute(stateUpdate, txTracer, true); }

        [Todo("After delta is executed we should validate the state root and if it is not as expected we should revert all the changes.")]
        public void Execute(Delta stateUpdate, ITxTracer txTracer) { Execute(stateUpdate, txTracer, false); }

        /// <summary>
        ///     This method calculates the basic gas cost of the <paramref name="entry" />
        ///     (excluding the cost of the VM execution).
        ///     The intrinsic cost is calculated as 21000 base cost + the entry data cost that is a function
        ///     of the number of bytes of data (and the cost of a single byte may differ between 0 bytes and non-0 bytes.
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="releaseSpec"></param>
        /// <returns>Total intrinsic cost of the <paramref name="entry" /></returns>
        public ulong CalculateIntrinsicGas(ContractEntry entry, IReleaseSpec releaseSpec)
        {
            ulong result = GasCostOf.Transaction; // the basic entry cost
            if (entry.Data != null)
            {
                // here is the difference between the 0 bytes and non-zero bytes cost
                // justified by a better compression level of zero bytes
                long txDataNonZeroGasCost = releaseSpec.IsEip2028Enabled ? GasCostOf.TxDataNonZeroEip2028 : GasCostOf.TxDataNonZero;
                int length = entry.Data.Length;
                for (int i = 0; i < length; i++)
                {
                    result += entry.Data[i] == 0 ? GasCostOf.TxDataZero : (ulong) txDataNonZeroGasCost;
                }
            }

            if (entry.IsValidDeploymentEntry && releaseSpec.IsEip2Enabled)
            {
                result += GasCostOf.TxCreate;
            }

            return result;
        }

        [Todo(Improve.MissingFunctionality, "We need to agree on delta to state mapping details")]
        private StateUpdate ToStateUpdate(Delta delta)
        {
            StateUpdate result = new StateUpdate
            {
                Difficulty = 1,
                Number = 1,
                Timestamp = (UInt256) delta.TimeStamp.Seconds,
                GasLimit = (long) delta.GasLimit,
                /* here we can read coinbase entries from the delta
                   but we need to decide how to split fees and which one to pick for the KVM */
                GasBeneficiary = Address.Zero,
                GasUsed = 0L
            };

            return result;
        }

        private Address GetAccountAddress(ByteString publicKeyByteString)
        {
            if (publicKeyByteString == null || publicKeyByteString.IsEmpty)
            {
                return null;
            }

            IPublicKey publicKey = _cryptoContext.GetPublicKeyFromBytes(publicKeyByteString.ToByteArray());
            return publicKey.ToKvmAddress();
        }

        private void QuickFail(ContractEntry entry, ExecutionEnvironment env, ITxTracer txTracer)
        {
            // here we need to propagate back to Delta
            env.CurrentBlock.GasUsed += (long) entry.GasLimit;
            if (txTracer.IsTracingReceipt)
            {
                txTracer.MarkAsFailed(env.ExecutingAccount, (long) entry.GasLimit, Bytes.Empty, "invalid");
            }
        }

        private void Execute(Delta delta, ITxTracer txTracer, bool readOnly)
        {
            StateUpdate stateUpdate = ToStateUpdate(delta);
            foreach (PublicEntry publicEntry in delta.PublicEntries)
            {
                ContractEntry contractEntry = new ContractEntry();
                contractEntry.Base = publicEntry.Base;
                contractEntry.Amount = publicEntry.Amount;
                contractEntry.Data = ByteString.Empty;
                contractEntry.GasLimit = 21000;
                Execute(contractEntry, stateUpdate, txTracer, readOnly);
            }

            // revert state if any fails (take snapshot)
            foreach (ContractEntry contractEntry in delta.ContractEntries)
            {
                Execute(contractEntry, stateUpdate, txTracer, readOnly);
            }

            if (!readOnly)
            {
                // we should assign block rewards here (or in Ledger)
                _stateProvider.CommitTree();
                _storageProvider.CommitTrees();
            }
            else
            {
                _storageProvider.Reset();
                _stateProvider.Reset();
            }
        }

        private void TraceLogInvalidTx(ContractEntry entry, string reason)
        {
            if (_logger.IsEnabled(LogEventLevel.Verbose))
            {
                _logger.Verbose("Invalid enrty {entry} ({reason})", entry, reason);
            }
        }

        /// <summary>
        /// Executes the <paramref name="entry"/>.
        /// </summary>
        /// <param name="entry">Transaction entry to be executed inside the <see cref="VirtualMachine"/>.</param>
        /// <param name="stateUpdate"><see cref="Delta"/> to be used for execution environment construction</param>
        /// <param name="txTracer">Tracer to extract the execution steps for debugging or analytics.</param>
        /// <param name="readOnly">Defines whether the state should be reverted after the execution.</param>
        /// <exception cref="TransactionCollisionException">Thrown when deployment address already has some code.</exception>
        /// <exception cref="OutOfGasException">Thrown when not enough gas is available for deposit.</exception>
        private void Execute(ContractEntry entry, StateUpdate stateUpdate, ITxTracer txTracer, bool readOnly)
        {
            IReleaseSpec spec = _specProvider.GetSpec(stateUpdate.Number);

            (Address sender, Address recipient) = ExtractSenderAndRecipient(entry);
            bool isPrecompile = recipient.IsPrecompiled(spec);
            ExecutionEnvironment env = PrepareEnv(entry, sender, recipient, stateUpdate, isPrecompile);

            ulong gasLimit = entry.GasLimit;
            ulong intrinsicGas = CalculateIntrinsicGas(entry, spec);

            if (_logger.IsEnabled(LogEventLevel.Verbose))
            {
                _logger.Verbose("Executing entry {entry}", entry);
            }

            if (!ValidateSender(entry, env, txTracer))
            {
                return;
            }

            if (!ValidateIntrinsicGas(entry, env, intrinsicGas, txTracer))
            {
                return;
            }

            if (!ValidateDeltaGasLimit(entry, env, txTracer))
            {
                return;
            }

            if (!_stateProvider.AccountExists(env.Sender))
            {
                if (env.GasPrice == UInt256.Zero)
                {
                    _stateProvider.CreateAccount(env.Sender, UInt256.Zero);
                }
            }

            if (!ValidateSenderBalance(entry, env, intrinsicGas, txTracer))
            {
                return;
            }

            if (!ValidateNonce(entry, env, txTracer))
            {
                return;
            }

            InitEntryExecution(env, gasLimit, spec, txTracer);

            // we prepare two fields to track the amount of gas spent / left
            ulong unspentGas = gasLimit - intrinsicGas;
            ulong spentGas = gasLimit;

            // the snapshots are needed to revert the subroutine state changes in case of an VM exception
            int stateSnapshot = _stateProvider.TakeSnapshot();
            int storageSnapshot = _storageProvider.TakeSnapshot();

            // we subtract value from sender
            // it will be added to recipient at the later stage (inside the VM)
            _stateProvider.SubtractFromBalance(sender, env.Value, spec);

            // we fail unless we succeed
            byte statusCode = StatusCode.Failure;
            TransactionSubstate substate = null;

            try
            {
                if (entry.IsValidDeploymentEntry)
                {
                    PrepareContractAccount(env.CodeSource);
                }
                
                ExecutionType executionType = entry.IsValidDeploymentEntry ? ExecutionType.Create : ExecutionType.Call;
                using (VmState state = new VmState((long) unspentGas, env, executionType, isPrecompile, true, false))
                {
                    substate = _virtualMachine.Run(state, txTracer);
                    unspentGas = (ulong) state.GasAvailable;
                }

                if (substate.ShouldRevert || substate.IsError)
                {
                    if (_logger.IsEnabled(LogEventLevel.Verbose))
                    {
                        _logger.Verbose("Restoring state from before transaction");
                    }
                    
                    _stateProvider.Restore(stateSnapshot);
                    _storageProvider.Restore(storageSnapshot);
                }
                else
                {
                    if (entry.IsValidDeploymentEntry)
                    {
                        DeployCode(env, substate, ref unspentGas, spec);
                    }
                    
                    DestroyAccounts(substate);
                    statusCode = StatusCode.Success;
                }

                spentGas = Refund(gasLimit, unspentGas, substate, env, spec);
            }
            catch (Exception ex) when (ex is EvmException || ex is OverflowException)
            {
                if (_logger.IsEnabled(LogEventLevel.Verbose))
                {
                    _logger.Verbose($"EVM EXCEPTION: {ex.GetType().Name}");
                }
                
                _stateProvider.Restore(stateSnapshot);
                _storageProvider.Restore(storageSnapshot);
            }

            if (_logger.IsEnabled(LogEventLevel.Verbose))
            {
                _logger.Verbose("Gas spent: " + spentGas);
            }

            Address gasBeneficiary = stateUpdate.GasBeneficiary;
            bool wasBeneficiaryAccountDestroyed = statusCode != StatusCode.Failure
             && (substate?.DestroyList.Contains(gasBeneficiary) ?? false);
            if (!wasBeneficiaryAccountDestroyed)
            {
                if (!_stateProvider.AccountExists(gasBeneficiary))
                {
                    _stateProvider.CreateAccount(gasBeneficiary, (ulong) spentGas * env.GasPrice);
                }
                else
                {
                    _stateProvider.AddToBalance(gasBeneficiary, (ulong) spentGas * env.GasPrice, spec);
                }
            }

            if (!readOnly)
            {
                _storageProvider.Commit(txTracer.IsTracingState ? txTracer : null);
                _stateProvider.Commit(spec, txTracer.IsTracingState ? txTracer : null);
                stateUpdate.GasUsed += (long) spentGas;
            }
            else
            {
                _storageProvider.Reset();
                _stateProvider.Reset();
            }

            if (txTracer.IsTracingReceipt)
            {
                if (statusCode == StatusCode.Failure)
                {
                    txTracer.MarkAsFailed(env.CodeSource, (long) spentGas, substate?.ShouldRevert ?? false ? substate.Output : Bytes.Empty, substate?.Error);
                }
                else
                {
                    if (substate == null)
                    {
                        throw new InvalidOperationException("Substate should not be null after a successful VM run.");
                    }
                    
                    txTracer.MarkAsSuccess(env.CodeSource, (long) spentGas, substate.Output, substate.Logs.Any() ? substate.Logs.ToArray() : LogEntry.EmptyLogs);
                }
            }
        }

        /// <summary>
        /// Accounts for which SELF_DESTRUCT opcode was invoked during the tx execution.
        /// Note that one of them can be coinbase / validator and in such case the miner / validator rewards are lost.
        /// </summary>
        /// <param name="substate"></param>
        private void DestroyAccounts(TransactionSubstate substate)
        {
            foreach (Address toBeDestroyed in substate.DestroyList)
            {
                if (_logger.IsEnabled(LogEventLevel.Verbose))
                {
                    _logger.Verbose($"Destroying account {toBeDestroyed}");
                }
                
                _stateProvider.DeleteAccount(toBeDestroyed);
            }
        }
        
        private void DeployCode(ExecutionEnvironment env, TransactionSubstate substate, ref ulong unspentGas, IReleaseSpec spec)
        {
            ulong codeDepositGasCost = (ulong) CodeDepositHandler.CalculateCost(substate.Output.Length, spec);
            if (unspentGas < codeDepositGasCost && spec.IsEip2Enabled)
            {
                throw new OutOfGasException();
            }

            if (unspentGas >= codeDepositGasCost)
            {
                Keccak codeHash = _stateProvider.UpdateCode(substate.Output);
                _stateProvider.UpdateCodeHash(env.CodeSource, codeHash, spec);
                unspentGas -= codeDepositGasCost;
            }
        }

        private (Address sender, Address recipient) ExtractSenderAndRecipient(ContractEntry entry)
        {
            Address sender = GetAccountAddress(entry.Base.SenderPublicKey);
            Address recipient = entry.TargetContract == null
                ? GetAccountAddress(entry.Base.ReceiverPublicKey)
                : new Address(entry.TargetContract);
            if (entry.IsValidDeploymentEntry)
            {
                recipient = Address.OfContract(sender, _stateProvider.GetNonce(sender));
            }

            return (sender, recipient);
        }

        private ExecutionEnvironment PrepareEnv(ContractEntry entry,
            Address sender,
            Address recipient,
            StateUpdate stateUpdate,
            bool isPrecompile)
        {
            UInt256 value = entry.Amount.ToUInt256();
            var machineCode = entry.IsValidDeploymentEntry ? entry.Data.ToByteArray() : null;
            var data = entry.IsValidDeploymentEntry ? null : entry.Data.ToByteArray();

            ExecutionEnvironment env = new ExecutionEnvironment();
            env.Value = value;
            env.TransferValue = value;
            env.Sender = sender;
            env.CodeSource = recipient;
            env.ExecutingAccount = recipient;
            env.CurrentBlock = stateUpdate;
            env.GasPrice = entry.GasPrice;
            env.InputData = data ?? new byte[0];
            env.CodeInfo = isPrecompile
                ? new CodeInfo(recipient)
                : machineCode == null
                    ? _virtualMachine.GetCachedCodeInfo(recipient)
                    : new CodeInfo(machineCode);
            env.Originator = sender;
            return env;
        }

        private bool ValidateDeltaGasLimit(ContractEntry entry, ExecutionEnvironment env, ITxTracer txTracer)
        {
            if (entry.GasLimit > (ulong) (env.CurrentBlock.GasLimit - env.CurrentBlock.GasUsed))
            {
                TraceLogInvalidTx(entry, $"BLOCK_GAS_LIMIT_EXCEEDED {entry.GasLimit} > {env.CurrentBlock.GasLimit} - {env.CurrentBlock.GasUsed}");
                QuickFail(entry, env, txTracer);
                return false;
            }

            return true;
        }

        private bool ValidateIntrinsicGas(ContractEntry entry,
            ExecutionEnvironment env,
            ulong intrinsicGas,
            ITxTracer txTracer)
        {
            if (_logger.IsEnabled(LogEventLevel.Verbose))
            {
                _logger.Verbose("Intrinsic gas calculated for {entry}: {intrinsicGas}", entry, intrinsicGas);
            }

            if (entry.GasLimit < intrinsicGas)
            {
                TraceLogInvalidTx(entry, $"GAS_LIMIT_BELOW_INTRINSIC_GAS {entry.GasLimit} < {intrinsicGas}");
                QuickFail(entry, env, txTracer);
                return false;
            }

            return true;
        }

        private bool ValidateSender(ContractEntry entry, ExecutionEnvironment env, ITxTracer txTracer)
        {
            if (env.Sender == null)
            {
                TraceLogInvalidTx(entry, "SENDER_NOT_SPECIFIED");
                QuickFail(entry, env, txTracer);
                return false;
            }

            return true;
        }

        private bool ValidateNonce(ContractEntry entry, ExecutionEnvironment env, ITxTracer txTracer)
        {
            if (entry.Base.Nonce != _stateProvider.GetNonce(env.Sender))
            {
                TraceLogInvalidTx(entry, $"WRONG_TRANSACTION_NONCE: {entry.Base.Nonce} (expected {_stateProvider.GetNonce(env.Sender)})");
                QuickFail(entry, env, txTracer);
                return false;
            }

            return true;
        }

        private bool ValidateSenderBalance(ContractEntry entry,
            ExecutionEnvironment env,
            ulong intrinsicGas,
            ITxTracer txTracer)
        {
            UInt256 senderBalance = _stateProvider.GetBalance(env.Sender);
            if (intrinsicGas * env.GasPrice + env.Value > senderBalance)
            {
                TraceLogInvalidTx(entry, $"INSUFFICIENT_SENDER_BALANCE: ({env.Sender})_BALANCE = {senderBalance}");
                QuickFail(entry, env, txTracer);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Prepares and validates the account address for contract deployment.
        /// </summary>
        /// <param name="recipient">Contract address.</param>
        /// <exception cref="TransactionCollisionException">Thrown when the address is already in use</exception>
        private void PrepareContractAccount(Address recipient)
        {
            if (_stateProvider.AccountExists(recipient))
            {
                bool addressHasCode = (_virtualMachine.GetCachedCodeInfo(recipient)?.MachineCode?.Length ?? 0) != 0;
                bool addressWasUsed = _stateProvider.GetNonce(recipient) != 0;
                if (addressHasCode || addressWasUsed)
                {
                    if (_logger.IsEnabled(LogEventLevel.Verbose))
                    {
                        _logger.Verbose($"Contract collision at {recipient}");
                    }

                    throw new TransactionCollisionException();
                }

                _stateProvider.UpdateStorageRoot(recipient, Keccak.EmptyTreeHash);
            }
        }

        private void InitEntryExecution(ExecutionEnvironment env, ulong gasLimit, IReleaseSpec spec, ITxTracer txTracer)
        {
            // first we increment nonce on the executing account
            _stateProvider.IncrementNonce(env.Sender);

            // then we subtract money from the sender's account to pay for gas - this will be paid
            // even if the entry execution fails
            _stateProvider.SubtractFromBalance(env.Sender, gasLimit * env.GasPrice, spec);

            // we commit the nonce and gas payment
            _stateProvider.Commit(_specProvider.GetSpec(env.CurrentBlock.Number), txTracer.IsTracingState ? txTracer : null);
        }

        /// <summary>
        /// Refunds are issued as a result of calling specific <see cref="VirtualMachine"/> opcodes like SSTORE or
        /// SELFDESTRUCT. 
        /// </summary>
        /// <param name="gasLimit">Gas limit of the entry needed here because
        /// (<paramref name="gasLimit"/> - <paramref name="unspentGas"/>) / 2 is a hard cap for the gas refund.</param>
        /// <param name="unspentGas">Unspent gas of the entry needed here because
        /// (<paramref name="gasLimit"/> - <paramref name="unspentGas"/>) / 2 is a hard cap for the gas refund.</param>
        /// <param name="substate">Substate of the transaction before the refunds are issued.</param>
        /// <param name="env">Details of the execution environment.</param>
        /// <param name="spec">Provides the refund logic version details.</param>
        /// <returns>Returns spent gas after the refund.</returns>
        private ulong Refund(ulong gasLimit,
            ulong unspentGas,
            TransactionSubstate substate,
            ExecutionEnvironment env,
            IReleaseSpec spec)
        {
            ulong spentGas = gasLimit;
            if (!substate.IsError)
            {
                spentGas -= unspentGas;
                ulong refund = substate.ShouldRevert
                    ? 0
                    : Math.Min(spentGas / 2UL, (ulong) (substate.Refund + substate.DestroyList.Count * RefundOf.Destroy));

                if (_logger.IsEnabled(LogEventLevel.Verbose))
                {
                    _logger.Verbose("Refunding unused gas of {unspent} and refund of {refund}", unspentGas, refund);
                }

                UInt256 refundValue = (unspentGas + refund) * env.GasPrice;
                _stateProvider.AddToBalance(env.Sender, refundValue, spec);
                spentGas -= refund;
            }

            return spentGas;
        }
    }
}
