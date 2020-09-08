
using System;
using System.Collections.Generic;
using Nethermind.Abi;
using Nethermind.Consensus.AuRa.Contracts;
using Nethermind.Core;
using Nethermind.Evm;
using Nethermind.Serialization.Json.Abi;
using Nethermind.State;

namespace Catalyst.Module.ConvanSmartContract
{
    public class DfsSmartContract : Nethermind.Consensus.AuRa.Contracts.Contract
    {
        private readonly IAbiEncoder _abiEncoder;

        private static readonly IEqualityComparer<LogEntry> LogEntryEqualityComparer = new LogEntryAddressAndTopicEqualityComparer();

        internal static readonly AbiDefinition Definition = new AbiDefinitionParser().Parse<DfsSmartContract>();

        private ConstantContract Constant { get; }

        public DfsSmartContract(
            ITransactionProcessor transactionProcessor,
            IAbiEncoder abiEncoder,
            Address contractAddress,
            IStateProvider stateProvider,
            IReadOnlyTransactionProcessorSource readOnlyReadOnlyTransactionProcessorSource)
            : base(transactionProcessor, abiEncoder, contractAddress)
        {
            _abiEncoder = abiEncoder ?? throw new ArgumentNullException(nameof(abiEncoder));
            Constant = GetConstant(stateProvider, readOnlyReadOnlyTransactionProcessorSource);
        }
        
        public Address[] GetPeersByCid(BlockHeader blockHeader) => Constant.Call<Address[]>(blockHeader, Definition.GetFunction(nameof(GetPeersByCid)), Address.Zero);

        private Address[] DecodePeerAddresses(byte[] data)
        {
            var objects = _abiEncoder.Decode(Definition.GetFunction(nameof(GetPeersByCid)).GetReturnInfo(), data);
            return GetPeerAddresses(objects);
        }

        private static Address[] GetPeerAddresses(object[] objects)
        {
            return (Address[])objects[0];
        }

        public void EnsureSystemAccount()
        {
            EnsureSystemAccount(Constant.StateProvider);
        }
    }
}
