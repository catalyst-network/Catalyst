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

using Catalyst.Protocol.Common;
using Catalyst.Protocol.IPPN;
using Catalyst.Protocol.Transaction;
using FluentAssertions;
using Google.Protobuf;
using System;
using Xunit;

namespace Catalyst.Protocol.UnitTests
{
    public class ProtobufExtensionsTests
    {
        public static void Can_Identify_Broadcast_Message()
        {
            var protocolMessageSigned = new ProtocolMessageSigned
            {
                Message = new ProtocolMessage
                {
                    Value = new TransactionBroadcast().ToByteString(),
                    TypeUrl = TransactionBroadcast.Descriptor.ShortenedFullName()
                }
            };

            var protocolMessage = new ProtocolMessage
            {
                Value = protocolMessageSigned.ToByteString(),
                TypeUrl = ProtocolMessageSigned.Descriptor.ShortenedFullName()
            };

            protocolMessage.IsBroadCastMessage().Should().BeTrue();
        }

        [Fact]
        public static void ShortenedFullName_should_remove_namespace_start()
        {
            TransactionBroadcast.Descriptor.FullName.Should().Be("Catalyst.Protocol.Transaction.TransactionBroadcast");
            TransactionBroadcast.Descriptor.ShortenedFullName().Should().Be("Transaction.TransactionBroadcast");
        }

        [Fact]
        public static void ShortenedProtoFullName_should_remove_namespace_start()
        {
            PingRequest.Descriptor.FullName.Should().Be("Catalyst.Protocol.IPPN.PingRequest");
            typeof(PingRequest).ShortenedProtoFullName().Should().Be("IPPN.PingRequest");
        }

        [Theory]
        [InlineData("MyFunnyRequest", "MyFunnyResponse")]
        [InlineData("Request", "Response")]
        [InlineData("Some.Namespace.ClassRequest", "Some.Namespace.ClassResponse")]
        public static void GetResponseType_should_swap_request_suffix_for_response_suffix(string requestType, string responseType)
        {
            requestType.GetResponseType().Should().Be(responseType);
        }

        [Theory]
        [InlineData("MyFunnyResponse", "MyFunnyRequest")]
        [InlineData("Response", "Request")]
        [InlineData("Some.Namespace.ClassResponse", "Some.Namespace.ClassRequest")]
        public static void GetRequestType_should_swap_request_suffix_for_response_suffix(string responseType, string requestType)
        {
            responseType.GetRequestType().Should().Be(requestType);
        }
    }
}
