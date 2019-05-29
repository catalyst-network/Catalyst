using System;
using System.Collections.Generic;
using System.Text;
using Catalyst.Common.Config;
using Catalyst.Common.Interfaces.IO;
using DotNetty.Transport.Channels;
using Microsoft.Extensions.Configuration;

namespace Catalyst.Node.Core
{
    public class NodeBusinessEventFactory : INodeBusinessEventFactory
    {
        private readonly IConfigurationRoot _configurationRoot;

        public NodeBusinessEventFactory(IConfigurationRoot configurationRoot)
        {
            _configurationRoot = configurationRoot;
        }

        public IEventLoopGroup NewRpcServerLoopGroup()
        {
            var businessThreads = _configurationRoot
               .GetSection("CatalystNodeConfiguration")
               .GetSection("Rpc")
               .GetValue<int>("ServerBusinessThreads", Constants.BusinessHandlerLogicDefaultThreadCount);

            return new MultithreadEventLoopGroup(businessThreads);
        }

        public IEventLoopGroup NewUdpServerLoopGroup()
        {
            var businessThreads = _configurationRoot
               .GetSection("CatalystNodeConfiguration")
               .GetSection("Peer")
               .GetValue<int>("ServerBusinessThreads", Constants.BusinessHandlerLogicDefaultThreadCount);
            return new MultithreadEventLoopGroup(businessThreads);
        }

        public IEventLoopGroup NewUdpClientLoopGroup()
        {
            var businessThreads = _configurationRoot
               .GetSection("CatalystNodeConfiguration")
               .GetSection("Peer")
               .GetValue<int>("ClientBusinessThreads", Constants.BusinessHandlerLogicDefaultThreadCount);
            return new MultithreadEventLoopGroup(businessThreads);
        }
    }
}
