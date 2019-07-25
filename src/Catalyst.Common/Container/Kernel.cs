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

using System;
using System.IO;
using System.Reflection;
using Autofac;
using Autofac.Configuration;
using AutofacSerilogIntegration;
using Catalyst.Common.Config;
using Catalyst.Common.Interfaces;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.Interfaces.Config;
using Catalyst.Common.Interfaces.Util;
using Catalyst.Common.Util;
using Microsoft.Extensions.Configuration;
using Serilog;
using SharpRepository.Ioc.Autofac;
using SharpRepository.Repository;

namespace Catalyst.Common.Container
{
    public sealed class Kernel
    {
        public ILogger Logger { get; private set; }
        private string _withPersistence;
        private Config.Network _network;
        private readonly bool _overwrite;
        private readonly string _fileName;
        private string _targetConfigFolder;
        private IConfigCopier _configCopier;
        private readonly ContainerBuilder _containerBuilder;
        private readonly ConfigurationBuilder _configurationBuilder;
        private readonly ICancellationTokenProvider _cancellationTokenProvider;
        
        public static Kernel Initramfs(ICancellationTokenProvider cancellationTokenProvider = default, string fileName = "Catalyst.Node..log", bool overwrite = false)
        {
            return new Kernel(cancellationTokenProvider, fileName, overwrite);
        }

        public void LogUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Logger.Fatal((Exception) e.ExceptionObject, "Unhandled exception, Terminating: {0}", e.IsTerminating);
        }
        
        public void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            _cancellationTokenProvider.CancellationTokenSource.Cancel();
        }
        
        private Kernel(ICancellationTokenProvider cancellationTokenProvider, string fileName, bool overwrite)
        {
            Logger = new LoggerConfiguration()
               .WriteTo.Console()
               .WriteTo.File(Path.Combine(Path.GetTempPath(), fileName), rollingInterval: RollingInterval.Day)
               .CreateLogger()
               .ForContext(MethodBase.GetCurrentMethod().DeclaringType);

            _cancellationTokenProvider = cancellationTokenProvider ?? new CancellationTokenProvider();

            _overwrite = overwrite;
            _fileName = fileName;
            _containerBuilder = new ContainerBuilder();
            _configurationBuilder = new ConfigurationBuilder();
        }

        public Kernel BuildKernel()
        {
            _configCopier.RunConfigStartUp(_targetConfigFolder, _network, null, _overwrite);
            
            var config = _configurationBuilder.Build();
            var configurationModule = new ConfigurationModule(config);

            _containerBuilder.RegisterLogger(Logger);
            _containerBuilder.RegisterInstance(config);
            
            if (!string.IsNullOrEmpty(_withPersistence))
            {
                var repoFactory = RepositoryFactory.BuildSharpRepositoryConfiguation(config.GetSection(_withPersistence));
                _containerBuilder.RegisterSharpRepository(repoFactory);
            }
            
            _containerBuilder.RegisterModule(configurationModule);
            
            Logger = new LoggerConfiguration()
               .ReadFrom
               .Configuration(configurationModule.Configuration).WriteTo
               .File(Path.Combine(_targetConfigFolder, _fileName),
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] ({MachineName}/{ThreadId}) {Message} ({SourceContext}){NewLine}{Exception}")
               .CreateLogger()
               .ForContext(MethodBase.GetCurrentMethod().DeclaringType);
            
            return this;
        }

        public void StartNode()
        {
            using (var instance = _containerBuilder.Build().BeginLifetimeScope(MethodBase.GetCurrentMethod().DeclaringType.AssemblyQualifiedName))
            {
                instance.Resolve<ICatalystNode>()
                   .RunAsync(_cancellationTokenProvider.CancellationTokenSource.Token)
                   .Wait(_cancellationTokenProvider.CancellationTokenSource.Token);
            }
        }

        public void StartCli()
        {
            const int bufferSize = 1024 * 67 + 128;

            Console.SetIn(
                new StreamReader(
                    Console.OpenStandardInput(bufferSize),
                    Console.InputEncoding, false, bufferSize
                )
            );
            
            using (var instance = _containerBuilder.Build().BeginLifetimeScope(MethodBase.GetCurrentMethod().DeclaringType.AssemblyQualifiedName))
            {
                instance.Resolve<ICatalystCli>()
                   .RunConsole(_cancellationTokenProvider.CancellationTokenSource.Token);
            }
        }

        public Kernel WithDataDirectory()
        {
            _targetConfigFolder = new FileSystem.FileSystem().GetCatalystDataDir().FullName;
            return this;
        }

        public Kernel WithNetworksConfigFile(Config.Network network = default)
        {
            _network = network ?? Config.Network.Dev;
            _configurationBuilder
               .AddJsonFile(
                    Path.Combine(_targetConfigFolder, Constants.NetworkConfigFile(_network))
                );

            return this;
        }
        
        public Kernel WithComponentsConfigFile(string components = default)
        {
            _configurationBuilder
               .AddJsonFile(
                    Path.Combine(_targetConfigFolder, components ?? Constants.ComponentsJsonConfigFile)
                );

            return this;
        }
        
        public Kernel WithSerilogConfigFile(string serilog = default)
        {
            _configurationBuilder
               .AddJsonFile(
                    Path.Combine(_targetConfigFolder, serilog ?? Constants.SerilogJsonConfigFile)
                );

            return this;
        }

        public Kernel WithConfigurationFile(string configFileName)
        {
            _configurationBuilder
               .AddJsonFile(
                    Path.Combine(_targetConfigFolder, configFileName)
                );

            return this;
        }

        public Kernel WithConfigCopier(IConfigCopier configCopier = default)
        {
            _configCopier = configCopier ?? new ConfigCopier();
            return this;
        }

        public Kernel WithPersistenceConfiguration(string configSection = null)
        {
            _withPersistence = configSection ?? "CatalystNodeConfiguration:PersistenceConfiguration";

            return this;
        }
    }
}
