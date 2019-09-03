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
using Catalyst.Core.Extensions;
using Catalyst.Core.IO.Messaging.Correlation;
using Catalyst.Core.Util;
using Catalyst.Protocol;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.IPPN;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using FluentAssertions;
using Google.Protobuf;
using Multiformats.Hash;
using Xunit;

namespace Catalyst.Core.UnitTests.Extensions
{
    public class ProtobufExtensionsTests
    {
        [Fact]
        public static void ToAnySigned_should_happen_new_guid_to_request_if_not_specified()
        {
            //this ensures we won't get Guid.Empty and then a risk of mismatch;
            var wrapped = new PingRequest().ToProtocolMessage(PeerIdHelper.GetPeerId("you"));
            wrapped.CorrelationId.Should().NotBeEquivalentTo(Guid.Empty.ToByteString());
        }

        [Fact]
        public static void ToAnySigned_should_set_the_wrapper_fields()
        {
            var guid = CorrelationId.GenerateCorrelationId();
            var peerId = PeerIdHelper.GetPeerId("blablabla");
            var expectedContent = "content";
            var wrapped = new PeerId
            {
                ProtocolVersion = expectedContent.ToUtf8ByteString()
            }.ToProtocolMessage(peerId, guid);

            wrapped.CorrelationId.ToCorrelationId().Id.Should().Be(guid.Id);
            wrapped.PeerId.Should().Be(peerId);
            wrapped.TypeUrl.Should().Be(PeerId.Descriptor.ShortenedFullName());
            wrapped.FromProtocolMessage<PeerId>().ProtocolVersion.Should().Equal(expectedContent.ToUtf8ByteString());
        }

        [Fact]
        public static void ToProtocolMessage_When_Processing_Request_Should_Generate_New_CorrelationId_If_Not_Specified()
        {
            var peerId = PeerIdHelper.GetPeerId("someone");
            var request = new GetPeerCountRequest().ToProtocolMessage(peerId);
            request.CorrelationId.ToCorrelationId().Should().NotBe(default);
        }

        [Fact]
        public static void ToProtocolMessage_When_Processing_Response_Should_Fail_If_No_CorrelationId_Specified()
        {
            var peerId = PeerIdHelper.GetPeerId("someone");
            var response = new GetPeerCountResponse
            {
                PeerCount = 13
            };
            new Action(() => response.ToProtocolMessage(peerId))
               .Should().Throw<ArgumentException>();
        }

        [Fact]
        public void ToMultihash_Can_Convert_Valid_ByteString_To_Multihash()
        {
            var initialHash = Multihash.Sum(HashType.BLAKE2B_256, Encoding.UTF8.GetBytes("hello"));
            var byteString = initialHash.ToBytes().ToByteString();

            var convertedHash = byteString.AsMultihash();

            convertedHash.Should().Be(initialHash);
        }

        [Fact]
        public void ToMultihashString_Can_Convert_Valid_ByteString_To_String()
        {
            var initialHash = Multihash.Encode("hello", HashType.BLAKE2B_256);
            var byteString = initialHash.ToBytes().ToByteString();

            var multihash = byteString.AsMultihashString();
            multihash.Should().NotBe(null);
        }

        [Fact]
        public void ToCorrelationId_Should_Take_Care_Of_All_ByteStrings()
        {
            var tooLong = ByteUtil.GenerateRandomByteArray(43).ToByteString();
            var tooShort = ByteUtil.GenerateRandomByteArray(14).ToByteString();

            tooLong.ToCorrelationId().Should().Be(new CorrelationId(tooLong.ToByteArray().Take(16).ToArray()));
            tooShort.ToCorrelationId().Should().Be(new CorrelationId(tooShort.ToByteArray().Concat(new byte[] {0, 0}).ToArray()));
            ((ByteString) null).ToCorrelationId().Should().Be(new CorrelationId(Guid.Empty));
        }
    }
}
