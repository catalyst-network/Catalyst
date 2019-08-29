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
using Catalyst.Core.Extensions;
using Catalyst.Core.Util;
using FluentAssertions;
using Multiformats.Base;
using Multiformats.Hash;
using Xunit;

namespace Catalyst.Core.UnitTests.Extensions
{
    public class MultihashExtensionTests
    {
        [Fact]
        public void AsBase32Address_Should_Produce_Parsable_Addresses()
        {
            var random = new Random();
            
            for (var i = 0; i <= 50; i++)
            {
                EnsureBytesRandomBytesCanBeHashedAndReadBack(random.Next(10, 2000));
            }
        }

        private static void EnsureBytesRandomBytesCanBeHashedAndReadBack(int length)
        {
            var bytes = ByteUtil.GenerateRandomByteArray(length);
            var hash = Multihash.Sum(HashType.BLAKE2B_256, bytes);
            var address = hash.AsBase32Address();

            Multihash.TryParse(address, MultibaseEncoding.Base32Lower, out var parsedHash)
               .Should().BeTrue();

            parsedHash.Verify(bytes).Should().BeTrue();
        }
    }
}

