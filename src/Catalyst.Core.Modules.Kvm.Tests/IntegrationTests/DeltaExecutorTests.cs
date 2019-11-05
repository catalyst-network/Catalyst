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

using Catalyst.Abstractions.Cryptography;
using Catalyst.Core.Modules.Cryptography.BulletProofs;
using Catalyst.Protocol.Deltas;
using Catalyst.TestUtils;
using Google.Protobuf;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Core.Extensions;
using Nethermind.Evm;
using Nethermind.Evm.Tracing;
using Nethermind.Logging;
using Nethermind.Store;
using NSubstitute;
using Serilog.Events;
using Xunit;
using ILogger = Serilog.ILogger;

namespace Catalyst.Core.Modules.Kvm.Tests.IntegrationTests
{
    public sealed class DeltaExecutorTests
    {
        private readonly ICryptoContext _cryptoContext = new FfiWrapper();
        private readonly CatalystSpecProvider _specProvider;
        private readonly StateProvider _stateProvider;
        private readonly StorageProvider _storageProvider;
        private readonly VirtualMachine _virtualMachine;
        private readonly IPublicKey _recipient;
        private readonly IPublicKey _sender;
        private readonly IPublicKey _poorSender;
        private readonly DeltaExecutor _executor;

        public DeltaExecutorTests()
        {
            _specProvider = new CatalystSpecProvider();
            _stateProvider = new StateProvider(new StateDb(), new StateDb(), LimboLogs.Instance);
            _storageProvider = new StorageProvider(new StateDb(), _stateProvider, LimboLogs.Instance);
            _virtualMachine = new VirtualMachine(_stateProvider, _storageProvider, new StateUpdateHashProvider(), _specProvider, LimboLogs.Instance);
            var logger = Substitute.For<ILogger>();
            logger.IsEnabled(Arg.Any<LogEventLevel>()).Returns(true);
            
            _recipient = _cryptoContext.GeneratePrivateKey().GetPublicKey();
            _sender = _cryptoContext.GeneratePrivateKey().GetPublicKey();
            _poorSender = _cryptoContext.GeneratePrivateKey().GetPublicKey();
            
            _stateProvider.CreateAccount(_poorSender.ToKvmAddress(), 0.Kat());
            _stateProvider.CreateAccount(_sender.ToKvmAddress(), 1000.Kat());
            _stateProvider.CreateAccount(Address.Zero, 1000.Kat());
            _stateProvider.Commit(_specProvider.GenesisSpec);
            
            _executor = new DeltaExecutor(_specProvider, _stateProvider, _storageProvider, _virtualMachine, logger);
        }
        
        [Fact]
        public void Fails_when_sender_not_specified()
        {
            Delta delta = EntryUtils.PrepareSingleContractEntryDelta(_recipient, _sender, 3);
            delta.ContractEntries[0].Base.SenderPublicKey = ByteString.Empty;

            ITxTracer tracer = Substitute.For<ITxTracer>();
            tracer.IsTracingReceipt.Returns(true);
            
            _executor.Execute(delta, tracer);
            
            tracer.Received().MarkAsFailed(_recipient.ToKvmAddress(), 21000L, Bytes.Empty, "invalid");
        }
        
        [Fact]
        public void Fails_when_gas_limit_below_data_intrinsic_cost()
        {
            Delta delta = EntryUtils.PrepareSingleContractEntryDelta(_recipient, _sender, 0, "0x0102");
            delta.ContractEntries[0].GasLimit = 21001; // just above 21000 but not paying for data

            ITxTracer tracer = Substitute.For<ITxTracer>();
            tracer.IsTracingReceipt.Returns(true);
            
            _executor.Execute(delta, tracer);
            
            tracer.Received().MarkAsFailed(_recipient.ToKvmAddress(), 21001, Bytes.Empty, "invalid");
        }
        
        [Fact]
        public void Fails_when_gas_limit_below_entry_intrinsic_cost()
        {
            Delta delta = EntryUtils.PrepareSingleContractEntryDelta(_recipient, _sender, 0);
            delta.ContractEntries[0].GasLimit = 20999; // just below 21000

            ITxTracer tracer = Substitute.For<ITxTracer>();
            tracer.IsTracingReceipt.Returns(true);
            
            _executor.Execute(delta, tracer);
            
            tracer.Received().MarkAsFailed(_recipient.ToKvmAddress(), 20999, Bytes.Empty, "invalid");
        }
        
        [Fact]
        public void Fails_on_wrong_nonce()
        {
            Delta delta = EntryUtils.PrepareSingleContractEntryDelta(_recipient, _sender, 0, "0x", 1);

            ITxTracer tracer = Substitute.For<ITxTracer>();
            tracer.IsTracingReceipt.Returns(true);
            
            _executor.Execute(delta, tracer);
            
            tracer.Received().MarkAsFailed(_recipient.ToKvmAddress(), 21000, Bytes.Empty, "invalid");
        }
        
