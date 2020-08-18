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

using Autofac;
using Autofac.Configuration;
using Catalyst.Abstractions.Config;
using Catalyst.Abstractions.Validators;
using Catalyst.Core.Lib.Config;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.Validators;
using Catalyst.Core.Modules.Kvm.Validators;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace Catalyst.Core.Lib.Tests.UnitTests.Config
{
    [TestFixture]
    public class ValidatorsConfigTests
    {
        private readonly string _configJson = @"{
            ""validators"": {
                ""multi"": {
                    ""0"": {
                            ""list"": [ ""0x1a2149b4df5cbac970bc38fecc5237800c688c8b"" ]
                        },
                    ""900"": {
                            ""contract"": ""0x79dd7e4c1b9adb07f71b54dba2d54db2fa549de3""
                        }
                    }
                }
            }";

        private IContainer _container;

        [SetUp]
        public void Init()
        {
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddJsonStream(_configJson.ToMemoryStream());

            var config = configurationBuilder.Build();
            var configModule = new ConfigurationModule(config);

            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterInstance(config);
            containerBuilder.RegisterModule(configModule);

            containerBuilder.RegisterType<ValidatorSetConfig>().As<IValidatorSetConfig>().SingleInstance();

            //Readers
            containerBuilder.RegisterType<ListValidatorReader>().As<IValidatorReader>();
            containerBuilder.RegisterType<ContractValidatorReader>().As<IValidatorReader>();

            //Store
            containerBuilder.RegisterType<ValidatorSetStore>().As<IValidatorSetStore>().SingleInstance();

            _container = containerBuilder.Build();
        }

        [Test]
        public void Can_Parse_Validator_Json_File()
        {
            using (var scope = _container.BeginLifetimeScope())
            {
                var validatorSetStore = scope.Resolve<IValidatorSetStore>();
                var listSet = validatorSetStore.Get(0);
                var contractSet = validatorSetStore.Get(900);

                listSet.Should().BeOfType<ListValidatorSet>();
                contractSet.Should().BeOfType<ContractValidatorSet>();
            }
        }

        [Test]
        public void ValidatorSetStore_Can_Return_Correct_Validation_Set_At_Start_Block()
        {
            using (var scope = _container.BeginLifetimeScope())
            {
                var validatorSetStore = scope.Resolve<IValidatorSetStore>();

                var listSet = validatorSetStore.Get(0);
                listSet.Should().BeAssignableTo<ListValidatorSet>();

                var listSet2 = validatorSetStore.Get(450);
                listSet2.Should().BeAssignableTo<ListValidatorSet>();

                var contractSet = validatorSetStore.Get(900);
                contractSet.Should().BeAssignableTo<ContractValidatorSet>();

                var contractSet2 = validatorSetStore.Get(1350);
                contractSet2.Should().BeAssignableTo<ContractValidatorSet>();
            }
        }
    }
}
