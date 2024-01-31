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
using System.IO;
using System.Linq;
using Lib.P2P.PubSub;
using ProtoBuf;

namespace Lib.P2P.Tests.PubSub
{
    public sealed class PublishedMessageTest
    {
        private readonly Peer _self = new Peer {Id = "QmXK9VBxaXFuuT29AaPUTgW3jBWZ9JgLVZYdMYTHC6LLAH"};
        private readonly Peer _other = new Peer {Id = "QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ"};

        [Test]
        public void RoundTrip()
        {
            var a = new PublishedMessage
            {
                Topics = new[] {"topic"},
                Sender = _self,
                SequenceNumber = new byte[] {1, 2, 3, 4, 5, 6, 7, 8},
                DataBytes = new byte[] {0, 1, 0xfe, 0xff}
            };
            var ms = new MemoryStream();
            Serializer.Serialize(ms, a);
            ms.Position = 0;
            var b = Serializer.Deserialize<PublishedMessage>(ms);

            Assert.That(a.Topics.ToArray(), Is.EquivalentTo(b.Topics.ToArray()));
            Assert.That(a.Sender, Is.EqualTo(b.Sender));
            Assert.That(a.SequenceNumber, Is.EquivalentTo(b.SequenceNumber));
            Assert.That(a.DataBytes, Is.EquivalentTo(b.DataBytes));
            Assert.That(a.DataBytes.Length, Is.EqualTo(a.Size));
            Assert.That(b.DataBytes.Length, Is.EqualTo(b.Size));
        }

        [Test]
        public void MessageID_Is_Unique()
        {
            var a = new PublishedMessage
            {
                Topics = new[] {"topic"},
                Sender = _self,
                SequenceNumber = new byte[] {1, 2, 3, 4, 5, 6, 7, 8},
                DataBytes = new byte[] {0, 1, 0xfe, 0xff}
            };
            var b = new PublishedMessage
            {
                Topics = new[] {"topic"},
                Sender = _other,
                SequenceNumber = new byte[] {1, 2, 3, 4, 5, 6, 7, 8},
                DataBytes = new byte[] {0, 1, 0xfe, 0xff}
            };

            Assert.That(a.MessageId, Is.Not.EqualTo(b.MessageId));
        }

        [Test]
        public void CidNotSupported()
        {
            var _ = new PublishedMessage().Id;
        }

        [Test]
        public void DataStream()
        {
            var msg = new PublishedMessage {DataBytes = new byte[] {1}};
            Assert.That(1, Is.EqualTo(msg.DataStream.ReadByte()));
        }
    }
}
