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
using System.Threading.Tasks;
using Autofac;
using Autofac.Configuration;
using AutofacSerilogIntegration;
using Catalyst.Abstractions.Config;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.Types;
using Catalyst.Abstractions.Util;
using Catalyst.Core.Lib.Config;
using Catalyst.Core.Lib.Util;
using Catalyst.Protocol.Network;
using Microsoft.Extensions.Configuration;
using Serilog;
using SharpRepository.Ioc.Autofac;
using SharpRepository.Repository;

namespace Catalyst.Core.Lib.Kernel
{
    public sealed class Kernel : IDisposable
    {
        private string _withPersistence;
        private bool _overwrite;
        private readonly string _fileName;
        private string _targetConfigFolder;
        private IConfigCopier _configCopier;
        private readonly ConfigurationBuilder _configurationBuilder;
        public ILifetimeScope Instance;

        public ILogger Logger { get; private set; }
        public ContainerBuilder ContainerBuilder { get; set; }
        public ICancellationTokenProvider CancellationTokenProvider { get; }

        public delegate Task CustomBootLogic(Kernel kernel);

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
            _configCopier.RunConfigStartUp(_targetConfigFolder, NetworkType.Devnet, null, _overwrite, overrideNetworkFile);
            
            var config = _configurationBuilder.Build();
            var configurationModule = new ConfigurationModule(config);

            ContainerBuilder.RegisterInstance(config);
            ContainerBuilder.RegisterModule(configurationModule);
            
            if (!string.IsNullOrEmpty(_withPersistence))
            {
                var repoFactory = RepositoryFactory.BuildSharpRepositoryConfiguation(config.GetSection(_withPersistence));
                ContainerBuilder.RegisterSharpRepository(repoFactory);
            }

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
        
        public Kernel WithNetworksConfigFile(NetworkType networkType = NetworkType.Devnet, string overrideNetworkFile = null)
        {
            var fileName = Constants.NetworkConfigFile(networkType, overrideNetworkFile);

            _configurationBuilder
               .AddJsonFile(
                    Path.Combine(_targetConfigFolder, fileName)
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

        public Kernel WithConfigCopier(IConfigCopier configCopier)
        {
            _configCopier = configCopier;
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
        public async Task StartCustomAsync(CustomBootLogic customBootLogic)
        {
            await customBootLogic.Invoke(this).ConfigureAwait(false);
        }

        public void Dispose()
        {
            Instance?.Dispose();
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

        public void StartContainer()
        {
            var declaringType = MethodBase.GetCurrentMethod().DeclaringType;
            if (declaringType != null) 
            {
                Instance = ContainerBuilder.Build()
                   .BeginLifetimeScope(declaringType.AssemblyQualifiedName);
            }
        }
    }
}
