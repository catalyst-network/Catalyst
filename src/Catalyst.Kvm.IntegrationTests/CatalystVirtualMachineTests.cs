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
using FluentAssertions;
using Nethermind.Core;
using Nethermind.Core.Extensions;
using Nethermind.Core.Json;
using Nethermind.Core.Specs;
using Nethermind.Evm;
using Nethermind.Evm.Tracing;
using Nethermind.Logging;
using Nethermind.Store;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Kvm.IntegrationTests
{
    public sealed class CatalystVirtualMachineTests
    {
        private readonly ITestOutputHelper _testOutputHelper;
        public CatalystVirtualMachineTests(ITestOutputHelper testOutputHelper) { _testOutputHelper = testOutputHelper; }

        [Fact]
        public void Can_Run_Smoke_test()
        {
            var code = Bytes.FromHexString("0x600060000100");
            GethLikeTxTracer txTracer = RunVirtualMachine(code);
            EthereumJsonSerializer serializer = new EthereumJsonSerializer();
            var trace = txTracer.BuildResult();
            _testOutputHelper.WriteLine(serializer.Serialize(trace, true));
        }
        
        [Fact]
        public void Can_Invoke_Range_Proof_Precompile()
        {
            var code = Bytes.FromHexString("0x60008080806201000062050000F400");
            GethLikeTxTracer txTracer = RunVirtualMachine(code);
            EthereumJsonSerializer serializer = new EthereumJsonSerializer();
            var trace = txTracer.BuildResult();
            _testOutputHelper.WriteLine(serializer.Serialize(trace, true));
            trace.Failed.Should().Be(false);
            trace.Entries.Last().Stack.Count.Should().Be(1);
            trace.Entries.Last().Stack.Last().Should().Be(VirtualMachine.BytesOne32.ToHexString());
        }

        [Fact]
        public void Can_Invoke_Origin()
        {
            var code = Bytes.FromHexString("0x3200");
            GethLikeTxTracer txTracer = RunVirtualMachine(code);
            EthereumJsonSerializer serializer = new EthereumJsonSerializer();
            var trace = txTracer.BuildResult();
            _testOutputHelper.WriteLine(serializer.Serialize(trace, true));
            trace.Failed.Should().Be(false);
            trace.Entries.Last().Stack.Count.Should().Be(1);
            trace.Entries.Last().Stack.Last().Should().Be(VirtualMachine.BytesZero32.ToHexString());
        }
        
        [Fact]
        public void Can_Invoke_Address()
        {
            var code = Bytes.FromHexString("0x3000");
            GethLikeTxTracer txTracer = RunVirtualMachine(code);
            EthereumJsonSerializer serializer = new EthereumJsonSerializer();
            var trace = txTracer.BuildResult();
            _testOutputHelper.WriteLine(serializer.Serialize(trace, true));
            trace.Failed.Should().Be(false);
            trace.Entries.Last().Stack.Count.Should().Be(1);
            trace.Entries.Last().Stack.Last().Should().Be(VirtualMachine.BytesZero32.ToHexString());
        }
        
        [Fact]
        public void Can_Invoke_Blockhash()
        {
            var code = Bytes.FromHexString("0x60014000");
            Assert.Throws<NotImplementedException>(() => RunVirtualMachine(code));
        }

        [Fact]
        public void Can_Invoke_Coinbase()
        {
            var code = Bytes.FromHexString("0x4100");
            GethLikeTxTracer txTracer = RunVirtualMachine(code);
            EthereumJsonSerializer serializer = new EthereumJsonSerializer();
            var trace = txTracer.BuildResult();
            _testOutputHelper.WriteLine(serializer.Serialize(trace, true));
            trace.Failed.Should().Be(false);
            trace.Entries.Last().Stack.Count.Should().Be(1);
            trace.Entries.Last().Stack.Last().Should().Be(VirtualMachine.BytesZero32.ToHexString());
        }
        
        [Fact]
        public void Can_Invoke_Timestamp()
        {
            var code = Bytes.FromHexString("0x4200");
            GethLikeTxTracer txTracer = RunVirtualMachine(code);
            EthereumJsonSerializer serializer = new EthereumJsonSerializer();
            var trace = txTracer.BuildResult();
            _testOutputHelper.WriteLine(serializer.Serialize(trace, true));
            trace.Failed.Should().Be(false);
            trace.Entries.Last().Stack.Count.Should().Be(1);
            trace.Entries.Last().Stack.Last().Should().Be(VirtualMachine.BytesOne32.ToHexString());
        }
        
        [Fact]
        public void Can_Invoke_Number()
        {
            var code = Bytes.FromHexString("0x4300");
            GethLikeTxTracer txTracer = RunVirtualMachine(code);
            EthereumJsonSerializer serializer = new EthereumJsonSerializer();
            var trace = txTracer.BuildResult();
            _testOutputHelper.WriteLine(serializer.Serialize(trace, true));
            trace.Failed.Should().Be(false);
            trace.Entries.Last().Stack.Count.Should().Be(1);
            trace.Entries.Last().Stack.Last().Should().Be(VirtualMachine.BytesOne32.ToHexString());
        }
        
        [Fact]
        public void Can_Invoke_Difficulty()
        {
            var code = Bytes.FromHexString("0x4400");
            GethLikeTxTracer txTracer = RunVirtualMachine(code);
            EthereumJsonSerializer serializer = new EthereumJsonSerializer();
            var trace = txTracer.BuildResult();
            _testOutputHelper.WriteLine(serializer.Serialize(trace, true));
            trace.Failed.Should().Be(false);
            trace.Entries.Last().Stack.Count.Should().Be(1);
            trace.Entries.Last().Stack.Last().Should().Be(VirtualMachine.BytesOne32.ToHexString());
        }
        
        [Fact]
        public void Can_Invoke_Gas_Limit()
        {
            var code = Bytes.FromHexString("0x4500");
            GethLikeTxTracer txTracer = RunVirtualMachine(code);
            EthereumJsonSerializer serializer = new EthereumJsonSerializer();
            var trace = txTracer.BuildResult();
            _testOutputHelper.WriteLine(serializer.Serialize(trace, true));
            trace.Failed.Should().Be(false);
            trace.Entries.Last().Stack.Count.Should().Be(1);
            trace.Entries.Last().Stack.Last().Should().Be("f4240".PadLeft(64, '0'));
        }
        
        [Fact]
        public void Can_Invoke_Chain_Id()
        {
            var code = Bytes.FromHexString("0x4600");
            GethLikeTxTracer txTracer = RunVirtualMachine(code);
            EthereumJsonSerializer serializer = new EthereumJsonSerializer();
            var trace = txTracer.BuildResult();
            _testOutputHelper.WriteLine(serializer.Serialize(trace, true));
            trace.Failed.Should().Be(false);
            trace.Entries.Last().Stack.Count.Should().Be(1);
            trace.Entries.Last().Stack.Last().Should().Be(VirtualMachine.BytesOne32.ToHexString());
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
            GethLikeTxTracer txTracer = new GethLikeTxTracer(GethTraceOptions.Default);

            IDb stateDbDevice = new MemDb();
            IDb codeDbDevice = new MemDb();

            ISnapshotableDb stateDb = new StateDb(stateDbDevice);
            ISnapshotableDb codeDb = new StateDb(codeDbDevice);

            IStateProvider stateProvider = new StateProvider(stateDb, codeDb, LimboLogs.Instance);
            IStorageProvider storageProvider = new StorageProvider(stateDb, stateProvider, LimboLogs.Instance);

            IStateUpdateHashProvider stateUpdateHashProvider = new StateUpdateHashProvider();
            ISpecProvider specProvider = new CatalystSpecProvider();

            // these values will be set by the tx processor within the state update logic
            ExecutionEnvironment env = new ExecutionEnvironment();
            env.Originator = Address.Zero;       // tx sender
            env.Sender = Address.Zero;           // sender of this call for a given call depth
            env.ExecutingAccount = Address.Zero; // account in which context the code is executed, it may be different from the code source when invoking a lib
            env.Value = 1.Kat();                 // sometimes the value is just a description of an upper level call to be used by a an invoke library method
            env.TransferValue = 1.Kat();         // this is the actual value transferred from sender to recipient
            env.GasPrice = 0;                    // conversion from gas units to FULs
            env.InputData = new byte[0];         // only needed for contracts requiring input (ensure that this is not limited to 60bytes)
            env.CallDepth = 0;                   // zero when starting tx

            StateUpdate stateUpdate = new StateUpdate(); // Catalyst single state update context (all phases together)
            stateUpdate.Difficulty = 1;                  // some metric describing the state update that is relevant for consensus
            stateUpdate.Number = 1;                      // state update sequence number
            stateUpdate.Timestamp = 1;                   // state update T0
            stateUpdate.GasLimit = 1_000_000;            // max gas units to be available for this tx inside the kvm
            stateUpdate.GasBeneficiary = Address.Zero;   // will get all the gas fees
            stateUpdate.GasUsed = 0L;                    // zero if this is the first transaction in the update (accumulator over txs)
            env.CurrentBlock = stateUpdate;

            // this would be loaded by the tx processor from the recipient's code storage
            CodeInfo codeInfo = new CodeInfo(code);
            env.CodeInfo = codeInfo;
            env.CodeSource = Address.Zero;

            // this would be set by the tx processor that understands the type of transaction
            VmState vmState = new VmState(1_000_000L, env, ExecutionType.Transaction, false, true, false);

            CatalystVirtualMachine virtualMachine = new CatalystVirtualMachine(stateProvider, storageProvider, stateUpdateHashProvider, specProvider, LimboLogs.Instance);
            virtualMachine.Run(vmState, txTracer);
            return txTracer;
        }
    }
}