        [Fact]
        public void Fails_on_insufficient_balance()
        {
            Delta delta = EntryUtils.PrepareSingleContractEntryDelta(_recipient, _poorSender, 0);
            
            // when gas price is non-zero then sender needs to have a non-zero balance to pay for the gas cost
            delta.ContractEntries[0].GasPrice = 1;

            ITxTracer tracer = Substitute.For<ITxTracer>();
            tracer.IsTracingReceipt.Returns(true);
            
            _executor.Execute(delta, tracer);
            
            tracer.Received().MarkAsFailed(_recipient.ToKvmAddress(), 21000, Bytes.Empty, "invalid");
        }
        
        [Fact]
        public void Fails_when_tx_beyond_delta_gas_limit()
        {
            Delta delta = EntryUtils.PrepareSingleContractEntryDelta(_recipient, _sender, 0);
            delta.ContractEntries[0].GasLimit = 10_000_000;

            ITxTracer tracer = Substitute.For<ITxTracer>();
            tracer.IsTracingReceipt.Returns(true);
            
            _executor.Execute(delta, tracer);
            
            tracer.Received().MarkAsFailed(_recipient.ToKvmAddress(), 10_000_000, Bytes.Empty, "invalid");
        }
        
        [Fact]
        public void Can_deploy_code()
        {
            Delta delta = EntryUtils.PrepareSingleContractEntryDelta(null, _sender, 0, "0x60016000526001601FF300");
            delta.ContractEntries[0].GasLimit = 1_000_000L;
            delta.ContractEntries[0].Base.ReceiverPublicKey = ByteString.Empty;

            ITxTracer tracer = Substitute.For<ITxTracer>();
            tracer.IsTracingReceipt.Returns(true);
            
            _executor.Execute(delta, tracer);
            
            var contractAddress = Address.OfContract(_sender.ToKvmAddress(), 0);
            tracer.Received().MarkAsSuccess(contractAddress, 53370, Arg.Any<byte[]>(), Arg.Any<LogEntry[]>());
        }
        
        [Fact]
        public void Can_self_destruct()
        {
            Delta delta = EntryUtils.PrepareSingleContractEntryDelta(null, _sender, 0, "0x730001020304050607080910111213141516171819ff");
            delta.ContractEntries[0].GasLimit = 1_000_000L;
            delta.ContractEntries[0].Base.ReceiverPublicKey = ByteString.Empty;

            ITxTracer tracer = Substitute.For<ITxTracer>();
            tracer.IsTracingReceipt.Returns(true);
            
            _executor.Execute(delta, tracer);
            
            var contractAddress = Address.OfContract(_sender.ToKvmAddress(), 0);
            tracer.Received().MarkAsSuccess(contractAddress, 34343, Arg.Any<byte[]>(), Arg.Any<LogEntry[]>());
        }
        
        [Fact]
        public void Fails_when_not_enough_gas_for_code_deposit()
        {
            Delta delta = EntryUtils.PrepareSingleContractEntryDelta(null, _sender, 0, "0x60016000526001601FF300");
            delta.ContractEntries[0].GasLimit = 53369;
            delta.ContractEntries[0].Base.ReceiverPublicKey = ByteString.Empty;

            ITxTracer tracer = Substitute.For<ITxTracer>();
            tracer.IsTracingReceipt.Returns(true);
            
            _executor.Execute(delta, tracer);
            
            var contractAddress = Address.OfContract(_sender.ToKvmAddress(), 0);
            tracer.Received().MarkAsFailed(contractAddress, 53369, Arg.Any<byte[]>(), null);
        }
        
        [Fact]
        public void Throws_on_theoretical_contract_crash()
        {
            var contractAddress = Address.OfContract(_sender.ToKvmAddress(), 0);
            _stateProvider.CreateAccount(contractAddress, 1000.Kat());
            Keccak codeHash = _stateProvider.UpdateCode(Bytes.FromHexString("0x01"));
            _stateProvider.UpdateCodeHash(contractAddress, codeHash, _specProvider.GenesisSpec);
            _stateProvider.Commit(_specProvider.GenesisSpec);

            Delta delta = EntryUtils.PrepareSingleContractEntryDelta(null, _sender, 0, "0x60016000526001601FF300");
            delta.ContractEntries[0].GasLimit = 1_000_000L;
            delta.ContractEntries[0].Base.ReceiverPublicKey = ByteString.Empty;

            ITxTracer tracer = Substitute.For<ITxTracer>();
            tracer.IsTracingReceipt.Returns(true);
            
            _executor.Execute(delta, tracer);

            tracer.Received().MarkAsFailed(contractAddress, 1_000_000L, Arg.Any<byte[]>(), null);
        }
        
