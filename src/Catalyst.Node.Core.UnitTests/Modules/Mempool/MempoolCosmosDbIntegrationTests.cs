using System.IO;
using Autofac;
using Catalyst.Common.Config;
using Catalyst.Common.Interfaces.Modules.Mempool;
using Catalyst.Common.UnitTests.TestUtils;
using Catalyst.Node.Core.UnitTest.TestUtils;
using Catalyst.Protocol.Transaction;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Serilog;
using SharpRepository.Ioc.Autofac;
using SharpRepository.Repository.Ioc;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Node.Core.UnitTest.Modules.Mempool
{
    public class MempoolCosmosDbIntegrationTests : ConfigFileBasedTest
    {
        private readonly ILifetimeScope _scope;
        private readonly IMempool _memPool;
        private readonly Transaction _transaction;
        
        public MempoolCosmosDbIntegrationTests(ITestOutputHelper output) : base(output)
        {
            var config = SocketPortHelper.AlterConfigurationToGetUniquePort(new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ComponentsJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.NetworkConfigFile(Network.Dev)))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ShellNodesConfigFile))
               .Build(), CurrentTestName);
            
            ConfigureContainerBuilder(config);

            var container = ContainerBuilder.Build();
            _scope = container.BeginLifetimeScope(CurrentTestName);
            RepositoryDependencyResolver.SetDependencyResolver(new AutofacRepositoryDependencyResolver(container));
            
            _memPool = container.Resolve<IMempool>();
            
            var logger = Substitute.For<ILogger>();
            
            _transaction = TransactionHelper.GetTransaction();
        }
        
        [Fact]
        public void AddTransaction_UsingRepository_ShouldBeAddedToRepository()
        {
            _memPool.SaveTransaction(_transaction);

            var transaction = _memPool.GetTransaction(_transaction.Signature);
        }
    }
}
