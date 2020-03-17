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
using System.IO;
using System.Linq;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.Kvm;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.Transaction;
using Google.Protobuf;
using Nethermind.Core;
using Nethermind.Core.Attributes;
using Nethermind.Core.Crypto;
using Nethermind.Core.Extensions;
using Nethermind.Core.Specs;
using Nethermind.Dirichlet.Numerics;
using Nethermind.Evm;
using Nethermind.Evm.Precompiles;
using Nethermind.Evm.Tracing;
using Nethermind.State;
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
    public sealed class DeltaExecutor : IDeltaExecutor
    {
        private readonly ICryptoContext _cryptoContext;
        private readonly ILogger _logger;
        private readonly ISpecProvider _specProvider;
        private readonly IStateProvider _stateProvider;
        private readonly IStorageProvider _storageProvider;
        private readonly IKvm _virtualMachine;

        /// <summary>
        ///     Note that there is a distinct approach to state and storage even as only together they form the 'state'
        ///     in the business sense. <see cref="IStorageProvider" /> needs to handle storage trees for various accounts
        ///     while <see cref="IStateProvider" /> handles the basic accounts state with storage roots only.
        /// </summary>
        /// <param name="specProvider">The network upgrade spec - defines the virtual machine version.</param>
        /// <param name="stateProvider">Access to accounts.</param>
        /// <param name="storageProvider">Access to accounts' storage.</param>
        /// <param name="virtualMachine">A virtual machine to execute the code on.</param>
        /// <param name="cryptoContext">Support for crypto operations.</param>
        /// <param name="logger">Logger for the execution details.</param>
        public DeltaExecutor(ISpecProvider specProvider,
            IStateProvider stateProvider,
            IStorageProvider storageProvider,
            IKvm virtualMachine,
            ICryptoContext cryptoContext,
            ILogger logger)
        {
            _logger = logger;
            _specProvider = specProvider;
            _virtualMachine = virtualMachine;
            _cryptoContext = cryptoContext;
            _stateProvider = stateProvider;
            _storageProvider = storageProvider;
        }

        [Todo("Wider work needed to split calls and execution properly")]
        public void CallAndReset(Delta stateUpdate, ITxTracer txTracer) { Execute(stateUpdate, txTracer, true); }

        [Todo(
            "After delta is executed we should validate the state root and if it is not as expected we should revert all the changes.")]
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
        public static ulong CalculateIntrinsicGas(PublicEntry entry, IReleaseSpec releaseSpec)
        {
            ulong result = GasCostOf.Transaction; // the basic entry cost
            if (entry.Data != null)
            {
                // here is the difference between the 0 bytes and non-zero bytes cost
                // justified by a better compression level of zero bytes
                var txDataNonZeroGasCost = releaseSpec.IsEip2028Enabled
                    ? GasCostOf.TxDataNonZeroEip2028
                    : GasCostOf.TxDataNonZero;
                var length = entry.Data.Length;
                for (var i = 0; i < length; i++)
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
            var result = new StateUpdate
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

        private static void QuickFail(PublicEntry entry, ExecutionEnvironment env, ITxTracer txTracer)
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
            var stateUpdate = ToStateUpdate(delta);

            // revert state if any fails (take snapshot)
            foreach (var publicEntry in delta.PublicEntries)
            {
                Execute(publicEntry, stateUpdate, txTracer);
            }
            
            var spec = _specProvider.GetSpec(stateUpdate.Number);
            _storageProvider.Commit(txTracer.IsTracingState ? txTracer : null);
            _stateProvider.Commit(spec, txTracer.IsTracingState ? txTracer : null);
            
            if (!readOnly)
            {
                // IMPORTANT: if this line is removed then the trie state root will not be recalculated before saving in the DB
                // bad design - to change
                Keccak newStateRoot = _stateProvider.StateRoot;
                if (new Keccak(delta.StateRoot.ToByteArray()) != _stateProvider.StateRoot)
                {
                    if (_logger.IsEnabled(LogEventLevel.Error)) _logger.Error("Invalid delta state root - found {found} and should be {shouldBe}", _stateProvider.StateRoot, new Keccak(delta.StateRoot.ToByteArray()));
                }
                
                // compare state roots
                _storageProvider.CommitTrees();
                _stateProvider.CommitTree();
            }
            else
            {
                delta.StateRoot = _stateProvider.StateRoot.ToByteString(); 
                if (_logger.IsEnabled(LogEventLevel.Debug)) _logger.Debug($"Setting candidate delta {delta.DeltaNumber} root to {delta.StateRoot.ToKeccak()}");
                _stateProvider.Reset();
                _storageProvider.Reset();
            }
        }

        /// <summary>
        ///     @TODO we need to put this in some global logging system.
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="reason"></param>
        private void TraceLogInvalidTx(PublicEntry entry, string reason)
        {
            if (_logger.IsEnabled(LogEventLevel.Verbose))
            {
                _logger.Verbose("Invalid entry {entry} ({reason})", entry, reason);
            }
        }

        /// <summary>
        ///     Executes the <paramref name="entry" />.
        /// </summary>
        /// <param name="entry">Transaction entry to be executed inside the <see cref="VirtualMachine" />.</param>
        /// <param name="stateUpdate"><see cref="Delta" /> to be used for execution environment construction</param>
        /// <param name="txTracer">Tracer to extract the execution steps for debugging or analytics.</param>
        /// <param name="readOnly">Defines whether the state should be reverted after the execution.</param>
        /// <exception cref="TransactionCollisionException">Thrown when deployment address already has some code.</exception>
        /// <exception cref="OutOfGasException">Thrown when not enough gas is available for deposit.</exception>
        private void Execute(PublicEntry entry, StateUpdate stateUpdate, ITxTracer txTracer)
        {
            var spec = _specProvider.GetSpec(stateUpdate.Number);

            var (sender, recipient) = ExtractSenderAndRecipient(entry);
            var isPrecompile = recipient.IsPrecompiled(spec);
            var env = PrepareEnv(entry, sender, recipient, stateUpdate, isPrecompile);

            var gasLimit = entry.GasLimit;
            var intrinsicGas = CalculateIntrinsicGas(entry, spec);

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
            var unspentGas = gasLimit - intrinsicGas;
            var spentGas = gasLimit;

            // the snapshots are needed to revert the subroutine state changes in case of an VM exception
            var stateSnapshot = _stateProvider.TakeSnapshot();
            var storageSnapshot = _storageProvider.TakeSnapshot();

            // we subtract value from sender
            // it will be added to recipient at the later stage (inside the VM)
            _stateProvider.SubtractFromBalance(sender, env.Value, spec);

            // we fail unless we succeed
            var statusCode = StatusCode.Failure;
            TransactionSubstate substate = null;

            try
            {
                if (entry.IsValidDeploymentEntry) PrepareContractAccount(env.CodeSource);

                var executionType = entry.IsValidDeploymentEntry ? ExecutionType.Create : ExecutionType.Call;
                using (var state = new VmState((long) unspentGas, env, executionType, isPrecompile, true, false))
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

            var gasBeneficiary = stateUpdate.GasBeneficiary;
            var wasBeneficiaryAccountDestroyed = statusCode != StatusCode.Failure
             && (substate?.DestroyList.Contains(gasBeneficiary) ?? false);
            if (!wasBeneficiaryAccountDestroyed)
            {
                if (!_stateProvider.AccountExists(gasBeneficiary))
                {
                    _stateProvider.CreateAccount(gasBeneficiary, spentGas * env.GasPrice);
                }
                else
                {
                    _stateProvider.AddToBalance(gasBeneficiary, spentGas * env.GasPrice, spec);
                }
            }

            if (txTracer.IsTracingReceipt)
            {
                if (statusCode == StatusCode.Failure)
                {
                    txTracer.MarkAsFailed(env.CodeSource, (long) spentGas,
                        substate?.ShouldRevert ?? false ? substate.Output : Bytes.Empty, substate?.Error);
                }
                else
                {
                    if (substate == null)
                    {
                        throw new InvalidOperationException("Substate should not be null after a successful VM run.");
                    }

                    txTracer.MarkAsSuccess(env.CodeSource, (long) spentGas, substate.Output,
                        substate.Logs.Any() ? substate.Logs.ToArray() : LogEntry.EmptyLogs);
                }
            }
        }

        /// <summary>
        ///     Accounts for which SELF_DESTRUCT opcode was invoked during the tx execution.
        ///     Note that one of them can be coinbase / validator and in such case the miner / validator rewards are lost.
        /// </summary>
        /// <param name="substate"></param>
        private void DestroyAccounts(TransactionSubstate substate)
        {
            foreach (var toBeDestroyed in substate.DestroyList)
            {
                if (_logger.IsEnabled(LogEventLevel.Verbose))
                {
                    _logger.Verbose($"Destroying account {toBeDestroyed}");
                }

                _stateProvider.DeleteAccount(toBeDestroyed);
            }
        }

        private void DeployCode(ExecutionEnvironment env,
            TransactionSubstate substate,
            ref ulong unspentGas,
            IReleaseSpec spec)
        {
            var codeDepositGasCost = (ulong) CodeDepositHandler.CalculateCost(substate.Output.Length, spec);
            if (unspentGas < codeDepositGasCost && spec.IsEip2Enabled)
            {
                throw new OutOfGasException();
            }

            if (unspentGas < codeDepositGasCost)
            {
                return;
            }

            var codeHash = _stateProvider.UpdateCode(substate.Output);
            _stateProvider.UpdateCodeHash(env.CodeSource, codeHash, spec);
            unspentGas -= codeDepositGasCost;
        }

        private (Address sender, Address recipient) ExtractSenderAndRecipient(PublicEntry entry)
        {
            var sender = entry.SenderAddress.ToAddress();
            var recipient = entry.ReceiverAddress.ToAddress();
            if (entry.IsValidDeploymentEntry)
            {
                recipient = ContractAddress.From(sender, _stateProvider.GetNonce(sender));
            }

            return (sender, recipient);
        }

        private ExecutionEnvironment PrepareEnv(PublicEntry entry,
            Address sender,
            Address recipient,
            StateUpdate stateUpdate,
            bool isPrecompile)
        {
            var value = entry.Amount.ToUInt256();
            var machineCode = entry.IsValidDeploymentEntry ? entry.Data.ToByteArray() : null;
            var data = entry.IsValidDeploymentEntry ? null : entry.Data.ToByteArray();

            var env = new ExecutionEnvironment
            {
                Value = value,
                TransferValue = value,
                Sender = sender,
                CodeSource = recipient,
                ExecutingAccount = recipient,
                CurrentBlock = stateUpdate,
                GasPrice = entry.GasPrice.ToUInt256(),
                InputData = data ?? new byte[0],
                CodeInfo = isPrecompile
                    ? new CodeInfo(recipient)
                    : machineCode == null
                        ? _virtualMachine.GetCachedCodeInfo(recipient)
                        : new CodeInfo(machineCode),
                Originator = sender
            };
            return env;
        }

        private bool ValidateDeltaGasLimit(PublicEntry entry, ExecutionEnvironment env, ITxTracer txTracer)
        {
            if (entry.GasLimit <= (ulong) (env.CurrentBlock.GasLimit - env.CurrentBlock.GasUsed))
            {
                return true;
            }

            TraceLogInvalidTx(entry,
                $"BLOCK_GAS_LIMIT_EXCEEDED {entry.GasLimit.ToString()} > {env.CurrentBlock.GasLimit.ToString()} - {env.CurrentBlock.GasUsed.ToString()}");
            QuickFail(entry, env, txTracer);
            return false;
        }

        private bool ValidateIntrinsicGas(PublicEntry entry,
            ExecutionEnvironment env,
            ulong intrinsicGas,
            ITxTracer txTracer)
        {
            if (_logger.IsEnabled(LogEventLevel.Verbose))
            {
                _logger.Verbose("Intrinsic gas calculated for {entry}: {intrinsicGas}", entry, intrinsicGas);
            }

            if (entry.GasLimit >= intrinsicGas)
            {
                return true;
            }

            TraceLogInvalidTx(entry,
                $"GAS_LIMIT_BELOW_INTRINSIC_GAS {entry.GasLimit.ToString()} < {intrinsicGas.ToString()}");
            QuickFail(entry, env, txTracer);
            return false;
        }

        private bool ValidateSender(PublicEntry entry, ExecutionEnvironment env, ITxTracer txTracer)
        {
            if (env.Sender != null)
            {
                return true;
            }

            TraceLogInvalidTx(entry, "SENDER_NOT_SPECIFIED");
            QuickFail(entry, env, txTracer);
            return false;
        }

        private bool ValidateNonce(PublicEntry entry, ExecutionEnvironment env, ITxTracer txTracer)
        {
            if (entry.Nonce == _stateProvider.GetNonce(env.Sender))
            {
                return true;
            }

            TraceLogInvalidTx(entry,
                $"WRONG_TRANSACTION_NONCE: {entry.Nonce.ToString()} (expected {_stateProvider.GetNonce(env.Sender).ToString()})");
            QuickFail(entry, env, txTracer);
            return false;
        }

        private bool ValidateSenderBalance(PublicEntry entry,
            ExecutionEnvironment env,
            ulong intrinsicGas,
            ITxTracer txTracer)
        {
            var senderBalance = _stateProvider.GetBalance(env.Sender);
            if (intrinsicGas * env.GasPrice + env.Value <= senderBalance)
            {
                return true;
            }

            TraceLogInvalidTx(entry,
                $"INSUFFICIENT_SENDER_BALANCE: ({env.Sender})_BALANCE = {senderBalance.ToString()}");
            QuickFail(entry, env, txTracer);
            return false;
        }

        /// <summary>
        ///     Prepares and validates the account address for contract deployment.
        /// </summary>
        /// <param name="recipient">Contract address.</param>
        /// <exception cref="TransactionCollisionException">Thrown when the address is already in use</exception>
        private void PrepareContractAccount(Address recipient)
        {
            if (!_stateProvider.AccountExists(recipient)) return;

            var addressHasCode = (_virtualMachine.GetCachedCodeInfo(recipient)?.MachineCode?.Length ?? 0) != 0;
            var addressWasUsed = _stateProvider.GetNonce(recipient) != 0;
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

        private void InitEntryExecution(ExecutionEnvironment env, ulong gasLimit, IReleaseSpec spec, ITxTracer txTracer)
        {
            // first we increment nonce on the executing account
            _stateProvider.IncrementNonce(env.Sender);

            // then we subtract money from the sender's account to pay for gas - this will be paid
            // even if the entry execution fails
            _stateProvider.SubtractFromBalance(env.Sender, gasLimit * env.GasPrice, spec);

            // we commit the nonce and gas payment
            _stateProvider.Commit(_specProvider.GetSpec(env.CurrentBlock.Number),
                txTracer.IsTracingState ? txTracer : null);
        }

        /// <summary>
        ///     Refunds are issued as a result of calling specific <see cref="VirtualMachine" /> opcodes like SSTORE or
        ///     SELFDESTRUCT.
        /// </summary>
        /// <param name="gasLimit">
        ///     Gas limit of the entry needed here because
        ///     (<paramref name="gasLimit" /> - <paramref name="unspentGas" />) / 2 is a hard cap for the gas refund.
        /// </param>
        /// <param name="unspentGas">
        ///     Unspent gas of the entry needed here because
        ///     (<paramref name="gasLimit" /> - <paramref name="unspentGas" />) / 2 is a hard cap for the gas refund.
        /// </param>
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
            var spentGas = gasLimit;
            if (substate.IsError)
            {
                return spentGas;
            }

            spentGas -= unspentGas;
            var refund = substate.ShouldRevert
                ? 0
                : Math.Min(spentGas / 2UL, (ulong) (substate.Refund + substate.DestroyList.Count * RefundOf.Destroy));

            if (_logger.IsEnabled(LogEventLevel.Verbose))
            {
                _logger.Verbose("Refunding unused gas of {unspent} and refund of {refund}", unspentGas, refund);
            }

            var refundValue = (unspentGas + refund) * env.GasPrice;
            _stateProvider.AddToBalance(env.Sender, refundValue, spec);
            spentGas -= refund;

            return spentGas;
        }
    }
}
