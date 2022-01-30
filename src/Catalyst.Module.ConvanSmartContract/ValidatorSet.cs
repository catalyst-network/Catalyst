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
using Nethermind.Core;

namespace Catalyst.Module.ConvanSmartContract
{
    public class ValidatorSet
    {
        private readonly IValidatorSetContract _validatorSetContract;
        private readonly Address _contractAddress;

        public ValidatorSet(IValidatorSetContract validatorSetContract, Address contractAddress)
        {
            _validatorSetContract = validatorSetContract;
            _contractAddress = contractAddress;
        }

        /// <summary>
        /// Get current validator set (last enacted or initial if no changes ever made)
        /// function getValidators() constant returns (address[] _validators);
        /// </summary>
        public Address[] GetValidators()
        {
            return _validatorSetContract.GetValidators(_contractAddress);
        }
    }
}
