#region LICENSE

/*
 * Copyright (c) 2019 Catalyst Network
 *
 * This file is part of Catalyst.Cryptography.BulletProofs.Wrapper <https://github.com/catalyst-network/Rust.Cryptography.FFI.Wrapper>
 *
 * Catalyst.Cryptography.BulletProofs.Wrapper is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 2 of the License, or
 * (at your option) any later version.
 * 
 * Catalyst.Cryptography.BulletProofs.Wrapper is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with Catalyst.Cryptography.BulletProofs.Wrapper If not, see <https://www.gnu.org/licenses/>.
*/

#endregion

using System;
using Catalyst.Abstractions.Types;
using FluentAssertions;
using Xunit;

namespace Catalyst.Core.Modules.Cryptography.BulletProofs.Tests
{
    public class TypeTests
    {
        [Fact]
        public void TestLengthByte32()
        {
            var bytes = new Byte32();
            bytes.RawBytes.Length.Should().Be(32);
        }

        [Fact]
        public void TestLengthByte32FromExternal()
        {
            byte[] b = new byte[32];
            var bytes = new Byte32(b);
            bytes.RawBytes.Length.Should().Be(32);
        }

        [Fact]
        public void TestByte32SetFromNull()
        {
            var bytes = new Byte32();
            Action action = () => bytes.RawBytes = null;
            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void TestByte32FromExternalIncorrectLength()
        {
            byte[] b = new byte[64];
            Action action = () => new Byte32(b);
            action.Should().Throw<ArgumentException>("we shouldn't be able to put a byte array of length 64 in a Byte32");
        }

        [Fact]
        public void TestLengthByte32Random()
        {
            Byte32 bytes = Byte32.Random();
            bytes.RawBytes.Length.Should().Be(32);
        }

        [Fact]
        public void TestByte32Random()
        {
            Byte32 bytes1 = Byte32.Random();
            Byte32 bytes2 = Byte32.Random();
            bytes1.RawBytes.Should().NotEqual(bytes2.RawBytes);
        }

        [Fact]
        public void TestLengthByte64()
        {
            var bytes = new Byte64();
            bytes.RawBytes.Length.Should().Be(64);
        }

        [Fact]
        public void TestByte64SetFromNull()
        {
            var bytes = new Byte64();
            Action action = () => bytes.RawBytes = null;
            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void TestLengthByte64FromExternal()
        {
            byte[] b = new byte[64];
            var bytes = new Byte64(b);
            bytes.RawBytes.Length.Should().Be(64);
        }

        [Fact]
        public void TestByte64FromExternalIncorrectLength()
        {
            byte[] b = new byte[32];
            Action action = () => new Byte64(b);
            action.Should().Throw<ArgumentException>("we shouldn't be able to put a byte array of length 32 in a Byte64");
        }

        [Fact]
        public void TestLengthByte64Random()
        {
            Byte64 bytes = Byte64.Random();
            bytes.RawBytes.Length.Should().Be(64);
        }

        [Fact]
        public void TestByte64Random()
        {
            Byte64 bytes1 = Byte64.Random();
            Byte64 bytes2 = Byte64.Random();
            bytes1.RawBytes.Should().NotEqual(bytes2.RawBytes);
        }
    }
}
