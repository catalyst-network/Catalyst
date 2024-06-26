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
using System.Text;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Core.Modules.Cryptography.BulletProofs;
using Catalyst.Core.Modules.Hashing;
using Catalyst.TestUtils;
using Common.Logging;
using FluentAssertions;
using MultiFormats.Registry;
using Nethermind.Blockchain;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Core.Extensions;
using Nethermind.Core.Specs;
using Nethermind.Db;
using Nethermind.Evm;
using Nethermind.Evm.CodeAnalysis;
using Nethermind.Evm.Tracing;
using Nethermind.Evm.Tracing.GethStyle;
using Nethermind.Logging;
using Nethermind.Serialization.Json;
using Nethermind.Specs.Forks;
using Nethermind.Specs;
using Nethermind.State;
using Nethermind.Trie;
using Nethermind.Trie.Pruning;
using NUnit.Framework;
using IPrivateKey = Catalyst.Abstractions.Cryptography.IPrivateKey;
using Nethermind.Blockchain.Receipts;
using Nethermind.Crypto;
using Nethermind.Consensus.Comparers;
using Catalyst.TestUtils.Repository.TreeBuilder;

namespace Catalyst.Core.Modules.Kvm.Tests.IntegrationTests
{
    [TestFixture]
    [TestFixture]
    [Category(Traits.IntegrationTest)] 
    public sealed class CatalystVirtualMachineTests
    {
        [Test]
        public void Can_Run_Smoke_test()
        {
            var code = Bytes.FromHexString("0x600060000100");
            GethLikeTxTracer txTracer = RunVirtualMachine(code);
            EthereumJsonSerializer serializer = new EthereumJsonSerializer();
            GethLikeTxTrace trace = txTracer.BuildResult();
            TestContext.WriteLine(serializer.Serialize(trace, true));
        }

        [Test]
        public void Can_Invoke_Range_Proof_Precompile()
        {
            var code = Bytes.FromHexString("0x60008080806201000062050000F400");
            GethLikeTxTracer txTracer = RunVirtualMachine(code);
            EthereumJsonSerializer serializer = new EthereumJsonSerializer();
            GethLikeTxTrace trace = txTracer.BuildResult();
            TestContext.WriteLine(serializer.Serialize(trace, true));
            trace.Failed.Should().Be(false);
            trace.Entries.Last().Stack.Count().Should().Be(1);
        }

        [Test]
        public void Can_Invoke_Origin()
        {
            var code = Bytes.FromHexString("0x3200");
            GethLikeTxTracer txTracer = RunVirtualMachine(code);
            EthereumJsonSerializer serializer = new EthereumJsonSerializer();
            GethLikeTxTrace trace = txTracer.BuildResult();
            TestContext.WriteLine(serializer.Serialize(trace, true));
            trace.Failed.Should().Be(false);
            trace.Entries.Last().Stack.Count().Should().Be(1);
        }

        [Test]
        public void Can_Invoke_Address()
        {
            var code = Bytes.FromHexString("0x3000");
            GethLikeTxTracer txTracer = RunVirtualMachine(code);
            EthereumJsonSerializer serializer = new EthereumJsonSerializer();
            GethLikeTxTrace trace = txTracer.BuildResult();
            TestContext.WriteLine(serializer.Serialize(trace, true));
            trace.Failed.Should().Be(false);
            trace.Entries.Last().Stack.Count().Should().Be(1);
        }

        [Test]
        public void Can_Invoke_Blockhash()
        {
            var code = Bytes.FromHexString("0x60014000");
            Assert.Throws<NotImplementedException>(() => RunVirtualMachine(code));
        }

        [Test]
        public void Can_Invoke_Coinbase()
        {
            var code = Bytes.FromHexString("0x4100");
            GethLikeTxTracer txTracer = RunVirtualMachine(code);
            EthereumJsonSerializer serializer = new EthereumJsonSerializer();
            GethLikeTxTrace trace = txTracer.BuildResult();
            TestContext.WriteLine(serializer.Serialize(trace, true));
            trace.Failed.Should().Be(false);
            trace.Entries.Last().Stack.Count().Should().Be(1);
        }