        [Fact]
        public void Update_storage_root_on_contract_clash()
        {
            var contractAddress = Address.OfContract(_sender.ToKvmAddress(), 0);
            _stateProvider.CreateAccount(contractAddress, 1000.Kat());
            _stateProvider.Commit(_specProvider.GenesisSpec);

            Delta delta = EntryUtils.PrepareSingleContractEntryDelta(null, _sender, 0, "0x60016000526001601FF300");
            delta.ContractEntries[0].GasLimit = 1_000_000L;
            delta.ContractEntries[0].Base.ReceiverPublicKey = ByteString.Empty;

            ITxTracer tracer = Substitute.For<ITxTracer>();
            tracer.IsTracingReceipt.Returns(true);
            
            _executor.Execute(delta, tracer);

            tracer.Received().MarkAsSuccess(contractAddress, 53370, Arg.Any<byte[]>(), Arg.Any<LogEntry[]>());
        }
        
        [Fact]
        public void Can_deploy_code_read_only()
        {
            Delta delta = EntryUtils.PrepareSingleContractEntryDelta(null, _sender, 0, "0x60016000526001601FF300");
            delta.ContractEntries[0].GasLimit = 1_000_000L;
            delta.ContractEntries[0].Base.ReceiverPublicKey = ByteString.Empty;

            ITxTracer tracer = Substitute.For<ITxTracer>();
            tracer.IsTracingReceipt.Returns(true);
            
            _executor.CallAndRestore(delta, tracer);

            var contractAddress = Address.OfContract(_sender.ToKvmAddress(), 0);
            tracer.Received().MarkAsSuccess(contractAddress, 53370, Arg.Any<byte[]>(), Arg.Any<LogEntry[]>());
        }
        
        [Fact]
        public void Does_not_crash_on_kvm_error()
        {
            // here we test a case when we deploy a contract where constructor throws invalid opcode EVM error
            // 0xfe is a bad opcode that immediately causes an EVM error
            Delta delta = EntryUtils.PrepareSingleContractEntryDelta(null, _sender, 0, "0xfe");
            delta.ContractEntries[0].GasLimit = 1_000_000L;

            ITxTracer tracer = Substitute.For<ITxTracer>();
            tracer.IsTracingReceipt.Returns(true);
            
            _executor.Execute(delta, tracer);
            
            var contractAddress = Address.OfContract(_sender.ToKvmAddress(), 0);
            tracer.Received().MarkAsFailed(contractAddress, 1_000_000L, Arg.Any<byte[]>(), "Error");
        }

        [Fact]
        public void Does_not_crash_on_kvm_exception()
        {
            // here we test a case when we deploy a contract where constructor throws StackUnderflowException
            // 0x01 is the ADD opcode which requires two items on the stack and stack is empty here
            // added here for full coverage as the errors (EVM) are handled differently in some cases (via .NET exceptions)
            Delta delta = EntryUtils.PrepareSingleContractEntryDelta(null, _sender, 0, "0x01");
            delta.ContractEntries[0].GasLimit = 1_000_000L;

            ITxTracer tracer = Substitute.For<ITxTracer>();
            tracer.IsTracingReceipt.Returns(true);
            
            _executor.Execute(delta, tracer);
            
            var contractAddress = Address.OfContract(_sender.ToKvmAddress(), 0);
            tracer.Received().MarkAsFailed(contractAddress, 1_000_000L, Arg.Any<byte[]>(), "Error");
        }
        
        [Fact]
        public void Can_execute_transfers_from_public_entries()
        {
            Delta delta = EntryUtils.PrepareSinglePublicEntryDelta(_recipient, _sender, 0);

            ITxTracer tracer = Substitute.For<ITxTracer>();
            tracer.IsTracingReceipt.Returns(true);
            
            _executor.Execute(delta, tracer);
            
            tracer.Received().MarkAsSuccess(_recipient.ToKvmAddress(), 21000, Bytes.Empty, Arg.Any<LogEntry[]>());
        }
        
        [Fact]
        public void Can_add_gas_to_existing_balance()
        {
            Delta delta = EntryUtils.PrepareSingleContractEntryDelta(_recipient, _sender, 0);
            delta.ContractEntries[0].GasPrice = 1;

            ITxTracer tracer = Substitute.For<ITxTracer>();
            tracer.IsTracingReceipt.Returns(true);
            
            _executor.Execute(delta, tracer);
            
            tracer.Received().MarkAsSuccess(_recipient.ToKvmAddress(), 21000, Bytes.Empty, Arg.Any<LogEntry[]>());
        }
    }
}
