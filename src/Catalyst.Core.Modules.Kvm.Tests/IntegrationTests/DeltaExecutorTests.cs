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
using Catalyst.Abstractions.Kvm;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.Extensions.Protocol.Wire;
using Catalyst.Core.Modules.Cryptography.BulletProofs;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Protocol.Cryptography;
using Catalyst.Protocol.Network;
using Catalyst.TestUtils;
using Google.Protobuf;
using MultiFormats.Registry;
using Nethermind.Core;
using Nethermind.Core.Extensions;
using Nethermind.Db;
using Nethermind.Evm;
using Nethermind.Evm.Tracing;
using Nethermind.Logging;
using Nethermind.State;
using NSubstitute;
using Serilog.Events;
using NUnit.Framework;
using ILogger = Serilog.ILogger;
using Nethermind.Trie;
using Nethermind.Blockchain;
using Nethermind.Core.Specs;
using Nethermind.Core.Crypto;
using Nethermind.Specs;

namespace Catalyst.Core.Modules.Kvm.Tests.IntegrationTests
{
    [TestFixture]
    [Category(Traits.IntegrationTest)] 
    public sealed class DeltaExecutorTests
    {
        private ICryptoContext _cryptoContext = new FfiWrapper();
        private CatalystSpecProvider _specProvider;
        private WorldState _stateProvider;
        private IPrivateKey _senderPrivateKey;
        private IPublicKey _senderPublicKey;
        private SigningContext _signingContext;
        private IPublicKey _recipient;
        private IPublicKey _poorSender;
        private DeltaExecutor _executor;

        /**
         * @TODO this should extend file system based tests and resolve tests via autofac container
         */
        [SetUp]
        public void Init()
        {
            _specProvider = new CatalystSpecProvider();
            IDb codeDb = new MemDb();
            var patriciaTree = new PatriciaTree();
            _stateProvider = new WorldState(patriciaTree.TrieStore, codeDb, LimboLogs.Instance);
            BlockhashProvider blockHashProvider = new BlockhashProvider(null, LimboLogs.Instance);
            ISpecProvider specProvider = new CatalystSpecProvider();
            IKvm virtualMachine = new KatVirtualMachine(_stateProvider,
                blockHashProvider,
                specProvider,
                new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("keccak-256")),
                new FfiWrapper(),
                LimboLogs.Instance);
            var logger = Substitute.For<ILogger>();
            logger.IsEnabled(Arg.Any<LogEventLevel>()).Returns(true);

            _senderPrivateKey = _cryptoContext.GeneratePrivateKey();
            _senderPublicKey = _senderPrivateKey.GetPublicKey();

            _recipient = _cryptoContext.GeneratePrivateKey().GetPublicKey();
            _poorSender = _cryptoContext.GeneratePrivateKey().GetPublicKey();

            _stateProvider.CreateAccount(_poorSender.ToKvmAddress(), 0.Kat());
            _stateProvider.CreateAccount(_senderPublicKey.ToKvmAddress(), 1000.Kat());
            _stateProvider.CreateAccount(Address.Zero, 1000.Kat());
            _stateProvider.Commit(_specProvider.GenesisSpec);

            _executor = new DeltaExecutor(_specProvider, _stateProvider, virtualMachine,
                new FfiWrapper(), logger);

            _signingContext = new SigningContext
            {
                NetworkType = NetworkType.Devnet,
                SignatureType = SignatureType.TransactionPublic
            };
        }

        [Test]
        public void Fails_when_sender_not_specified()
        {
            var delta = EntryUtils.PrepareSingleContractEntryDelta(_recipient, _senderPublicKey, 3);
            delta.PublicEntries[0].SenderAddress = ByteString.Empty;
            delta.PublicEntries[0].Signature = delta.PublicEntries[0]
               .GenerateSignature(_cryptoContext, _senderPrivateKey, _signingContext);

            var tracer = Substitute.For<ITxTracer>();
            tracer.IsTracingReceipt.Returns(true);

            _executor.Execute(delta, tracer);

            tracer.Received().MarkAsFailed(_recipient.ToKvmAddress(), 21000L, Bytes.Empty, "invalid");
        }