        [Test]
        public void Can_Invoke_Timestamp()
        {
            var code = Bytes.FromHexString("0x4200");
            GethLikeTxTracer txTracer = RunVirtualMachine(code);
            EthereumJsonSerializer serializer = new EthereumJsonSerializer();
            GethLikeTxTrace trace = txTracer.BuildResult();
            TestContext.WriteLine(serializer.Serialize(trace, true));
            trace.Failed.Should().Be(false);
            trace.Entries.Last().Stack.Count().Should().Be(1);
        }

        [Test]
        public void Can_Invoke_Number()
        {
            var code = Bytes.FromHexString("0x4300");
            GethLikeTxTracer txTracer = RunVirtualMachine(code);
            EthereumJsonSerializer serializer = new EthereumJsonSerializer();
            GethLikeTxTrace trace = txTracer.BuildResult();
            TestContext.WriteLine(serializer.Serialize(trace, true));
            trace.Failed.Should().Be(false);
            trace.Entries.Last().Stack.Count().Should().Be(1);
        }

        [Test]
        public void Can_Invoke_Difficulty()
        {
            var code = Bytes.FromHexString("0x4400");
            GethLikeTxTracer txTracer = RunVirtualMachine(code);
            EthereumJsonSerializer serializer = new EthereumJsonSerializer();
            GethLikeTxTrace trace = txTracer.BuildResult();
            TestContext.WriteLine(serializer.Serialize(trace, true));
            trace.Failed.Should().Be(false);
            trace.Entries.Last().Stack.Count().Should().Be(1);
        }

        [Test]
        public void Can_Invoke_Gas_Limit()
        {
            var code = Bytes.FromHexString("0x4500");
            GethLikeTxTracer txTracer = RunVirtualMachine(code);
            EthereumJsonSerializer serializer = new EthereumJsonSerializer();
            GethLikeTxTrace trace = txTracer.BuildResult();
            TestContext.WriteLine(serializer.Serialize(trace, true));
            trace.Failed.Should().Be(false);
            trace.Entries.Last().Stack.Count().Should().Be(1);
            trace.Entries.Last().Stack.Last().Should().Be("f4240".PadLeft(64, '0'));
        }

        [Test]
        public void Can_Invoke_Chain_Id()
        {
            var code = Bytes.FromHexString("0x4600");
            GethLikeTxTracer txTracer = RunVirtualMachine(code);
            EthereumJsonSerializer serializer = new EthereumJsonSerializer();
            GethLikeTxTrace trace = txTracer.BuildResult();
            TestContext.WriteLine(serializer.Serialize(trace, true));
            trace.Failed.Should().Be(false);
            trace.Entries.Last().Stack.Count().Should().Be(1);
        }

        [Test]
        public void Blake_precompile()
        {
            Address blakeAddress = Address.FromNumber(1 + KatVirtualMachine.CatalystPrecompilesAddressingSpace);
            string addressCode = blakeAddress.Bytes.ToHexString(false);
            var code = Bytes.FromHexString("0x602060006080600073" + addressCode + "45fa00");
            GethLikeTxTracer txTracer = RunVirtualMachine(code);
            EthereumJsonSerializer serializer = new EthereumJsonSerializer();
            GethLikeTxTrace trace = txTracer.BuildResult();
            TestContext.WriteLine(serializer.Serialize(trace, true));
            trace.Entries.Last().Stack.First().Should().Be("0000000000000000000000000000000000000000000000000000000000000001");
            trace.Entries.Last().Memory.First().Should().Be("378d0caaaa3855f1b38693c1d6ef004fd118691c95c959d4efa950d6d6fcf7c1");
        }

        [Test]
        public void Ed25519_precompile_can_verify_correct_sig()
        {
            HashProvider hashProvider = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("blake2b-256"));

            FfiWrapper cryptoContext = new FfiWrapper();
            IPrivateKey signingPrivateKey = cryptoContext.GeneratePrivateKey();
            var signingPublicKeyBytes = signingPrivateKey.GetPublicKey().Bytes;

            var byteCode = PrepareEd25519PrecompileCall(hashProvider, cryptoContext, signingPrivateKey, signingPrivateKey);

