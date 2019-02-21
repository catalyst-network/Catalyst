using Autofac;
using Autofac.Configuration;
using Microsoft.Extensions.Configuration;

namespace Catalyst.Node.Core.UnitTest.Modules.Dfs {
    public abstract class BaseModuleConfigTest
    {
        protected IContainer Container;

        public BaseModuleConfigTest(string configFileUnderTest)
        {
            var configuration = new ConfigurationBuilder()
               .AddJsonFile(configFileUnderTest)
               .Build();

            var configurationModule = new ConfigurationModule(configuration);
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterModule(configurationModule);
            Container = containerBuilder.Build();
        }
    }
}