        [Test]
        public void Fails_when_gas_limit_below_data_intrinsic_cost()
        {
            var delta = EntryUtils.PrepareSingleContractEntryDelta(_recipient, _senderPublicKey, 0, "0x0102");
            delta.PublicEntries[0].GasLimit = 21001; // just above 21000 but not paying for data
            delta.PublicEntries[0].Signature = delta.PublicEntries[0]
               .GenerateSignature(_cryptoContext, _senderPrivateKey, _signingContext);

            var tracer = Substitute.For<ITxTracer>();
            tracer.IsTracingReceipt.Returns(true);

            _executor.Execute(delta, tracer);

            tracer.Received().MarkAsFailed(_recipient.ToKvmAddress(), 21001, Bytes.Empty, "invalid");
        }

        [Test]
        public void Fails_when_gas_limit_below_entry_intrinsic_cost()
        {
            var delta = EntryUtils.PrepareSingleContractEntryDelta(_recipient, _senderPublicKey, 0);
            delta.PublicEntries[0].GasLimit = 20999; // just below 21000
            delta.PublicEntries[0].Signature = delta.PublicEntries[0]
               .GenerateSignature(_cryptoContext, _senderPrivateKey, _signingContext);

            var tracer = Substitute.For<ITxTracer>();
            tracer.IsTracingReceipt.Returns(true);

            _executor.Execute(delta, tracer);

            tracer.Received().MarkAsFailed(_recipient.ToKvmAddress(), 20999, Bytes.Empty, "invalid");
        }

        [Test]
        public void Fails_on_wrong_nonce()
        {
            var delta = EntryUtils.PrepareSingleContractEntryDelta(_recipient, _senderPublicKey, 0, "0x", 1);
            delta.PublicEntries[0].Signature = delta.PublicEntries[0]
               .GenerateSignature(_cryptoContext, _senderPrivateKey, _signingContext);

            var tracer = Substitute.For<ITxTracer>();
            tracer.IsTracingReceipt.Returns(true);

            _executor.Execute(delta, tracer);

            tracer.Received().MarkAsFailed(_recipient.ToKvmAddress(), 21000, Bytes.Empty, "invalid");
        }

        [Test]
        public void Fails_on_insufficient_balance()
        {
            var delta = EntryUtils.PrepareSingleContractEntryDelta(_recipient, _poorSender, 0);

            // when gas price is non-zero then sender needs to have a non-zero balance to pay for the gas cost
            delta.PublicEntries[0].GasPrice = 1.ToUint256ByteString();
            delta.PublicEntries[0].Signature = delta.PublicEntries[0]
               .GenerateSignature(_cryptoContext, _senderPrivateKey, _signingContext);

            var tracer = Substitute.For<ITxTracer>();
            tracer.IsTracingReceipt.Returns(true);

            _executor.Execute(delta, tracer);

            tracer.Received().MarkAsFailed(_recipient.ToKvmAddress(), 21000, Bytes.Empty, "invalid");
        }

        [Test]
        public void Fails_when_tx_beyond_delta_gas_limit()
        {
            var delta = EntryUtils.PrepareSingleContractEntryDelta(_recipient, _senderPublicKey, 0);
            delta.PublicEntries[0].GasLimit = 10_000_000;
            delta.PublicEntries[0].Signature = delta.PublicEntries[0]
               .GenerateSignature(_cryptoContext, _senderPrivateKey, _signingContext);

            var tracer = Substitute.For<ITxTracer>();
            tracer.IsTracingReceipt.Returns(true);

            _executor.Execute(delta, tracer);

            tracer.Received().MarkAsFailed(_recipient.ToKvmAddress(), 10_000_000, Bytes.Empty, "invalid");
        }

        [Test]
        public void Can_deploy_code()
        {
            var delta = EntryUtils.PrepareSingleContractEntryDelta(null, _senderPublicKey, 0,
                "0x60016000526001601FF300");
            delta.PublicEntries[0].GasLimit = 1_000_000L;
            delta.PublicEntries[0].ReceiverAddress = ByteString.Empty;
            delta.PublicEntries[0].Signature = delta.PublicEntries[0]
               .GenerateSignature(_cryptoContext, _senderPrivateKey, _signingContext);

            var tracer = Substitute.For<ITxTracer>();
            tracer.IsTracingReceipt.Returns(true);

            _executor.Execute(delta, tracer);

            var contractAddress = ContractAddress.From(_senderPublicKey.ToKvmAddress(), 0);
            tracer.Received().MarkAsSuccess(contractAddress, 53370, Arg.Any<byte[]>(), Arg.Any<LogEntry[]>());
        }

