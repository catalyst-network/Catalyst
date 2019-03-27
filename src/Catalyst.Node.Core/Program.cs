/*
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

 using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
 using System.Threading;
 using Autofac;
using Autofac.Configuration;
using Autofac.Extensions.DependencyInjection;
using AutofacSerilogIntegration;
using Catalyst.Node.Common.Helpers.Config;
using Catalyst.Node.Common.Helpers.FileSystem;
using Catalyst.Node.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SharpRepository.Ioc.Autofac;
using SharpRepository.Repository;

namespace Catalyst.Node.Core
{
    public static class Program
    {
        private static readonly ILogger Logger;
        private static readonly string LifetimeTag;
        private static readonly string ExecutionDirectory;
        private static CancellationTokenSource _cancellationSource;

        static Program()
        {
            var declaringType = MethodBase.GetCurrentMethod().DeclaringType;
            Logger = Log.Logger.ForContext(declaringType);
            LifetimeTag = declaringType.AssemblyQualifiedName;
            ExecutionDirectory = Path.GetDirectoryName(declaringType.Assembly.Location);
        }

        public static int Main(string[] args)
        {
            _cancellationSource = new CancellationTokenSource();
            try
            {
                //Enable after checking safety implications, if plugins become important.
                // AssemblyLoadContext.Default.Resolving += TryLoadAssemblyFromExecutionDirectory;

                var targetConfigFolder = new FileSystem().GetCatalystHomeDir().FullName;
                var network = Network.Dev;

                ConfigCopier.RunConfigStartUp(targetConfigFolder, network, overwrite: true);

                var config = new ConfigurationBuilder()
                   .AddJsonFile(Path.Combine(targetConfigFolder, Constants.NetworkConfigFile(network)))
                   .AddJsonFile(Path.Combine(targetConfigFolder, Constants.ComponentsJsonConfigFile))
                   .AddJsonFile(Path.Combine(targetConfigFolder, Constants.SerilogJsonConfigFile))
                   .Build();

                //.Net Core service collection
                var serviceCollection = new ServiceCollection();
                //Add .Net Core services (if any) first
                //serviceCollection.AddLogging().AddDistributedMemoryCache();

                // register components from config file
                var configurationModule = new ConfigurationModule(config);
                var containerBuilder = new ContainerBuilder();
                containerBuilder.RegisterModule(configurationModule);

                var loggerConfiguration =
                    new LoggerConfiguration().ReadFrom.Configuration(configurationModule.Configuration);
                Log.Logger = loggerConfiguration.WriteTo
                   .File(Path.Combine(targetConfigFolder, "Catalyst.Node..log"), 
                        rollingInterval: RollingInterval.Day,
                        outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] ({MachineName}/{ThreadId}) {Message} ({SourceContext}){NewLine}{Exception}")
                   .CreateLogger();
                containerBuilder.RegisterLogger();

                var repoFactory = RepositoryFactory.BuildSharpRepositoryConfiguation(config.GetSection("PersistenceConfiguration"));
                containerBuilder.RegisterSharpRepository(repoFactory);

                containerBuilder.RegisterInstance(config);

                var container = containerBuilder.Build();

                using (var scope = container.BeginLifetimeScope(LifetimeTag,
                    //Add .Net Core serviceCollection to the Autofac container.
                    b => { b.Populate(serviceCollection, LifetimeTag); }))
                {
                   var node = container.Resolve<ICatalystNode>();
                   node.RunAsync(_cancellationSource.Token).Wait(_cancellationSource.Token);
                }

                Environment.ExitCode = 0;
            }
            catch (Exception e)
            {
                Log.Logger.Error(e, "Catalyst.Node failed to start.");
                Environment.ExitCode = 1;
            }

            return Environment.ExitCode;
        }

        static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            _cancellationSource.Cancel();
        }

        public static Assembly TryLoadAssemblyFromExecutionDirectory(AssemblyLoadContext context,
            AssemblyName assemblyName)
        {
            try
            {
                var assemblyFilePath = Path.Combine(ExecutionDirectory, $"{assemblyName.Name}.dll");
                Logger.Debug("Resolving assembly {0} from file {1}", assemblyName, assemblyFilePath);
                var assembly = context.LoadFromAssemblyPath(assemblyFilePath);
                return assembly;
            }
            catch (Exception e)
            {
                Logger.Warning(e, "Failed to load assembly {0} from file {1}.", e);
                return null;
            }
        }

        public static void LogUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                Log.Logger.Fatal("Unhandled exception, Terminating", e);
            }
            catch
            {
                using (var fs = new FileStream("error.log", FileMode.Create, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(fs))
                {
                    writer.WriteLine(e.ExceptionObject.ToString());
                    writer.WriteLine($"IsTerminating: {e.IsTerminating}");
                }
            }
        }
    }
}