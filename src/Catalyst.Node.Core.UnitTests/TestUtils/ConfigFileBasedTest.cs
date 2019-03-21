/*
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

using Autofac;
using Autofac.Configuration;
using AutofacSerilogIntegration;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Common.UnitTests.TestUtils;
using Microsoft.Extensions.Configuration;
using Serilog;
using SharpRepository.Ioc.Autofac;
using SharpRepository.Repository;
using Xunit.Abstractions;

namespace Catalyst.Node.Core.UnitTest.TestUtils {
    public class ConfigFileBasedTest : FileSystemBasedTest {

        protected ContainerBuilder ContainerBuilder;
        protected ConfigFileBasedTest(ITestOutputHelper output) : base(output) { }

        protected virtual void ConfigureContainerBuilder(IConfigurationRoot config)
        {
            var configurationModule = new ConfigurationModule(config);
            ContainerBuilder = new ContainerBuilder();
            ContainerBuilder.RegisterModule(configurationModule);

            var loggerConfiguration = new LoggerConfiguration().ReadFrom.Configuration(config);
            Log.Logger = loggerConfiguration.CreateLogger();
            ContainerBuilder.RegisterLogger();

            var repoFactory =
                RepositoryFactory.BuildSharpRepositoryConfiguation(config.GetSection("PersistenceConfiguration"));
            ContainerBuilder.RegisterSharpRepository(repoFactory);

            var passwordReader = new TestPasswordReader();
            ContainerBuilder.RegisterInstance(passwordReader).As<IPasswordReader>();
            
            var certificateStore = new TestCertificateStore();
            ContainerBuilder.RegisterInstance(certificateStore).As<ICertificateStore>();
        }
    }
}