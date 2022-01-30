#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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
using Catalyst.Abstractions.P2P;
using Catalyst.Core.Lib.P2P;
using Catalyst.Core.Modules.Cryptography.BulletProofs;
using Catalyst.TestUtils;
using FluentAssertions;
using MultiFormats;
using NUnit.Framework;

namespace Catalyst.Core.Lib.Tests.UnitTests.P2P
{
    public class PeerIdValidatorTests
    {
        private IPeerIdValidator _peerIdValidator;
        private MultiAddress _validPeerId;

        [SetUp]
        public void Init()
        {
            _validPeerId = MultiAddressHelper.GetAddress();
            _peerIdValidator = new PeerIdValidator(new FfiWrapper());
        }

        [Test]
        public void Can_Validate_PeerId_Format()
        {
            _peerIdValidator.ValidatePeerIdFormat(_validPeerId);

            TestContext.WriteLine(string.Join(" ", _validPeerId));
        }

        [Test]
        public void Can_Throw_Argument_Exception_On_No_Public_Key()
        {
            var invalidPeer = new MultiAddress($"/ip4/77.68.110.78/tcp/4001/");
            new Action(() => _peerIdValidator.ValidatePeerIdFormat(invalidPeer))
               .Should().Throw<ArgumentException>().WithMessage("MultiAddress has no PeerId*");
        }
    }
}
