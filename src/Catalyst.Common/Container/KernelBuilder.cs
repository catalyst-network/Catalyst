#region LICENSE

/**
* Copyright (c) 2019 Catalyst Network
*
* This file is part of Catalyst.Node <https://github.com/catalyst-network/Catalyst.Node>
*
* Catalyst.Node is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 2 of the License, or
* (at your option) any later version.
*
* Catalyst.Node is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with Catalyst.Node. If not, see <https://www.gnu.org/licenses/>.
*/

#endregion

using System.IO;
using System.Reflection;
using Autofac;
using Autofac.Configuration;
using AutofacSerilogIntegration;
using Catalyst.Common.Config;
using Catalyst.Common.Interfaces.Config;
using Catalyst.Common.Interfaces.Util;
using Catalyst.Common.Util;
using Microsoft.Extensions.Configuration;
using Serilog;
using SharpRepository.Ioc.Autofac;
using SharpRepository.Repository;

namespace Catalyst.Common.Container
{
    public sealed class KernelBuilder
    {
        public readonly ICancellationTokenProvider CancellationTokenProvider;
        private readonly string _fileName;
        private Config.Network _network;
        private string _targetConfigFolder;
        private IConfigCopier _configCopier;
        private readonly ContainerBuilder _containerBuilder;
        private readonly ConfigurationBuilder _configurationBuilder;
        public ILogger Logger;
        private bool _withPersistence;
        private ConfigurationModule _configurationModule;

        public static KernelBuilder GetContainerBuilder(ICancellationTokenProvider cancellationTokenProvider = default, string fileName = "Catalyst.Node..log")
        {
            return new KernelBuilder(cancellationTokenProvider, fileName);
        }

        private KernelBuilder(ICancellationTokenProvider cancellationTokenProvider, string fileName)
        {
            CancellationTokenProvider = cancellationTokenProvider ?? new CancellationTokenProvider();
            _fileName = fileName;
            Logger = ConsoleProgram.GetTempLogger(_fileName, MethodBase.GetCurrentMethod().DeclaringType);
            _containerBuilder = new ContainerBuilder();
            _configurationBuilder = new ConfigurationBuilder();
        }

        public IContainer BuildContainer()
        {
            _configCopier.RunConfigStartUp(_targetConfigFolder, _network, overwrite: true);
            
            var config = _configurationBuilder.Build();
            _configurationModule = new ConfigurationModule(config);

            _containerBuilder.RegisterLogger(Logger);
            _containerBuilder.RegisterInstance(config);

            if (_withPersistence)
            {
                // told you
                var repoFactory = RepositoryFactory.BuildSharpRepositoryConfiguation(config.GetSection("CatalystNodeConfiguration:PersistenceConfiguration"));
                _containerBuilder.RegisterSharpRepository(repoFactory);
            }
            
            _containerBuilder.RegisterModule(_configurationModule);
            
            Logger = new LoggerConfiguration()
               .ReadFrom
               .Configuration(_configurationModule.Configuration).WriteTo
               .File(Path.Combine(_targetConfigFolder, _fileName),
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] ({MachineName}/{ThreadId}) {Message} ({SourceContext}){NewLine}{Exception}")
               .CreateLogger()
               .ForContext(MethodBase.GetCurrentMethod().DeclaringType);
            
            return _containerBuilder.Build();
        }

        public KernelBuilder WithDataDirectory()
        {
            _targetConfigFolder = new FileSystem.FileSystem().GetCatalystDataDir().FullName;
            return this;
        }

        public KernelBuilder WithNetworksConfigFile(Config.Network network = default)
        {
            _network = network ?? Config.Network.Dev;
            _configurationBuilder
               .AddJsonFile(
                    Path.Combine(_targetConfigFolder, Constants.NetworkConfigFile(_network))
                );

            return this;
        }
        
        public KernelBuilder WithComponentsConfigFile(string components = default)
        {
            _configurationBuilder
               .AddJsonFile(
                    Path.Combine(_targetConfigFolder, components ?? Constants.ComponentsJsonConfigFile)
                );

            return this;
        }
        
        public KernelBuilder WithSerilogConfigFile(string serilog = default)
        {
            _configurationBuilder
               .AddJsonFile(
                    Path.Combine(_targetConfigFolder, serilog ?? Constants.SerilogJsonConfigFile)
                );

            return this;
        }

        public KernelBuilder WithConfigurationFile(string configFileName)
        {
            _configurationBuilder
               .AddJsonFile(
                    Path.Combine(_targetConfigFolder, configFileName)
                );

            return this;
        }

        public KernelBuilder WithConfigCopier(IConfigCopier configCopier = default)
        {
            _configCopier = configCopier ?? new ConfigCopier();
            return this;
        }

        public KernelBuilder WithPersistenceConfiguration()
        {
            _withPersistence = true; // I know this is gross

            return this;
        }
    }
}
