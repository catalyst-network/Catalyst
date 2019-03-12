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
        }
    }
}