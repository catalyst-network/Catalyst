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
using System.IO;
using Catalyst.Protocol.Deltas;
using FluentAssertions;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using NUnit.Framework;

namespace Catalyst.Protocol.Tests.UnitTests.Deltas
{
    public sealed class DeltaTests
    {
        private sealed class InvalidDeltas : List<Delta>
        {
            public InvalidDeltas()
            {
                Add(new Delta
                {
                    PreviousDeltaDfsHash = ByteString.Empty,
                    MerkleRoot = ByteString.CopyFromUtf8("abc"),
                    TimeStamp = new Timestamp
                    {
                        Nanos = 21, Seconds = 30
                    }
                });
                Add(new Delta
                {
                    PreviousDeltaDfsHash = ByteString.CopyFromUtf8("abc"),
                    MerkleRoot = ByteString.Empty,
                    TimeStamp = new Timestamp
                    {
                        Nanos = 21, Seconds = 30
                    }
                });
                Add(new Delta
                {
                    PreviousDeltaDfsHash = ByteString.CopyFromUtf8("abc"),
                    MerkleRoot = ByteString.CopyFromUtf8("def"),
                    TimeStamp = new Timestamp()
                });
            }
        }

        [Theory]
        [TestCase(typeof(InvalidDeltas))]
        public void Delta_IsValid_Should_Throw_On_Invalid_Delta(Delta delta)
        {
            new Action(() => delta.IsValid()).Should().Throw<InvalidDataException>();
        }

        [Test]
        public void Delta_IsValid_Should_Not_Throw_On_Valid_Delta()
        {
            var delta = new Delta
            {
                PreviousDeltaDfsHash = ByteString.CopyFromUtf8("good"),
                MerkleRoot = ByteString.CopyFromUtf8("valid"),
                TimeStamp = new Timestamp
                {
                    Nanos = 21, Seconds = 30
                }
            };
            AssertionExtensions.Should(delta.IsValid()).BeTrue();
        }
    }
}
