using System;
using Catalyst.Module.ConvanSmartContract;
using FluentAssertions;
using FluentAssertions.Json;
using Nethermind.Consensus.AuRa.Contracts;
using Nethermind.Serialization.Json.Abi;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Nethermind.Abi.Test.Json
{
    public class AbiDefinitionParserTests
    {
        [TestCase(typeof(ValidatorContract2))]
        public void Can_load_contract(Type contractType)
        {
            var parser = new AbiDefinitionParser();
            var json = parser.LoadContract(contractType);
            var contract = parser.Parse(json);
            var serialized = parser.Serialize(contract);
            JToken.Parse(serialized).Should().ContainSubtree(json);
        }
    }
}