        [Test]
        public void Can_self_destruct()
        {
            var delta = EntryUtils.PrepareSingleContractEntryDelta(null, _senderPublicKey, 0,
                "0x730001020304050607080910111213141516171819ff");
            delta.PublicEntries[0].GasLimit = 1_000_000L;
            delta.PublicEntries[0].ReceiverAddress = ByteString.Empty;
            delta.PublicEntries[0].Signature = delta.PublicEntries[0]
               .GenerateSignature(_cryptoContext, _senderPrivateKey, _signingContext);

            var tracer = Substitute.For<ITxTracer>();
            tracer.IsTracingReceipt.Returns(true);

            _executor.Execute(delta, tracer);

            var contractAddress = ContractAddress.From(_senderPublicKey.ToKvmAddress(), 0);
            tracer.Received().MarkAsSuccess(contractAddress, 34343, Arg.Any<byte[]>(), Arg.Any<LogEntry[]>());
        }

        [Test]
        public void Fails_when_not_enough_gas_for_code_deposit()
        {
            var delta = EntryUtils.PrepareSingleContractEntryDelta(null, _senderPublicKey, 0,
                "0x60016000526001601FF300");
            delta.PublicEntries[0].GasLimit = 53369;
            delta.PublicEntries[0].ReceiverAddress = ByteString.Empty;
            delta.PublicEntries[0].Signature = delta.PublicEntries[0]
               .GenerateSignature(_cryptoContext, _senderPrivateKey, _signingContext);

            var tracer = Substitute.For<ITxTracer>();
            tracer.IsTracingReceipt.Returns(true);

            _executor.Execute(delta, tracer);

            var contractAddress = ContractAddress.From(_senderPublicKey.ToKvmAddress(), 0);
            tracer.Received().MarkAsFailed(contractAddress, 53369, Arg.Any<byte[]>(), null);
        }

        [Test]
        public void Throws_on_theoretical_contract_crash()
        {
            var contractAddress = ContractAddress.From(_senderPublicKey.ToKvmAddress(), 0);
            _stateProvider.CreateAccount(contractAddress, 1000.Kat());
            IReleaseSpec specProvider = new ReleaseSpec();
            ReadOnlyMemory<byte> codeBytes = Bytes.FromHexString("0x01");
            _stateProvider.InsertCode(Address.Zero,
                new Hash256(Bytes.FromHexString("0x01")),
                codeBytes,
                specProvider);
            _stateProvider.Commit(_specProvider.GenesisSpec);

            var delta = EntryUtils.PrepareSingleContractEntryDelta(null, _senderPublicKey, 0,
                "0x60016000526001601FF300");
            delta.PublicEntries[0].GasLimit = 1_000_000L;
            delta.PublicEntries[0].ReceiverAddress = ByteString.Empty;
            delta.PublicEntries[0].Signature = delta.PublicEntries[0]
               .GenerateSignature(_cryptoContext, _senderPrivateKey, _signingContext);

            var tracer = Substitute.For<ITxTracer>();
            tracer.IsTracingReceipt.Returns(true);

            _executor.Execute(delta, tracer);

            tracer.Received().MarkAsFailed(contractAddress, 1_000_000L, Arg.Any<byte[]>(), null);
        }

        [Test]
        public void Update_storage_root_on_contract_clash()
        {
            var contractAddress = ContractAddress.From(_senderPublicKey.ToKvmAddress(), 0);
            _stateProvider.CreateAccount(contractAddress, 1000.Kat());
            _stateProvider.Commit(_specProvider.GenesisSpec);

            var delta = EntryUtils.PrepareSingleContractEntryDelta(null, _senderPublicKey, 0,
                "0x60016000526001601FF300");
            delta.PublicEntries[0].GasLimit = 1_000_000L;
            delta.PublicEntries[0].ReceiverAddress = ByteString.Empty;
            delta.PublicEntries[0].Signature = delta.PublicEntries[0]
               .GenerateSignature(_cryptoContext, _senderPrivateKey, _signingContext);

            var tracer = Substitute.For<ITxTracer>();
            tracer.IsTracingReceipt.Returns(true);

            _executor.Execute(delta, tracer);

            tracer.Received().MarkAsSuccess(contractAddress, 53370, Arg.Any<byte[]>(), Arg.Any<LogEntry[]>());
        }

