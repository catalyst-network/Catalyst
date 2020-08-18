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

using Catalyst.Abstractions.Config;
using Catalyst.Abstractions.Validators;
using Catalyst.Core.Lib.Validators;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;

namespace Catalyst.Core.Lib.Tests.UnitTests.Validators
{
    [TestFixture]
    public class ValidatorSetStoreTests
    {
        [Test]
        public void Can_Read_Validator_Set_From_Store()
        {
            var listValidatorSet = new ListValidatorSet(0, new[] { "0x1a2149b4df5cbac970bc38fecc5237800c688c8b" });
            var validatorSets = new List<IValidatorSet>() { listValidatorSet };

            var validatorSetConfig = Substitute.For<IValidatorSetConfig>();
            validatorSetConfig.GetValidatorSetsAsync().Returns(validatorSets);

            var validatorSetStorer = new ValidatorSetStore(validatorSetConfig);

            validatorSetStorer.Get(0).GetValidators().Should().BeEquivalentTo(listValidatorSet.GetValidators());
        }

        [Test]
        public void Can_Read_Validator_Set_From_Store_At_StartBlock()
        {
            var listValidatorSet1 = new ListValidatorSet(0, new[] { "0x1a2149b4df5cbac970bc38fecc5237800c688c8a" });
            var listValidatorSet2 = new ListValidatorSet(100, new[] { "0x1a2149b4df5cbac970bc38fecc5237800c688c8b", "0x1a2149b4df5cbac970bc38fecc5237800c688c8b" });

            var validatorSets = new List<IValidatorSet>() { listValidatorSet1, listValidatorSet2 };

            var validatorSetConfig = Substitute.For<IValidatorSetConfig>();
            validatorSetConfig.GetValidatorSetsAsync().Returns(validatorSets);

            var validatorSetStorer = new ValidatorSetStore(validatorSetConfig);

            validatorSetStorer.Get(0).GetValidators().Should().BeEquivalentTo(listValidatorSet1.GetValidators());
            validatorSetStorer.Get(50).GetValidators().Should().BeEquivalentTo(listValidatorSet1.GetValidators());

            validatorSetStorer.Get(100).GetValidators().Should().BeEquivalentTo(listValidatorSet2.GetValidators());
            validatorSetStorer.Get(150).GetValidators().Should().BeEquivalentTo(listValidatorSet2.GetValidators());
        }
    }
}
