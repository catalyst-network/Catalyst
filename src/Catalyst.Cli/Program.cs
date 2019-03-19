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
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
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
        private static readonly string ExecutionDirectory;
        private static CancellationTokenSource _cancellationSource;
        
        static Program()
        {
            var declaringType = MethodBase.GetCurrentMethod().DeclaringType;
            Logger = Log.Logger.ForContext(declaringType);
            LifetimeTag = declaringType.AssemblyQualifiedName;
            ExecutionDirectory = Path.GetDirectoryName(declaringType.Assembly.Location);
        }

        /// <summary>
        ///     Main cli loop
        /// </summary>
        /// <param name="args"></param>
        public static int Main()
        {
            const int bufferSize = 1024 * 67 + 128;
            _cancellationSource = new CancellationTokenSource();

            try
            {
                var targetConfigFolder = new FileSystem().GetCatalystHomeDir().FullName;
                
                // check if user home data dir has a shell config
                var shellFilePath = Path.Combine(targetConfigFolder, Constants.ShellConfig);
                var shellComponentsFilePath = Path.Combine(targetConfigFolder, Constants.ShellComponentsJsonConfigFile);
                var shellSeriLogFilePath = Path.Combine(targetConfigFolder, Constants.ShellSerilogJsonConfigFile);

                if (!File.Exists(shellFilePath))
                {
                    File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/shell.json"),
                        shellFilePath);
                    File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/shell.components.json"),
                        shellComponentsFilePath);
                    File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/shell.serilog.json"),
                        shellSeriLogFilePath);
                }
                
                var config = new ConfigurationBuilder()
                   .AddJsonFile(Path.Combine(targetConfigFolder, Constants.ShellConfig))
                   .AddJsonFile(Path.Combine(targetConfigFolder, Constants.ShellComponentsJsonConfigFile))
                   .AddJsonFile(Path.Combine(targetConfigFolder, Constants.ShellSerilogJsonConfigFile))
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
                        outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] ({MachineName}/{ThreadId}) {Message} ({SourceContext}){NewLine}{Exception}")
                   .CreateLogger();
            
                containerBuilder.RegisterLogger();

                var container = containerBuilder.Build();
                
                Console.SetIn(
                    new StreamReader(
                        Console.OpenStandardInput(bufferSize),
                        Console.InputEncoding, false, bufferSize
                    )
                );
                
                using (var scope = container.BeginLifetimeScope(LifetimeTag,
                    //Add .Net Core serviceCollection to the Autofac container.
                    b => { b.Populate(serviceCollection, LifetimeTag); }))
                {
                    var shell = container.Resolve<ICatalystCli>();
                    shell._aRunConsole();
                }
                
                Environment.ExitCode = 0;
            
                return 0;
            }
            catch (Exception e)
            {
                Log.Logger.Error(e, "Catalyst.Node failed to start.");
                Environment.ExitCode = 1;
            }

            return Environment.ExitCode;
        }
    }
}