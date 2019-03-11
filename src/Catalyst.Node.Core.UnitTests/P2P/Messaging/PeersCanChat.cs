using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Autofac;
using Autofac.Configuration;
using Autofac.Extensions.DependencyInjection;
using AutofacSerilogIntegration;
using Catalyst.Node.Common.Helpers.Config;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Common.UnitTests.TestUtils;
using Catalyst.Node.Core.P2P.Messaging;
using Catalyst.Node.Core.UnitTest.Modules.Mempool;
using Catalyst.Node.Core.UnitTest.TestUtils;
using DotNetty.Transport.Channels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SharpRepository.Ioc.Autofac;
using SharpRepository.Repository;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Node.Core.UnitTest.P2P.Messaging
{
    public class PeersCanChat : ConfigFileBasedTest
    {
        private readonly IConfigurationRoot _config;

        public PeersCanChat(ITestOutputHelper output) : base(output)
        {
            _config = new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ComponentsJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.NetworkConfigFile(Network.Dev)))
               .Build();
        }

        [Fact]
        public void Peers_Can_Chat()
        {
            ConfigureContainerBuilder(_config);
           
            var serviceCollection = new ServiceCollection();

            var container = ContainerBuilder.Build();
            using (var scope = container.BeginLifetimeScope(_currentTestName,
                //Add .Net Core serviceCollection to the Autofac container.
                b => { b.Populate(serviceCollection, _currentTestName); }))
            {
                var peer1 = container.Resolve<IP2PMessaging>();
                var peer2 = container.Resolve<IP2PMessaging>();

                //peer1.Ping()
            }
        }
    }
}
