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
using System.Collections.Generic;
using System.Net;
using System.Text;
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
        private JsonProtoObjectConverter<TransactionBroadcast> _jsonProtoObjectConverter;

        public JsonProtoObjectConverterTests()
        {
            _jsonProtoObjectConverter = new JsonProtoObjectConverter<TransactionBroadcast>();
        }

        //public JsonSerializerSettings GenerateSerializerSettings(JsonConverter jsonConverter)
        //{
        //    var settings = new JsonSerializerSettings
        //    {
        //        Converters = new List<JsonConverter>
        //        {
        //            jsonConverter
        //        },
        //        NullValueHandling = NullValueHandling.Ignore
        //    };
        //    return settings;
        //}

        //[Fact]
        //public void Can_Serialize_ProtoObject()
        //{
        //    var settings = GenerateSerializerSettings(new JsonProtoObjectConverter<TransactionBroadcast>());

        //    var transaction = TransactionHelper.GetContractTransaction(ByteString.CopyFromUtf8("test"), 10, 10, 10);
        //    var transactionBytes = transaction.ToByteArray();

        //    var json = JsonConvert.SerializeObject(transaction, settings);
        //    var transactionJsonDeserialized = JsonConvert.DeserializeObject<TransactionBroadcast>(json, settings);
        //    var transactionJsonDeserializedBytes = transactionJsonDeserialized.ToByteArray();

        //    transactionJsonDeserializedBytes.Should().BeEquivalentTo(transactionBytes);
        //}
    }
}
