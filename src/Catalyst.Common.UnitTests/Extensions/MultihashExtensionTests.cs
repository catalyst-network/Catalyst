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

using Catalyst.Common.Extensions;
using Catalyst.Common.Util;
using FluentAssertions;
using Multiformats.Base;
using Multiformats.Hash;
using NSubstitute;
using Xunit;

namespace Catalyst.Common.UnitTests.Extensions
{
    public class MultihashExtensionTests
    {
        [Fact]
        public void MyTestedMethod_Should_Be_Producing_This_Result_When_Some_Conditions_Are_Met()
        {
            var bytes = ByteUtil.GenerateRandomByteArray(345);
            var hash = Multihash.Sum(HashType.BLAKE2B_256, bytes);
            var address = hash.AsBase32Address();
            Multihash.TryParse(address, MultibaseEncoding.Base32Lower, out var mh).Should().BeTrue();
        }
    }
}

