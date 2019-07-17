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
using System.Threading;
using Autofac;
using Autofac.Configuration;
using AutofacSerilogIntegration;
using Catalyst.Common.Config;
using Catalyst.Common.FileSystem;
using Catalyst.Common.Util;
using Catalyst.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Serilog;
using SharpRepository.Ioc.Autofac;
using SharpRepository.Repository;
using Constants = Catalyst.Common.Config.Constants;

namespace Catalyst.Node
{
    internal static class Program
    {
        private static ILogger _logger;
        private static readonly string LifetimeTag;
        private static readonly string ExecutionDirectory;
        private static readonly Type DeclaringType;
        private static readonly string LogFileName = "Catalyst.Node..log";

        private static CancellationTokenSource _cancellationSource;

        static Program()
        {
            DeclaringType = MethodBase.GetCurrentMethod().DeclaringType;
            _logger = ConsoleProgram.GetTempLogger(LogFileName, DeclaringType);

            AppDomain.CurrentDomain.UnhandledException +=
                (sender, args) => ConsoleProgram.LogUnhandledException(_logger, sender, args);

            LifetimeTag = DeclaringType.AssemblyQualifiedName;
            ExecutionDirectory = Path.GetDirectoryName(DeclaringType.Assembly.Location);
        }

        public static int Main(string[] args)
        {
            _logger.Information("Catalyst.Node started with process id {0}",
                System.Diagnostics.Process.GetCurrentProcess().Id.ToString());

            _cancellationSource = new CancellationTokenSource();
            try
            {
                var targetConfigFolder = new FileSystem().GetCatalystDataDir().FullName;
                var network = Network.Dev;

#if (DEBUG)                
                new ConfigCopier().RunConfigStartUp(targetConfigFolder, network, overwrite: true);
#elif (RELEASE)
                new ConfigCopier().RunConfigStartUp(targetConfigFolder, network);
#endif
                
                var config = new ConfigurationBuilder()
                   .AddJsonFile(Path.Combine(targetConfigFolder, Constants.NetworkConfigFile(network)))
                   .AddJsonFile(Path.Combine(targetConfigFolder, Constants.ComponentsJsonConfigFile))
                   .AddJsonFile(Path.Combine(targetConfigFolder, Constants.SerilogJsonConfigFile))
                   .Build();

                // register components from config file
                var configurationModule = new ConfigurationModule(config);
                var containerBuilder = new ContainerBuilder();
                containerBuilder.RegisterModule(configurationModule);

                var loggerConfiguration =
                    new LoggerConfiguration().ReadFrom.Configuration(configurationModule.Configuration);

                _logger = loggerConfiguration.WriteTo
                   .File(Path.Combine(targetConfigFolder, LogFileName), 
                        rollingInterval: RollingInterval.Day,
                        outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] ({MachineName}/{ThreadId}) {Message} ({SourceContext}){NewLine}{Exception}")
                   .CreateLogger().ForContext(DeclaringType);

                containerBuilder.RegisterLogger(_logger);
                containerBuilder.RegisterInstance(config);

                var repoFactory = RepositoryFactory.BuildSharpRepositoryConfiguation(config.GetSection("PersistenceConfiguration"));
                containerBuilder.RegisterSharpRepository(repoFactory);

                var container = containerBuilder.Build();
                using (container.BeginLifetimeScope(LifetimeTag))
                {
                    var node = container.Resolve<ICatalystNode>();
                    node.RunAsync(_cancellationSource.Token).Wait(_cancellationSource.Token);
                }

                Environment.ExitCode = 0;
            }
            catch (Exception e)
            {
                _logger.Fatal(e, "Catalyst.Node stopped unexpectedly");
                Environment.ExitCode = 1;
            }

            return Environment.ExitCode;
        }

        static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            _cancellationSource.Cancel();
        }
    }
}
