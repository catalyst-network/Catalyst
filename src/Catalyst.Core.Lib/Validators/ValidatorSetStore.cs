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

using Catalyst.Abstractions.Validators;
using System.Collections.Generic;
using System.Linq;

namespace Catalyst.Core.Lib.Validators
{
    public class ValidatorSetStore : IValidatorSetStore
    {
        private SortedList<int, IValidatorSet> _validatorSets;

        public ValidatorSetStore() => _validatorSets = new SortedList<int, IValidatorSet>();

        public void Add(IValidatorSet validatorSet) => _validatorSets.Add(validatorSet.StartBlock, validatorSet);

        public IValidatorSet Get(int startBlock) => _validatorSets.FirstOrDefault(x => x.Key >= startBlock).Value;
    }
}
