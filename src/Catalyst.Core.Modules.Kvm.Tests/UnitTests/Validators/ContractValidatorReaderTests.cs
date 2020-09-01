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
using Catalyst.Core.Modules.Kvm.Validators;
using FluentAssertions;
using NUnit.Framework;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Catalyst.Abstractions.Contract;
using NSubstitute;

namespace Catalyst.Core.Modules.Kvm.Tests.UnitTests.Validators
{
    [TestFixture]
    public class ContractValidatorReaderTests
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

            //Convert json to memory stream, there is a extension in Catalyst.Core.Lib.Extensions but
            //because of circular reference it will need to be refactored and Catalyst.Core.Lib.Extensions
            //functionality should be in its own class lib.
            var memoryStream = new MemoryStream();
            var streamWriter = new StreamWriter(memoryStream);
            streamWriter.Write(json);
            streamWriter.Flush();
            memoryStream.Position = 0;

            //Build config from json memory stream
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddJsonStream(memoryStream);
            var config = configurationBuilder.Build();

            var validatorSets = new List<IValidatorSet>();

            var multi = config.GetSection("validators:multi");
            var validatorSetAtStartBlocks = multi.GetChildren();

            var validatorReader = new ContractValidatorReader(Substitute.For<IValidatorSetContract>());

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

            validatorSets.Count.Should().Be(1);
        }
    }
}
