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

using Catalyst.Core.Modules.Cryptography.BulletProofs;
using Catalyst.Core.Modules.Hashing;
using MultiFormats.Registry;
using Nethermind.Core.Specs;
using Nethermind.Evm;
using Nethermind.Logging;
using Nethermind.State;
using NSubstitute;
using NUnit.Framework;

namespace Catalyst.Core.Modules.Kvm.Tests.UnitTests
{
    [TestFixture]
    public sealed class CatalystVirtualMachineTests
    {
        [Test]
        public void Catalyst_virtual_machine_can_be_initialized()
        {
            var virtualMachine = new KatVirtualMachine(
                Substitute.For<IWorldState>(),
                Substitute.For<IBlockhashProvider>(),
                Substitute.For<ISpecProvider>(),
                new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("keccak-256")),
                new FfiWrapper(), 
                LimboLogs.Instance);
            Assert.That(virtualMachine, Is.Not.Null);
        }
    }
}
