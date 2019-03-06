using System;
using Autofac;
using Autofac.Configuration;
using Microsoft.Extensions.Configuration;

namespace Catalyst.Node.Core.UnitTest.TestUtils
{
    public abstract class BaseModuleConfigTest
    {
        protected IContainer Container;

        protected BaseModuleConfigTest(string configFileUnderTest, Action<ContainerBuilder> extraRegistrations = null)
        {
            var configuration = new ConfigurationBuilder()
               .AddJsonFile(configFileUnderTest)
               .Build();

            var configurationModule = new ConfigurationModule(configuration);
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterModule(configurationModule);
            extraRegistrations?.Invoke(containerBuilder);
            Container = containerBuilder.Build();
        }
    }
}