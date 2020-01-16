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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProtoBuf;

namespace Lib.P2P.Tests.PubSub
{
    [TestClass]
    public class PublishedMessageTest
    {
        private Peer self = new Peer {Id = "QmXK9VBxaXFuuT29AaPUTgW3jBWZ9JgLVZYdMYTHC6LLAH"};
        private Peer other = new Peer {Id = "QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ"};

        [TestMethod]
        public void RoundTrip()
        {
            var a = new PublishedMessage
            {
                Topics = new string[] {"topic"},
                Sender = self,
                SequenceNumber = new byte[] {1, 2, 3, 4, 5, 6, 7, 8},
                DataBytes = new byte[] {0, 1, 0xfe, 0xff}
            };
            var ms = new MemoryStream();
            Serializer.Serialize(ms, a);
            ms.Position = 0;
            var b = Serializer.Deserialize<PublishedMessage>(ms);
            ;

            CollectionAssert.AreEqual(a.Topics.ToArray(), b.Topics.ToArray());
            Assert.AreEqual(a.Sender, b.Sender);
            CollectionAssert.AreEqual(a.SequenceNumber, b.SequenceNumber);
            CollectionAssert.AreEqual(a.DataBytes, b.DataBytes);
            Assert.AreEqual(a.DataBytes.Length, a.Size);
            Assert.AreEqual(b.DataBytes.Length, b.Size);
        }

        [TestMethod]
        public void MessageID_Is_Unique()
        {
            var a = new PublishedMessage
            {
                Topics = new string[] {"topic"},
                Sender = self,
                SequenceNumber = new byte[] {1, 2, 3, 4, 5, 6, 7, 8},
                DataBytes = new byte[] {0, 1, 0xfe, 0xff}
            };
            var b = new PublishedMessage
            {
                Topics = new string[] {"topic"},
                Sender = other,
                SequenceNumber = new byte[] {1, 2, 3, 4, 5, 6, 7, 8},
                DataBytes = new byte[] {0, 1, 0xfe, 0xff}
            };

            Assert.AreNotEqual(a.MessageId, b.MessageId);
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void CidNotSupported()
        {
            var _ = new PublishedMessage().Id;
        }

        [TestMethod]
        public void DataStream()
        {
            var msg = new PublishedMessage {DataBytes = new byte[] {1}};
            Assert.AreEqual(1, msg.DataStream.ReadByte());
        }
    }
}
