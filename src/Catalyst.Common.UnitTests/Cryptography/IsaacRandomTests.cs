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
using Catalyst.Common.Cryptography;
using Catalyst.Common.Interfaces.Cryptography;
using Xunit;

namespace Catalyst.Common.UnitTests.Cryptography
{
    public class IsaacRandomTests
    {
        [Theory]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(100000)]
        public void Test_Deterministic_Algorithm(uint sequenceSize)
        {
            var seed = Guid.NewGuid().ToString();
            uint[] sequence = new uint[sequenceSize];
            uint[] duplicateSequence = new uint[sequenceSize];

            IsaacRandom cipher = new IsaacRandom(seed);
            for (int i = 0; i < sequenceSize; i++)
            {
                sequence[i] = cipher.NextInt();
            }

            IDeterministicRandom cipherClone = new IsaacRandom(seed);
            for (int i = 0; i < sequenceSize; i++)
            {
                duplicateSequence[i] = cipherClone.NextInt();
            }

            Assert.True(sequence.SequenceEqual(duplicateSequence));
        }
    }
}