        [Test]
        public void Can_deploy_code_read_only()
        {
            var delta = EntryUtils.PrepareSingleContractEntryDelta(null, _senderPublicKey, 0,
                "0x60016000526001601FF300");
            delta.PublicEntries[0].GasLimit = 1_000_000L;
            delta.PublicEntries[0].ReceiverAddress = ByteString.Empty;
            delta.PublicEntries[0].Signature = delta.PublicEntries[0]
               .GenerateSignature(_cryptoContext, _senderPrivateKey, _signingContext);

            var tracer = Substitute.For<ITxTracer>();
            tracer.IsTracingReceipt.Returns(true);

            _executor.CallAndReset(delta, tracer);

            var contractAddress = ContractAddress.From(_senderPublicKey.ToKvmAddress(), 0);
            tracer.Received().MarkAsSuccess(contractAddress, 53370, Arg.Any<byte[]>(), Arg.Any<LogEntry[]>());
        }

        [Test]
        public void Does_not_crash_on_kvm_error()
        {
            // here we test a case when we deploy a contract where constructor throws invalid opcode EVM error
            // 0xfe is a bad opcode that immediately causes an EVM error
            var delta = EntryUtils.PrepareSingleContractEntryDelta(null, _senderPublicKey, 0, "0xfe");
            delta.PublicEntries[0].GasLimit = 1_000_000L;
            delta.PublicEntries[0].Signature = delta.PublicEntries[0]
               .GenerateSignature(_cryptoContext, _senderPrivateKey, _signingContext);

            var tracer = Substitute.For<ITxTracer>();
            tracer.IsTracingReceipt.Returns(true);

            _executor.Execute(delta, tracer);

            var contractAddress = ContractAddress.From(_senderPublicKey.ToKvmAddress(), 0);
            tracer.Received().MarkAsFailed(contractAddress, 1_000_000L, Arg.Any<byte[]>(), "BadInstruction");
        }

        [Test]
        public void Does_not_crash_on_kvm_exception()
        {
            // here we test a case when we deploy a contract where constructor throws StackUnderflowException
            // 0x01 is the ADD opcode which requires two items on the stack and stack is empty here
            // added here for full coverage as the errors (EVM) are handled differently in some cases (via .NET exceptions)
            var delta = EntryUtils.PrepareSingleContractEntryDelta(null, _senderPublicKey, 0, "0x01");
            delta.PublicEntries[0].GasLimit = 1_000_000L;
            delta.PublicEntries[0].Signature = delta.PublicEntries[0]
               .GenerateSignature(_cryptoContext, _senderPrivateKey, _signingContext);

            var tracer = Substitute.For<ITxTracer>();
            tracer.IsTracingReceipt.Returns(true);

            _executor.Execute(delta, tracer);

            var contractAddress = ContractAddress.From(_senderPublicKey.ToKvmAddress(), 0);
            tracer.Received().MarkAsFailed(contractAddress, 1_000_000L, Arg.Any<byte[]>(), "StackUnderflow");
        }

        [Test]
        public void Can_execute_transfers_from_public_entries()
        {
            var delta = EntryUtils.PrepareSinglePublicEntryDelta(_recipient, _senderPublicKey, 0);
            delta.PublicEntries[0].Signature = delta.PublicEntries[0]
               .GenerateSignature(_cryptoContext, _senderPrivateKey, _signingContext);

            var tracer = Substitute.For<ITxTracer>();
            tracer.IsTracingReceipt.Returns(true);

            _executor.Execute(delta, tracer);

            tracer.Received().MarkAsSuccess(_recipient.ToKvmAddress(), 21000, Bytes.Empty, Arg.Any<LogEntry[]>());
        }

        [Test]
        public void Can_add_gas_to_existing_balance()
        {
            var delta = EntryUtils.PrepareSingleContractEntryDelta(_recipient, _senderPublicKey, 0);
            delta.PublicEntries[0].GasPrice = 1.ToUint256ByteString();
            delta.PublicEntries[0].Signature = delta.PublicEntries[0]
               .GenerateSignature(_cryptoContext, _senderPrivateKey, _signingContext);

            var tracer = Substitute.For<ITxTracer>();
            tracer.IsTracingReceipt.Returns(true);

            _executor.Execute(delta, tracer);

            tracer.Received().MarkAsSuccess(_recipient.ToKvmAddress(), 21000, Bytes.Empty, Arg.Any<LogEntry[]>());
        }
    }
}
