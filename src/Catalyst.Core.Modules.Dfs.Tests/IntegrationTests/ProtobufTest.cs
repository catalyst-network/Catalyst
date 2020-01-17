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
using ProtoBuf;

namespace Catalyst.Core.Modules.Dfs.Tests.IntegrationTests
{
    [ProtoContract]
    internal class M1
    {
        [ProtoMember(1, IsRequired = false)]
        public byte[] Data;
    }

    [ProtoContract]
    internal class M2
    {
        [ProtoMember(1, IsRequired = false)]
        public ArraySegment<byte>? Data;
    }

    // [TestClass]
    // [Ignore("https://github.com/mgravell/protobuf-net/issues/368")]
    // public class ProtobufTest
    // {
    //     [Fact]
    //     public void NullData()
    //     {
    //         var m1 = new M1();
    //         var ms1 = new MemoryStream();
    //         Serializer.Serialize<M1>(ms1, m1);
    //         var bytes1 = ms1.ToArray();
    //
    //         var m2 = new M2();
    //         var ms2 = new MemoryStream();
    //         Serializer.Serialize<M2>(ms2, m2);
    //         var bytes2 = ms2.ToArray();
    //
    //         Assert.Equal(bytes1, bytes2);
    //     }
    //
    //     [Fact]
    //     public void SomeData()
    //     {
    //         var data = new byte[] {10, 11, 12};
    //         var m1 = new M1 {Data = data};
    //         var ms1 = new MemoryStream();
    //         Serializer.Serialize<M1>(ms1, m1);
    //         var bytes1 = ms1.ToArray();
    //
    //         var m2 = new M2 {Data = new ArraySegment<byte>(data)};
    //         var ms2 = new MemoryStream();
    //         Serializer.Serialize<M2>(ms2, m2);
    //         var bytes2 = ms2.ToArray();
    //
    //         Assert.Equal(bytes1, bytes2);
    //     }
    // }
}
