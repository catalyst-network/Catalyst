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
using Catalyst.Abstractions.Validators;
using Catalyst.Core.Lib.Validators;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Catalyst.Node.POA.CE.Tests.UnitTests.Config
{
    [TestFixture]
    public class ValidatorsConfigTests
    {
        [Test]
        public void Can_Parse_Validator_Json_File()
        {
            var validatorsJson = File.ReadAllText("Config/validators.json");
            var validatorsJObj = JObject.Parse(validatorsJson);
            var multiSets = (JObject)validatorsJObj.GetValue("multi");

            var containerBuilder = new ContainerBuilder();

            //Store
            containerBuilder.RegisterType<ValidatorSetStore>().As<IValidatorSetStore>().SingleInstance();

            //Readers
            containerBuilder.RegisterType<ListValidatorReader>().As<IValidatorReader>();
            containerBuilder.RegisterType<ContractValidatorReader>().As<IValidatorReader>();

            //Validator
            //containerBuilder.RegisterType<Validators>().As<Validators>();

            var container = containerBuilder.Build();
            using (var scope = container.BeginLifetimeScope())
            {
                var validatorSetStore = scope.Resolve<IValidatorSetStore>();
                var listSet = validatorSetStore.Get(0).GetValidators();
                var contractSet = validatorSetStore.Get(900).GetValidators();
                //validators.ReadValidatorSets(multiSets);

                //var listSet = validators.GetValidators(0);
                //var contractSet = validators.GetValidators(900);

                var c = 0;
            }
        }
    }
}
