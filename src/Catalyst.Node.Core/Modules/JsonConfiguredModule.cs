using System;
using System.IO;
using Autofac;
using Autofac.Configuration;
using Dawn;
using Microsoft.Extensions.Configuration;

namespace Catalyst.Node.Core.Modules
{
    public class JsonConfiguredModule : Module
    {
        private readonly ConfigurationModule _configurationModule;

        public JsonConfiguredModule(string configFilePath)
        {
            var configFileFullPath = Path.Combine(Environment.CurrentDirectory, configFilePath);
            var config = new ConfigurationBuilder()
               .AddJsonFile(configFileFullPath)
               .Build();
            _configurationModule = new ConfigurationModule(config);
        }

        /// <inheritdoc />
        protected override void Load(ContainerBuilder builder)
        {
            Guard.Argument(builder, nameof(builder)).NotNull();
            builder.RegisterModule(_configurationModule);
        }
    }
}
