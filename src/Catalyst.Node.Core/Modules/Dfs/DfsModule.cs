using System;
using System.IO;
using Autofac;
using Autofac.Configuration;
using Dawn;
using Microsoft.Extensions.Configuration;

namespace Catalyst.Node.Core.Modules.Dfs
{
    public class DfsModule : Module
    {
        private readonly IpfsDfs.ISettings _settings;
        private ConfigurationModule _configurationModule;

        public DfsModule(string configFile)
        {
            var configFileFullPath = Path.Combine(Environment.CurrentDirectory, configFile);
            var config = new ConfigurationBuilder()
               .AddJsonFile(configFileFullPath)
               .Build();
            _configurationModule = new ConfigurationModule(config);
        }
        /// <summary>
        /// </summary>
        /// <param name="builder"></param>
        protected override void Load(ContainerBuilder builder)
        {
            Guard.Argument(builder, nameof(builder)).NotNull();
            builder.RegisterModule(_configurationModule);
            //builder.RegisterType<IpfsConnector>().As<Owned<IIpfs>>().InstancePerDependency();
            //builder.RegisterType<IpfsDfs>().As<IDfs>().SingleInstance();
        }
    }
}