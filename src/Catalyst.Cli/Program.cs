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
using System.Runtime.Loader;
 
using Autofac;
using Autofac.Configuration;
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
        private static string CatalystSubfolder => ".Catalyst";
        private static string ShellFileName => "shell.json";

        /// <summary>
        ///     Main cli loop
        /// </summary>
        /// <param name="args"></param>
        public static int Main()
        {
            const int bufferSize = 1024 * 67 + 128;
            
            var catalystHomeDirectory = new FileSystem().GetCatalystHomeDir().FullName;
            var network = Network.Dev;

            //Check Catalyst Home directory exists
            if (!Directory.Exists(catalystHomeDirectory))
            {
                Directory.CreateDirectory(catalystHomeDirectory);
            }
            
            //Copy any config files from the local config to Catalyst Home
            /*Copier to be udpated as it is not working correctly*/
            //var configCopier = new ConfigCopier();
            //configCopier.RunConfigStartUp(catalystHomeDirectory, network, AppDomain.CurrentDomain.BaseDirectory,  true);

            // check if user home data dir has a shell config
            var shellFilePath = Path.Combine(catalystHomeDirectory, ShellFileName);
            if (!File.Exists(shellFilePath))
            {
                File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.shell.json"),
                    shellFilePath);
            }

            // resolve config from autofac
            AssemblyLoadContext.Default.Resolving += (context, assembly) =>
                context.LoadFromAssemblyPath(
                    Path.Combine(Directory.GetCurrentDirectory(),
                        $"{assembly.Name}.dll"));

            //Add all config files to the the configuration builder
            var config = new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(shellFilePath))
               .AddJsonFile(Path.Combine(catalystHomeDirectory, Constants.SerilogJsonConfigFile))
               .Build();
            
            
            // register components from config file
            var configurationModule = new ConfigurationModule(config);
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterModule(configurationModule);

            var loggerConfiguration =
                new LoggerConfiguration().ReadFrom.Configuration(configurationModule.Configuration);
            Log.Logger = loggerConfiguration.WriteTo
               .File(Path.Combine(catalystHomeDirectory, "Catalyst.Node..log"), 
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] ({MachineName}/{ThreadId}) {Message} ({SourceContext}){NewLine}{Exception}")
               .CreateLogger();
            containerBuilder.RegisterLogger();

            //var shellModule = new ConfigurationModule(config);

            containerBuilder.RegisterModule(configurationModule);

            var container = containerBuilder.Build();

            Console.SetIn(
                new StreamReader(
                    Console.OpenStandardInput(bufferSize),
                    Console.InputEncoding, false, bufferSize
                )
            );

            var client = container.Resolve<IRPCClient>();
            client.RunClientAsync();
            
            var shell = container.Resolve<IAds>();
            shell.RunConsole();

            
            return 0;
        }
    }
}