using System.Collections.Generic;
using Autofac;
using Catalyst.Node.Common;
using Catalyst.Node.Common.Modules;
using Dawn;
using Microsoft.Extensions.Configuration;

namespace Catalyst.Node.Core.Modules.Dfs
{
    public class DfsModule : Module
    {
        private readonly IpfsDfs.ISettings _settings;

        public DfsModule(ushort connectRetries, string apiPath)
        {
            _settings = new IpfsDfs.Settings(connectRetries, apiPath);
        }
        /// <summary>
        /// </summary>
        /// <param name="builder"></param>
        protected override void Load(ContainerBuilder builder)
        {
            Guard.Argument(builder, nameof(builder)).NotNull();
            builder.RegisterType<IpfsConnector>().As<IIpfs>().InstancePerDependency();
            builder.RegisterInstance<IpfsDfs.ISettings>(_settings);
            builder.RegisterType<IpfsDfs>().As<IDfs>().SingleInstance();
        }
    }
}