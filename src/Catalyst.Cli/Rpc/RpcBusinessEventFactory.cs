using System;
using System.Collections.Generic;
using System.Text;
using Catalyst.Common.Config;
using Catalyst.Common.Interfaces.IO;
using DotNetty.Transport.Channels;
using Microsoft.Extensions.Configuration;

namespace Catalyst.Cli.Rpc
{
    public class RpcBusinessEventFactory : IRpcBusinessEventFactory
    {
        private readonly IConfigurationRoot _configurationRoot;

        public RpcBusinessEventFactory(IConfigurationRoot configurationRoot) { _configurationRoot = configurationRoot; }

        public IEventLoopGroup NewRpcClientLoopGroup()
        {
            var threadCount = _configurationRoot.GetSection("CatalystCliRpcNodes")
               .GetValue<int>("BusinessThreads", Constants.BusinessHandlerLogicDefaultThreadCount);
            return new MultithreadEventLoopGroup(threadCount);
        }
    }
}
