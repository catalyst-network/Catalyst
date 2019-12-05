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

using Catalyst.Core.Lib.Util;
using Catalyst.Protocol.Wire;
using Catalyst.TestUtils;
using FluentAssertions;
using Google.Protobuf;
using Newtonsoft.Json;
using Xunit;

namespace Catalyst.Core.Lib.Tests.UnitTests.Utils
{
    public sealed class JsonProtoObjectConverterTests
    {
        private readonly string _transactionJson;
        private readonly TransactionBroadcast _transaction;
        private readonly JsonProtoObjectConverter<TransactionBroadcast> _jsonProtoObjectConverter;

        public JsonProtoObjectConverterTests()
        {
            _jsonProtoObjectConverter = new JsonProtoObjectConverter<TransactionBroadcast>();
            _transaction = TransactionHelper.GetContractTransaction(ByteString.CopyFromUtf8("test"), 10, 10, 10);
            _transactionJson = "{ \"publicEntry\": { \"base\": { \"receiverPublicKey\": \"cmVjZWl2ZXI=\", \"senderPublicKey\": \"c2VuZGVy\", \"transactionFees\": \"Ag==\" }, \"amount\": \"Cg==\", \"data\": \"dGVzdA==\", \"timestamp\": \"1970-01-01T03:25:45Z\", \"signature\": { \"signingContext\": { \"networkType\": \"DEVNET\", \"signatureType\": \"TRANSACTION_PUBLIC\" }, \"rawBytes\": \"c2lnbmF0dXJl\" }, \"gasPrice\": \"Cg==\", \"gasLimit\": \"10\" } }";
        }

        [Fact]
        public void Check_Convertibility_Must_Succeed()
        {
            _jsonProtoObjectConverter.CanConvert(_transaction.GetType()).Should().BeTrue();
        }

        [Fact]
        public void Check_Convertibility_Should_Fail()
        {
            _jsonProtoObjectConverter.CanConvert(_transaction.ToByteArray().GetType()).Should().BeFalse();
        }

        [Fact]
        public void Write_To_Json_Should_Succeed()
        {
            var serialized = JsonConvert.SerializeObject(_transaction, _jsonProtoObjectConverter);
            serialized.Should().Contain(_transactionJson);
        }

        [Fact]
        public void Read_Json_Should_Succeed()
        {
            JsonConvert.DeserializeObject<TransactionBroadcast>(_transactionJson, _jsonProtoObjectConverter).Should()
               .Be(_transaction);
        }
    }
}
