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
        
        [Fact]
        public void Fails_when_sender_not_specified()
        {
            DeltaExecutor executor = PrepareExecutorForTest();
            
            var recipient = _cryptoContext.GeneratePrivateKey().GetPublicKey();
            var sender = _cryptoContext.GeneratePrivateKey().GetPublicKey();
            Delta delta = EntryUtils.PrepareSingleContractEntryDelta(recipient, sender, 3);
            delta.ContractEntries[0].Base.SenderPublicKey = ByteString.Empty;

            ITxTracer tracer = Substitute.For<ITxTracer>();
            tracer.IsTracingReceipt.Returns(true);
            
            executor.Execute(delta, tracer);
            
            tracer.Received().MarkAsFailed(recipient.ToKvmAddress(), 21000L, Bytes.Empty, "invalid");
        }
        
        [Fact]
        public void Fails_when_gas_limit_below_data_intrinsic_cost()
        {
            DeltaExecutor executor = PrepareExecutorForTest();
            
            var recipient = _cryptoContext.GeneratePrivateKey().GetPublicKey();
            var sender = _cryptoContext.GeneratePrivateKey().GetPublicKey();
            Delta delta = EntryUtils.PrepareSingleContractEntryDelta(recipient, sender, 0, "0x0102");
            delta.ContractEntries[0].GasLimit = 21001; // just above 21000 but not paying for data

            ITxTracer tracer = Substitute.For<ITxTracer>();
            tracer.IsTracingReceipt.Returns(true);
            
            executor.Execute(delta, tracer);
            
            tracer.Received().MarkAsFailed(recipient.ToKvmAddress(), 21001, Bytes.Empty, "invalid");
        }
        
        [Fact]
        public void Fails_when_gas_limit_below_entry_intrinsic_cost()
        {
            DeltaExecutor executor = PrepareExecutorForTest();
            
            var recipient = _cryptoContext.GeneratePrivateKey().GetPublicKey();
            var sender = _cryptoContext.GeneratePrivateKey().GetPublicKey();
            Delta delta = EntryUtils.PrepareSingleContractEntryDelta(recipient, sender, 0);
            delta.ContractEntries[0].GasLimit = 20999; // just below 21000

            ITxTracer tracer = Substitute.For<ITxTracer>();
            tracer.IsTracingReceipt.Returns(true);
            
            executor.Execute(delta, tracer);
            
            tracer.Received().MarkAsFailed(recipient.ToKvmAddress(), 20999, Bytes.Empty, "invalid");
        }
        
        [Fact]
        public void Fails_on_wrong_nonce()
        {
            DeltaExecutor executor = PrepareExecutorForTest();
            
            var recipient = _cryptoContext.GeneratePrivateKey().GetPublicKey();
            var sender = _cryptoContext.GeneratePrivateKey().GetPublicKey();
            Delta delta = EntryUtils.PrepareSingleContractEntryDelta(recipient, sender, 0, "0x", 1);

            ITxTracer tracer = Substitute.For<ITxTracer>();
            tracer.IsTracingReceipt.Returns(true);
            
            executor.Execute(delta, tracer);
            
            tracer.Received().MarkAsFailed(recipient.ToKvmAddress(), 21000, Bytes.Empty, "invalid");
        }
        
        [Fact]
        public void Fails_on_insufficient_balance()
        {
            DeltaExecutor executor = PrepareExecutorForTest();
            
            var recipient = _cryptoContext.GeneratePrivateKey().GetPublicKey();
            var sender = _cryptoContext.GeneratePrivateKey().GetPublicKey();
            Delta delta = EntryUtils.PrepareSingleContractEntryDelta(recipient, sender, 0);
            delta.ContractEntries[0].GasPrice = 1;

            ITxTracer tracer = Substitute.For<ITxTracer>();
            tracer.IsTracingReceipt.Returns(true);
            
            executor.Execute(delta, tracer);
            
            tracer.Received().MarkAsFailed(recipient.ToKvmAddress(), 21000, Bytes.Empty, "invalid");
        }
        
        [Fact]
        public void Fails_when_tx_beyond_delta_gas_limit()
        {
            DeltaExecutor executor = PrepareExecutorForTest();
            
            var recipient = _cryptoContext.GeneratePrivateKey().GetPublicKey();
            var sender = _cryptoContext.GeneratePrivateKey().GetPublicKey();
            Delta delta = EntryUtils.PrepareSingleContractEntryDelta(recipient, sender, 0);
            delta.ContractEntries[0].GasLimit = 10_000_000;

            ITxTracer tracer = Substitute.For<ITxTracer>();
            tracer.IsTracingReceipt.Returns(true);
            
            executor.Execute(delta, tracer);
            
            tracer.Received().MarkAsFailed(recipient.ToKvmAddress(), 10_000_000, Bytes.Empty, "invalid");
        }
        
        [Fact]
        public void Can_deploy_code()
        {
            DeltaExecutor executor = PrepareExecutorForTest();
            
            var sender = _cryptoContext.GeneratePrivateKey().GetPublicKey();
            Delta delta = EntryUtils.PrepareSingleContractEntryDelta(null, sender, 0, "0x60016000526001601FF300");
            delta.ContractEntries[0].GasLimit = 1_000_000L;
            delta.ContractEntries[0].Base.ReceiverPublicKey = ByteString.Empty;

            ITxTracer tracer = Substitute.For<ITxTracer>();
            tracer.IsTracingReceipt.Returns(true);
            
            executor.Execute(delta, tracer);
            
            var contractAddress = Address.OfContract(sender.ToKvmAddress(), 0);
            tracer.Received().MarkAsSuccess(contractAddress, 53370, Arg.Any<byte[]>(), Arg.Any<LogEntry[]>());
        }
        
        [Fact]
        public void Can_self_destruct()
        {
            DeltaExecutor executor = PrepareExecutorForTest();
            
            var sender = _cryptoContext.GeneratePrivateKey().GetPublicKey();
            Delta delta = EntryUtils.PrepareSingleContractEntryDelta(null, sender, 0, "0x730001020304050607080910111213141516171819ff");
            delta.ContractEntries[0].GasLimit = 1_000_000L;
            delta.ContractEntries[0].Base.ReceiverPublicKey = ByteString.Empty;

            ITxTracer tracer = Substitute.For<ITxTracer>();
            tracer.IsTracingReceipt.Returns(true);
            
            executor.Execute(delta, tracer);
            
            var contractAddress = Address.OfContract(sender.ToKvmAddress(), 0);
            tracer.Received().MarkAsSuccess(contractAddress, 34343, Arg.Any<byte[]>(), Arg.Any<LogEntry[]>());
        }
        
        [Fact]
        public void Fails_when_not_enough_gas_for_code_deposit()
        {
            DeltaExecutor executor = PrepareExecutorForTest();
            
            var sender = _cryptoContext.GeneratePrivateKey().GetPublicKey();
            Delta delta = EntryUtils.PrepareSingleContractEntryDelta(null, sender, 0, "0x60016000526001601FF300");
            delta.ContractEntries[0].GasLimit = 53369;
            delta.ContractEntries[0].Base.ReceiverPublicKey = ByteString.Empty;

            ITxTracer tracer = Substitute.For<ITxTracer>();
            tracer.IsTracingReceipt.Returns(true);
            
            executor.Execute(delta, tracer);
            
            var contractAddress = Address.OfContract(sender.ToKvmAddress(), 0);
            tracer.Received().MarkAsFailed(contractAddress, 53369, Arg.Any<byte[]>(), null);
        }
        
        [Fact]
        public void Throws_on_theoretical_contract_crash()
        {
            CatalystSpecProvider specProvider = new CatalystSpecProvider();
            StateProvider stateProvider = new StateProvider(new StateDb(), new StateDb(), LimboLogs.Instance);
            StorageProvider storageProvider = new StorageProvider(new StateDb(), stateProvider, LimboLogs.Instance);
            VirtualMachine virtualMachine = new VirtualMachine(stateProvider, storageProvider, new StateUpdateHashProvider(), specProvider, LimboLogs.Instance);
            var logger = Substitute.For<ILogger>();
            logger.IsEnabled(Arg.Any<LogEventLevel>()).Returns(true);

            var sender = _cryptoContext.GeneratePrivateKey().GetPublicKey();
            var contractAddress = Address.OfContract(sender.ToKvmAddress(), 0);
            stateProvider.CreateAccount(contractAddress, 1000.Kat());
            Keccak codeHash = stateProvider.UpdateCode(Bytes.FromHexString("0x01"));
            stateProvider.UpdateCodeHash(contractAddress, codeHash, specProvider.GenesisSpec);
            stateProvider.Commit(specProvider.GenesisSpec);
            
            DeltaExecutor executor = new DeltaExecutor(specProvider, stateProvider, storageProvider, virtualMachine, logger);

            Delta delta = EntryUtils.PrepareSingleContractEntryDelta(null, sender, 0, "0x60016000526001601FF300");
            delta.ContractEntries[0].GasLimit = 1_000_000L;
            delta.ContractEntries[0].Base.ReceiverPublicKey = ByteString.Empty;

            ITxTracer tracer = Substitute.For<ITxTracer>();
            tracer.IsTracingReceipt.Returns(true);
            
            executor.Execute(delta, tracer);

            tracer.Received().MarkAsFailed(contractAddress, 1_000_000L, Arg.Any<byte[]>(), null);
        }
        
        [Fact]
        public void Update_storage_root_on_contract_clash()
        {
            CatalystSpecProvider specProvider = new CatalystSpecProvider();
            StateProvider stateProvider = new StateProvider(new StateDb(), new StateDb(), LimboLogs.Instance);
            StorageProvider storageProvider = new StorageProvider(new StateDb(), stateProvider, LimboLogs.Instance);
            VirtualMachine virtualMachine = new VirtualMachine(stateProvider, storageProvider, new StateUpdateHashProvider(), specProvider, LimboLogs.Instance);
            var logger = Substitute.For<ILogger>();
            logger.IsEnabled(Arg.Any<LogEventLevel>()).Returns(true);

            var sender = _cryptoContext.GeneratePrivateKey().GetPublicKey();
            var contractAddress = Address.OfContract(sender.ToKvmAddress(), 0);
            stateProvider.CreateAccount(contractAddress, 1000.Kat());
            stateProvider.Commit(specProvider.GenesisSpec);
            
            DeltaExecutor executor = new DeltaExecutor(specProvider, stateProvider, storageProvider, virtualMachine, logger);

            Delta delta = EntryUtils.PrepareSingleContractEntryDelta(null, sender, 0, "0x60016000526001601FF300");
            delta.ContractEntries[0].GasLimit = 1_000_000L;
            delta.ContractEntries[0].Base.ReceiverPublicKey = ByteString.Empty;

            ITxTracer tracer = Substitute.For<ITxTracer>();
            tracer.IsTracingReceipt.Returns(true);
            
            executor.Execute(delta, tracer);

            tracer.Received().MarkAsSuccess(contractAddress, 53370, Arg.Any<byte[]>(), Arg.Any<LogEntry[]>());
        }
        
        [Fact]
        public void Can_deploy_code_read_only()
        {
            DeltaExecutor executor = PrepareExecutorForTest();
            
            var sender = _cryptoContext.GeneratePrivateKey().GetPublicKey();
            Delta delta = EntryUtils.PrepareSingleContractEntryDelta(null, sender, 0, "0x60016000526001601FF300");
            delta.ContractEntries[0].GasLimit = 1_000_000L;
            delta.ContractEntries[0].Base.ReceiverPublicKey = ByteString.Empty;

            ITxTracer tracer = Substitute.For<ITxTracer>();
            tracer.IsTracingReceipt.Returns(true);
            
            executor.CallAndRestore(delta, tracer);

            var contractAddress = Address.OfContract(sender.ToKvmAddress(), 0);
            tracer.Received().MarkAsSuccess(contractAddress, 53370, Arg.Any<byte[]>(), Arg.Any<LogEntry[]>());
        }
        
        [Fact]
        public void Does_not_crash_on_kvm_error()
        {
            DeltaExecutor executor = PrepareExecutorForTest();

            var sender = _cryptoContext.GeneratePrivateKey().GetPublicKey();
            Delta delta = EntryUtils.PrepareSingleContractEntryDelta(null, sender, 0, "0xfe");
            delta.ContractEntries[0].GasLimit = 1_000_000L;

            ITxTracer tracer = Substitute.For<ITxTracer>();
            tracer.IsTracingReceipt.Returns(true);
            
            executor.Execute(delta, tracer);
            
            var contractAddress = Address.OfContract(sender.ToKvmAddress(), 0);
            tracer.Received().MarkAsFailed(contractAddress, 1_000_000L, Arg.Any<byte[]>(), "Error");
        }

        private static DeltaExecutor PrepareExecutorForTest()
        {
            CatalystSpecProvider specProvider = new CatalystSpecProvider();
            StateProvider stateProvider = new StateProvider(new StateDb(), new StateDb(), LimboLogs.Instance);
            StorageProvider storageProvider = new StorageProvider(new StateDb(), stateProvider, LimboLogs.Instance);
            VirtualMachine virtualMachine = new VirtualMachine(stateProvider, storageProvider, new StateUpdateHashProvider(), specProvider, LimboLogs.Instance);
            var logger = Substitute.For<ILogger>();
            logger.IsEnabled(Arg.Any<LogEventLevel>()).Returns(true);
            
            DeltaExecutor executor = new DeltaExecutor(specProvider, stateProvider, storageProvider, virtualMachine, logger);
            return executor;
        }

        [Fact]
        public void Does_not_crash_on_kvm_exception()
        {
            DeltaExecutor executor = PrepareExecutorForTest();
            
            var sender = _cryptoContext.GeneratePrivateKey().GetPublicKey();
            Delta delta = EntryUtils.PrepareSingleContractEntryDelta(null, sender, 0, "0x01");
            delta.ContractEntries[0].GasLimit = 1_000_000L;

            ITxTracer tracer = Substitute.For<ITxTracer>();
            tracer.IsTracingReceipt.Returns(true);
            
            executor.Execute(delta, tracer);
            
            var contractAddress = Address.OfContract(sender.ToKvmAddress(), 0);
            tracer.Received().MarkAsFailed(contractAddress, 1_000_000L, Arg.Any<byte[]>(), "Error");
        }
        
        [Fact]
        public void Can_execute_transfers_from_public_entries()
        {
            DeltaExecutor executor = PrepareExecutorForTest();
            
            var recipient = _cryptoContext.GeneratePrivateKey().GetPublicKey();
            var sender = _cryptoContext.GeneratePrivateKey().GetPublicKey();
            Delta delta = EntryUtils.PrepareSinglePublicEntryDelta(recipient, sender, 0);

            ITxTracer tracer = Substitute.For<ITxTracer>();
            tracer.IsTracingReceipt.Returns(true);
            
            executor.Execute(delta, tracer);
            
            tracer.Received().MarkAsSuccess(recipient.ToKvmAddress(), 21000, Bytes.Empty, Arg.Any<LogEntry[]>());
        }
        
        [Fact]
        public void Can_add_gas_to_existing_balance()
        {
            CatalystSpecProvider specProvider = new CatalystSpecProvider();
            StateProvider stateProvider = new StateProvider(new StateDb(), new StateDb(), LimboLogs.Instance);
            StorageProvider storageProvider = new StorageProvider(new StateDb(), stateProvider, LimboLogs.Instance);
            VirtualMachine virtualMachine = new VirtualMachine(stateProvider, storageProvider, new StateUpdateHashProvider(), specProvider, LimboLogs.Instance);
            var logger = Substitute.For<ILogger>();
            logger.IsEnabled(Arg.Any<LogEventLevel>()).Returns(true);
            
            var recipient = _cryptoContext.GeneratePrivateKey().GetPublicKey();
            var sender = _cryptoContext.GeneratePrivateKey().GetPublicKey();
            
            stateProvider.CreateAccount(sender.ToKvmAddress(), 1000.Kat());
            stateProvider.CreateAccount(Address.Zero, 1000.Kat());
            stateProvider.Commit(specProvider.GenesisSpec);
            
            DeltaExecutor executor = new DeltaExecutor(specProvider, stateProvider, storageProvider, virtualMachine, logger);
            
            Delta delta = EntryUtils.PrepareSingleContractEntryDelta(recipient, sender, 0);
            delta.ContractEntries[0].GasPrice = 1;

            ITxTracer tracer = Substitute.For<ITxTracer>();
            tracer.IsTracingReceipt.Returns(true);
            
            executor.Execute(delta, tracer);
            
            tracer.Received().MarkAsSuccess(recipient.ToKvmAddress(), 21000, Bytes.Empty, Arg.Any<LogEntry[]>());
        }
    }
}
