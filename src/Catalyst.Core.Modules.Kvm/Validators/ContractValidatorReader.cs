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

using Catalyst.Abstractions.Contract;
using Catalyst.Abstractions.Validators;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace Catalyst.Core.Modules.Kvm.Validators
{
    public class ContractValidatorReader : IValidatorReader
    {
        private readonly IValidatorSetContract _validatorSetContract;

        public ContractValidatorReader(IValidatorSetContract validatorSetContract)
        {
            _validatorSetContract = validatorSetContract;
        }

        public void AddValidatorSet(IList<IValidatorSet> validatorSets, long startBlock, IConfigurationSection configurationSection)
        {
            if (configurationSection.Key.ToLower() == "contract")
            {
                var contractAddress = configurationSection.Value;
                validatorSets.Add(new ContractValidatorSet(_validatorSetContract, startBlock, contractAddress));
            }
        }
    }
}
