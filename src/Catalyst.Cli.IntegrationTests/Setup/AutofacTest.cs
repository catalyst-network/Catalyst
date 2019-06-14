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

using System;
using Autofac;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.TestUtils;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using System.IO;
using Autofac.Configuration;
using Autofac.Extensions.DependencyInjection;
using Catalyst.Common.FileSystem;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using AutofacSerilogIntegration;
using Catalyst.Common.Config;
using Constants = Catalyst.Common.Config.Constants;

namespace Catalyst.Cli.IntegrationTests.Commands
{
    public sealed class AutofacTest
    {
        private static string LifetimeTag => MethodBase.GetCurrentMethod().DeclaringType.AssemblyQualifiedName;

        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void Cli_Autofac_Startup_Resolve_Test()
        {
            var msg = string.Empty;
            var serviceCollection = new ServiceCollection();

            try
            {
                var targetConfigFolder = new FileSystem().GetCatalystDataDir().FullName;

                new CliConfigCopier().RunConfigStartUp(targetConfigFolder, Network.Dev, overwrite: true);

                var config = new ConfigurationBuilder()
                   .AddJsonFile(Path.Combine(targetConfigFolder, Constants.ShellComponentsJsonConfigFile))
                   .AddJsonFile(Path.Combine(targetConfigFolder, Constants.SerilogJsonConfigFile))
                   .AddJsonFile(Path.Combine(targetConfigFolder, Constants.ShellNodesConfigFile))
                   .AddJsonFile(Path.Combine(targetConfigFolder, Constants.ShellConfigFile))
                   .Build();

                // register components from config file
                var configurationModule = new ConfigurationModule(config);
                var containerBuilder = new ContainerBuilder();

                containerBuilder.RegisterModule(configurationModule);

                containerBuilder.RegisterLogger(null);
                containerBuilder.RegisterInstance(config);

                var container = containerBuilder.Build();

                // Add .Net Core serviceCollection to the Autofac container.
                using (container.BeginLifetimeScope(LifetimeTag, b => 
                { b.Populate(serviceCollection, LifetimeTag); }))
                {
                    var shell = container.Resolve<ICatalystCli>();
                }
            }
            catch (Exception e)
            {
                msg = e.Message;
            }

            msg.Should().Be(string.Empty);
        }
    }
}