            GethLikeTxTracer txTracer = RunVirtualMachine(byteCode);
            EthereumJsonSerializer serializer = new EthereumJsonSerializer();
            GethLikeTxTrace trace = txTracer.BuildResult();
            TestContext.WriteLine(serializer.Serialize(trace, true));
            trace.Entries.Last().Stack.First().Should().Be("0000000000000000000000000000000000000000000000000000000000000001");
            trace.Entries.Last().Memory.First().Should().StartWith("01");
        }
        
        [Test]
        public void Ed25519_precompile_can_verify_incorrect_sig()
        {
            HashProvider hashProvider = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("keccak-256"));
            FfiWrapper cryptoContext = new FfiWrapper();
            IPrivateKey signingPrivateKey = cryptoContext.GeneratePrivateKey();
            IPrivateKey otherPrivateKey = cryptoContext.GeneratePrivateKey();
            Assert.That(signingPrivateKey, Is.EqualTo(otherPrivateKey));

            var byteCode = PrepareEd25519PrecompileCall(hashProvider, cryptoContext, signingPrivateKey, otherPrivateKey);

            GethLikeTxTracer txTracer = RunVirtualMachine(byteCode);
            EthereumJsonSerializer serializer = new EthereumJsonSerializer();
            GethLikeTxTrace trace = txTracer.BuildResult();
            TestContext.WriteLine(serializer.Serialize(trace, true));
            trace.Entries.Last().Stack.First().Should().Be("0000000000000000000000000000000000000000000000000000000000000001");
            trace.Entries.Last().Memory.First().Should().StartWith("00");
        }
        
        [Test]
        public void Ed25519_precompile_can_report_too_short_input()
        {
            var byteCode = Bytes.FromHexString(
                
                // PUSH1 32 PUSH1 0 PUSH1 128 PUSH1 0 PUSH20 address GAS STATICCALL
                // make a call to precompile and pass invalid [0,128) bytes of memory as an input
                // and store result at [0,1) of memory array
                // allow precompile to use all the gas required
                "600160006080600073" +
                GetEd25519PrecompileAddressAsHex() +
                "45fa00");

            GethLikeTxTracer txTracer = RunVirtualMachine(byteCode);
            EthereumJsonSerializer serializer = new EthereumJsonSerializer();
            GethLikeTxTrace trace = txTracer.BuildResult();
            TestContext.WriteLine(serializer.Serialize(trace, true));
            trace.Entries.Last().Stack.First().Should().Be("0000000000000000000000000000000000000000000000000000000000000000");
        }
        
        [Test]
        public void Ed25519_precompile_can_report_too_long_input()
        {
            var byteCode = Bytes.FromHexString(
                
                // PUSH1 32 PUSH1 0 PUSH1 192 PUSH1 0 PUSH20 address GAS STATICCALL
                // make a call to precompile and pass invalid [0,192) bytes of memory as an input
                // and store result at [0,1) of memory array
                // allow precompile to use all the gas required
                "6001600060c0600073" +
                GetEd25519PrecompileAddressAsHex() +
                "45fa00");

            GethLikeTxTracer txTracer = RunVirtualMachine(byteCode);
            EthereumJsonSerializer serializer = new EthereumJsonSerializer();
            GethLikeTxTrace trace = txTracer.BuildResult();
            TestContext.WriteLine(serializer.Serialize(trace, true));
            trace.Entries.Last().Stack.First().Should().Be("0000000000000000000000000000000000000000000000000000000000000000");
        }

        private byte[] PrepareEd25519PrecompileCall(HashProvider hashProvider, FfiWrapper cryptoContext, IPrivateKey signingPrivateKey, IPrivateKey otherPrivateKey)
        {
            var data = Encoding.UTF8.GetBytes("Testing testing 1 2 3");
            var message = hashProvider.ComputeMultiHash(data).Digest;

            var signingContext = Encoding.UTF8.GetBytes("Testing testing 1 2 3 context");
            var context = hashProvider.ComputeMultiHash(signingContext).Digest;

            ISignature signature = cryptoContext.Sign(signingPrivateKey, message, context);
            var signatureBytes = signature.SignatureBytes;
            var publicKeyBytes = otherPrivateKey.GetPublicKey().Bytes;

            // below verify check is not needed but allows for greater confidence for any person in the future
            // that would approach and debug test when it goes wrong
            // =====================================================
            cryptoContext.Verify(signature, message, context)
               .Should().BeTrue("signature generated with private key should verify with corresponding public key");

            // =====================================================

            // save message hash in memory at position 0x00
            string pushToMemoryAt0 = "7f" + message.ToHexString(false) + "600052";

            // save first 32 bytes of the sig in memory starting from position 0x20
            string pushToMemoryAt32 = "7f" + signatureBytes.AsSpan(0, 32).ToHexString(false) + "602052";

            // save remaining 32 bytes of the sig in memory starting from position 0x40
            string pushToMemoryAt64 = "7f" + signatureBytes.AsSpan(32, 32).ToHexString(false) + "604052";

            // save context bytes in memory starting from position 0x60 (but this should be changed if context is smaller)
            string pushToMemoryAt96 = "7f" + context.ToHexString(false) + "606052";

            // save public key bytes in memory starting from position 0x80 (but this should be changed if context is smaller)
            string pushToMemoryAt128 = "7f" + publicKeyBytes.ToHexString(false) + "608052";

            // address of the precompile within Catalyst
            string addressCode = GetEd25519PrecompileAddressAsHex();

            var byteCode = Bytes.FromHexString(
                pushToMemoryAt0 +
                pushToMemoryAt32 +
                pushToMemoryAt64 +
                pushToMemoryAt96 +
                pushToMemoryAt128 +

                // PUSH1 32 PUSH1 0 PUSH1 160 PUSH1 0 PUSH20 address GAS STATICCALL
                // make a call to precompile and pass [0,160) bytes of memory as an input
                // and store result at [0,1) of memory array
                // allow precompile to use all the gas required
                "6001600060a0600073" +
                addressCode +
                "45fa00");

            TestContext.WriteLine(byteCode.ToHexString());
            return byteCode;
        }

        private static string GetEd25519PrecompileAddressAsHex()
        {
            Address edAddress = Address.FromNumber(2 + KatVirtualMachine.CatalystPrecompilesAddressingSpace);
            string addressCode = edAddress.Bytes.ToHexString(false);
            return addressCode;
        }

        /*
         * also need to test:
         * BALANCE
         * EXTCODECOPY
         * CALLDATACOPY
         * GASPRICE
         * CREATE
         */

        private static GethLikeTxTracer RunVirtualMachine(byte[] code)
        {
            GethLikeTxTracer txTracer = new GethLikeTxMemoryTracer(GethTraceOptions.Default);
            
            NoErrorLimboLogs logManager = NoErrorLimboLogs.Instance;

            IDbProvider dbProvider = TestMemDbProvider.Init();
            IDb codeDb = dbProvider.CodeDb;
            IDb stateDb = dbProvider.StateDb;
            SingleReleaseSpecProvider specProvider =
                new(ConstantinopleFix.Instance, MainnetSpecProvider.Instance.NetworkId, MainnetSpecProvider.Instance.ChainId);

            TrieStore trieStore = new(stateDb, LimboLogs.Instance);
            StateReader stateReader = new(trieStore, codeDb, logManager);
            WorldState stateProvider = new(trieStore, codeDb, logManager);
            stateProvider.CreateAccount(new Address(new ValueHash256()), 10000.Ether());
            stateProvider.Commit(specProvider.GenesisSpec);
            stateProvider.CommitTree(0);
            stateProvider.RecalculateStateRoot();

            InMemoryReceiptStorage receiptStorage = new();

            EthereumEcdsa ecdsa = new(specProvider.ChainId, logManager);
            BlockTree tree = Build.A.BlockTree().WithoutSettingHead.TestObject;
            ITransactionComparerProvider transactionComparerProvider =
                new TransactionComparerProvider(specProvider, tree);
            
            BlockhashProvider blockhashProvider = new(tree, specProvider, stateProvider, LimboLogs.Instance);

            KatVirtualMachine virtualMachine = new KatVirtualMachine(stateProvider,
                blockhashProvider,
                specProvider,
                new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("keccak-256")),
                new FfiWrapper(),
                LimboLogs.Instance);

            return txTracer;
        }
    }
}
