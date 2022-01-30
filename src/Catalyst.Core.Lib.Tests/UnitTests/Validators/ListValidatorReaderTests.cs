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

using Catalyst.Abstractions.Validators;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.Validators;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Catalyst.Core.Lib.Tests.UnitTests.Validators
{
    [TestFixture]
    public class ListValidatorReaderTests
    {
        [Test]
        public void Can_Parse_Validator_List()
        {
            var json = @"{
            ""validators"": {
                ""multi"": {
                    ""0"": {
                            ""list"": [ ""0x1a2149b4df5cbac970bc38fecc5237800c688c8b"" ]
                        },
                    ""1"": {
                            ""list"": [ ""0x1a2149b4df5cbac970bc38fecc5237800c688c8c"" ]
                        },
                    ""2"": {
                            ""contract"": ""0x79dd7e4c1b9adb07f71b54dba2d54db2fa549de3""
                        }
                    }
                }
            }";

            var validatorSets = new List<IValidatorSet>();

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddJsonStream(json.ToMemoryStream());

            var config = configurationBuilder.Build();

            var multi = config.GetSection("validators:multi");
            var validatorSetAtStartBlocks = multi.GetChildren();

            var validatorReader = new ListValidatorReader();

            foreach (var validatorSetAtStartBlock in validatorSetAtStartBlocks)
            {
                if (validatorSetAtStartBlock.Key == null)
                {
                    continue;
                }

                var startBlock = long.Parse(validatorSetAtStartBlock.Key);
                var property = validatorSetAtStartBlock.GetChildren().FirstOrDefault();
                validatorReader.AddValidatorSet(validatorSets, startBlock, property);
            }

            validatorSets.Count.Should().Be(2);
        }
    }
}
