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
using Microsoft.Extensions.DependencyInjection;
using Autofac;
using Autofac.Configuration;
using Autofac.Extensions.DependencyInjection;
using AutofacSerilogIntegration;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Common.Helpers.FileSystem;
using Catalyst.Node.Common.Helpers.Config;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Catalyst.Cli
{
    public static class Program
    {
        private static readonly ILogger Logger;
        private static readonly string LifetimeTag;

        static Program()
        {
            var declaringType = MethodBase.GetCurrentMethod().DeclaringType;
            Logger = Log.Logger.ForContext(declaringType);
            LifetimeTag = declaringType.AssemblyQualifiedName;
        }

        /// <summary>
        ///     Main cli loop
        /// </summary>
        /// <param name="args"></param>
        public static int Main()
        {
            Log.Logger.Debug(System.Diagnostics.Process.GetCurrentProcess().Id.ToString());
            const int bufferSize = 1024 * 67 + 128;

            try
            {
                var targetConfigFolder = new FileSystem().GetCatalystHomeDir().FullName;

                new CliConfigCopier().RunConfigStartUp(targetConfigFolder, Network.Dev);

                var config = new ConfigurationBuilder()
                   .AddJsonFile(Path.Combine(targetConfigFolder, Constants.ShellComponentsJsonConfigFile))
                   .AddJsonFile(Path.Combine(targetConfigFolder, Constants.SerilogJsonConfigFile))
                   .AddJsonFile(Path.Combine(targetConfigFolder, Constants.ShellNodesConfigFile))
                   .Build();

                var serviceCollection = new ServiceCollection();

                // register components from config file
                var configurationModule = new ConfigurationModule(config);
                var containerBuilder = new ContainerBuilder();

                containerBuilder.RegisterModule(configurationModule);

                var loggerConfiguration =
                    new LoggerConfiguration().ReadFrom.Configuration(configurationModule.Configuration);
                Log.Logger = loggerConfiguration.WriteTo
                   .File(Path.Combine(targetConfigFolder, "Catalyst.Node..log"),
                        rollingInterval: RollingInterval.Day,
                        outputTemplate:
                        "{Timestamp:HH:mm:ss} [{Level:u3}] ({MachineName}/{ThreadId}) {Message} ({SourceContext}){NewLine}{Exception}")
                   .CreateLogger();

                containerBuilder.RegisterLogger();

                containerBuilder.RegisterInstance(config);

                var container = containerBuilder.Build();

                Console.SetIn(
                    new StreamReader(
                        Console.OpenStandardInput(bufferSize),
                        Console.InputEncoding, false, bufferSize
                    )
                );

                using (var scope = container.BeginLifetimeScope(LifetimeTag,

                    // Add .Net Core serviceCollection to the Autofac container.
                    b => { b.Populate(serviceCollection, LifetimeTag); }))
                {
                    var shell = container.Resolve<ICatalystCli>();

                    shell.Ads.RunConsole();
                }

                Environment.ExitCode = 0;

                return 0;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Catalyst.Node failed to start." + e.Message);
                Environment.ExitCode = 1;
            }
            finally
            {
                Console.ReadLine();
            }

            return Environment.ExitCode;
        }
    }
}
