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
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Catalyst.Core.Lib.Config
{
    public class ValidatorSetConfig : IValidatorSetConfig
    {
        private readonly IConfigurationRoot _configuration;
        private readonly IEnumerable<IValidatorReader> _validatorReaders;

        public ValidatorSetConfig(IConfigurationRoot configuration, IEnumerable<IValidatorReader> validatorReaders)
        {
            _configuration = configuration;
            _validatorReaders = validatorReaders;
        }

        public async Task<IEnumerable<IValidatorSet>> GetValidatorSetsAsync()
        {
            var validatorSets = new List<IValidatorSet>();
            var multi = _configuration.GetSection("validators:multi");
            var validatorSetAtStartBlocks = multi.GetChildren();

            foreach (var validatorSetAtStartBlock in validatorSetAtStartBlocks)
            {
                foreach (var validatorReader in _validatorReaders)
                {
                    if (validatorSetAtStartBlock.Key == null)
                    {
                        continue;
                    }

                    var startBlock = long.Parse(validatorSetAtStartBlock.Key);
                    var property = validatorSetAtStartBlock.GetChildren().FirstOrDefault();
                    validatorReader.AddValidatorSet(validatorSets, startBlock, property);
                }
            }

            return validatorSets;
        }
    }
}
