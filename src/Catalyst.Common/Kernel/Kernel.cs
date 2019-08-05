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
using Catalyst.Common.Config;
using Catalyst.Common.Interfaces;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.Interfaces.Config;
using Catalyst.Common.Interfaces.Registry;
using Catalyst.Common.Interfaces.Util;
using Catalyst.Common.Util;
using Microsoft.Extensions.Configuration;
using Serilog;
using SharpRepository.Ioc.Autofac;
using SharpRepository.Repository;

namespace Catalyst.Common.Kernel
{
    public sealed class Kernel : IDisposable
    {
        public ILogger Logger { get; private set; }
        private string _withPersistence;
        private Config.Network _network;
        private bool _overwrite;
        private readonly string _fileName;
        private string _targetConfigFolder;
        private IConfigCopier _configCopier;
        public ContainerBuilder ContainerBuilder { get; set; }
        private readonly ConfigurationBuilder _configurationBuilder;
        private ILifetimeScope _instance;
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

        public Kernel BuildKernel(bool overwrite = false)
        {
            _overwrite = overwrite;
            _configCopier.RunConfigStartUp(_targetConfigFolder, _network, null, _overwrite);
            
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
        public void StartNode()
        {
            if (_instance == null)
            {
                _instance = ContainerBuilder.Build()
                   .BeginLifetimeScope(MethodBase.GetCurrentMethod().DeclaringType.AssemblyQualifiedName);
            }
            
            _instance.Resolve<ICatalystNode>()
               .RunAsync(CancellationTokenProvider.CancellationTokenSource.Token)
               .Wait(CancellationTokenProvider.CancellationTokenSource.Token);
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
            
            if (_instance == null)
            {
                _instance = ContainerBuilder.Build()
                   .BeginLifetimeScope(MethodBase.GetCurrentMethod().DeclaringType.AssemblyQualifiedName);
            }

            _instance.Resolve<ICatalystCli>()
               .RunConsole(CancellationTokenProvider.CancellationTokenSource.Token);
        }
        
        public Kernel WithPasswordOverRide(string certificatePasswordKey = null,
            string ipfsPasswordKey = null,
            string defaultNodePasswordKey = null)
        {
            if (certificatePasswordKey == null && ipfsPasswordKey == null 
             && defaultNodePasswordKey == null)
            {
                return this;
            }

            _instance = ContainerBuilder.Build()
               .BeginLifetimeScope(MethodBase.GetCurrentMethod().DeclaringType.AssemblyQualifiedName);
            
            var passwordRegistry = _instance.Resolve<IPasswordRegistry>();

            if (certificatePasswordKey != null)
            {
                var cpk = new SecureString();
                    
                foreach (var c in certificatePasswordKey)
                {
                    cpk.AppendChar(c);
                }
                    
                passwordRegistry.AddItemToRegistry(PasswordRegistryKey.CertificatePassword, cpk);
            }
                
            if (ipfsPasswordKey != null)
            {
                var ipk = new SecureString();
                    
                foreach (var c in ipfsPasswordKey)
                {
                    ipk.AppendChar(c);
                }
                    
                passwordRegistry.AddItemToRegistry(PasswordRegistryKey.IpfsPassword, ipk);
            }
                              
            if (defaultNodePasswordKey != null)
            {
                var dnpk = new SecureString();
                    
                foreach (var c in defaultNodePasswordKey)
                {
                    dnpk.AppendChar(c);
                }
                    
                passwordRegistry.AddItemToRegistry(PasswordRegistryKey.DefaultNodePassword, dnpk);
            }

            return this;
        }
    }
}
