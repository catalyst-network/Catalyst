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
using System.Security;
using Autofac;
using Autofac.Configuration;
using AutofacSerilogIntegration;
using Catalyst.Abstractions;
using Catalyst.Abstractions.Cli;
using Catalyst.Abstractions.Config;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.IO.Events;
using Catalyst.Abstractions.Types;
using Catalyst.Abstractions.Util;
using Catalyst.Core.Config;
using Catalyst.Core.IO.Events;
using Catalyst.Core.Util;
using Catalyst.Protocol.Interfaces.Validators;
using Catalyst.Protocol.Validators;
using Microsoft.Extensions.Configuration;
using Serilog;
using SharpRepository.Ioc.Autofac;
using SharpRepository.Repository;

namespace Catalyst.Core.Kernel
{
    public sealed class Kernel : IDisposable
    {
        private string _withPersistence;
        private bool _overwrite;
        private readonly string _fileName;
        private string _targetConfigFolder;
        private IConfigCopier _configCopier;
        private readonly ConfigurationBuilder _configurationBuilder;
        private ILifetimeScope _instance;

        public ILogger Logger { get; private set; }
        public ContainerBuilder ContainerBuilder { get; set; }
        public ICancellationTokenProvider CancellationTokenProvider { get; }

        public delegate void CustomBootLogic(Kernel kernel);

        public static Kernel Initramfs(bool overwrite = false,
            string fileName = "Catalyst.Node..log",
            ICancellationTokenProvider cancellationTokenProvider = default)
        {
            return new Kernel(cancellationTokenProvider, fileName);
        }

        public void LogUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Logger.Fatal((Exception) e.ExceptionObject, "Unhandled exception, Terminating: {0}", e.IsTerminating);
        }
        
        public void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            CancellationTokenProvider.CancellationTokenSource.Cancel();
        }
        
        private Kernel(ICancellationTokenProvider cancellationTokenProvider, string fileName)
        {
            Logger = new LoggerConfiguration()
               .WriteTo.Console()
               .WriteTo.File(Path.Combine(Path.GetTempPath(), fileName), rollingInterval: RollingInterval.Day)
               .CreateLogger()
               .ForContext(MethodBase.GetCurrentMethod().DeclaringType);

            CancellationTokenProvider = cancellationTokenProvider ?? new CancellationTokenProvider();

            _fileName = fileName;
            ContainerBuilder = new ContainerBuilder();
            _configurationBuilder = new ConfigurationBuilder();
        }

        public Kernel BuildKernel(bool overwrite = false, string overrideNetworkFile = null)
        {
            _overwrite = overwrite;
            _configCopier.RunConfigStartUp(_targetConfigFolder, Protocol.Common.Network.Devnet, null, _overwrite, overrideNetworkFile);
            
            var config = _configurationBuilder.Build();
            var configurationModule = new ConfigurationModule(config);

            ContainerBuilder.RegisterInstance(config);
            
            if (!string.IsNullOrEmpty(_withPersistence))
            {
                var repoFactory = RepositoryFactory.BuildSharpRepositoryConfiguation(config.GetSection(_withPersistence));
                ContainerBuilder.RegisterSharpRepository(repoFactory);
            }
            
            ContainerBuilder.RegisterModule(configurationModule);

            Logger = new LoggerConfiguration()
               .ReadFrom
               .Configuration(configurationModule.Configuration).WriteTo
               .File(Path.Combine(_targetConfigFolder, _fileName),
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] ({MachineName}/{ThreadId}) {Message} ({SourceContext}){NewLine}{Exception}")
               .CreateLogger()
               .ForContext(MethodBase.GetCurrentMethod().DeclaringType);
            ContainerBuilder.RegisterLogger(Logger);

            Log.Logger = Logger;
            
            return this;
        }
        
        public Kernel WithDataDirectory()
        {
            _targetConfigFolder = new FileSystem.FileSystem().GetCatalystDataDir().FullName;
            return this;
        }
        
        public Kernel WithNetworksConfigFile(Protocol.Common.Network network = Protocol.Common.Network.Devnet, string overrideNetworkFile = null)
        {
            var fileName = Constants.NetworkConfigFile(network, overrideNetworkFile);

            _configurationBuilder
               .AddJsonFile(
                    Path.Combine(_targetConfigFolder, fileName)
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

        /// <summary>
        ///     Allows custom nodes to write custom code for containerBuilder
        /// </summary>
        /// <param name="customBootLogic"></param>
        public void StartCustom(CustomBootLogic customBootLogic)
        {
            customBootLogic.Invoke(this);
        }

        /// <summary>
        ///     Default container resolution for Catalyst.Node
        /// </summary>
        public Kernel StartNode()
        {
            ContainerBuilder.RegisterType<TransactionValidator>().As<ITransactionValidator>();
            ContainerBuilder.RegisterType<TransactionReceivedEvent>().As<ITransactionReceivedEvent>();

            StartContainer();
            BsonSerializationProviders.Init();
            _instance.Resolve<ICatalystNode>()
               .RunAsync(CancellationTokenProvider.CancellationTokenSource.Token)
               .Wait(CancellationTokenProvider.CancellationTokenSource.Token);
            return this;
        }

        public void Dispose()
        {
            _instance?.Dispose();
        }

        /// <summary>
        ///     Default container resolution for advanced CLI.
        /// </summary>
        public void StartCli()
        {
            const int bufferSize = 1024 * 67 + 128;

            Console.SetIn(
                new StreamReader(
                    Console.OpenStandardInput(bufferSize),
                    Console.InputEncoding, false, bufferSize
                )
            );

            StartContainer();

            _instance.Resolve<ICatalystCli>()
               .RunConsole(CancellationTokenProvider.CancellationTokenSource.Token);
        }
        
        public Kernel WithPassword(PasswordRegistryTypes types, string password)
        {
            if (password == null)
            {
                return this;
            }

            ContainerBuilder.RegisterBuildCallback(buildCallback =>
            {
                var passwordRegistry = buildCallback.Resolve<IPasswordRegistry>();
                var ss = new SecureString();
                foreach (var c in password)
                {
                    ss.AppendChar(c);
                }

                passwordRegistry.AddItemToRegistry(types, ss);
            });
            
            return this;
        }

        private void StartContainer()
        {
            _instance = ContainerBuilder.Build()
               .BeginLifetimeScope(MethodBase.GetCurrentMethod().DeclaringType.AssemblyQualifiedName);
        }
    }
}
