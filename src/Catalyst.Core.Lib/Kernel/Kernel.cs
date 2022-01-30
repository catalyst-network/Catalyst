#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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
using Catalyst.Abstractions.Ledger.Models;
using Catalyst.Abstractions.Types;
using Catalyst.Abstractions.Util;
using Catalyst.Core.Lib.Config;
using Catalyst.Core.Lib.DAO.Ledger;
using Catalyst.Core.Lib.DAO.Transaction;
using Catalyst.Core.Lib.Util;
using Catalyst.Protocol.Network;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Filters;
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
        private NetworkType _networkType;

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
               .Filter.ByExcluding(Matching.FromSource("Microsoft"))
               .CreateLogger()
               .ForContext(MethodBase.GetCurrentMethod().DeclaringType);

            CancellationTokenProvider = cancellationTokenProvider ?? new CancellationTokenProvider(true);

            _fileName = fileName;
            ContainerBuilder = new ContainerBuilder();
            _configurationBuilder = new ConfigurationBuilder();
        }

        public Kernel BuildKernel(bool overwrite = false, string overrideNetworkFile = null)
        {
            _overwrite = overwrite;
            _configCopier.RunConfigStartUp(_targetConfigFolder, _networkType, null, _overwrite, overrideNetworkFile);
            
            var config = _configurationBuilder.Build();
            ConfigurationModule configurationModule = new(config);

            ContainerBuilder.RegisterInstance(config);
            ContainerBuilder.RegisterType<NetworkTypeProvider>().As<INetworkTypeProvider>()
                .WithParameter("networkType", _networkType).SingleInstance();
            
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
               .Filter.ByExcluding(Matching.FromSource("Microsoft"))
               .Filter.ByExcluding(Matching.FromSource("LibP2P"))
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
        
        public Kernel WithNetworksConfigFile(string overrideNetworkFile = null)
        {
            var fileName = Constants.NetworkConfigFile(_networkType, overrideNetworkFile);

            _configurationBuilder
               .AddJsonFile(
                    Path.Combine(_targetConfigFolder, fileName)
                );

            return this;
        }

        public Kernel WithValidatorSetFile()
        {
            var fileName = Constants.ValidatorSetConfigFile;

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

        //Default network type set
        public Kernel WithNetworkType(NetworkType networkType)
        {
            _networkType = networkType!=NetworkType.Unknown ? networkType : NetworkType.Testnet;
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
                SecureString ss = new();
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

        public Kernel Reset(bool shouldReset)
        {
            if (shouldReset)
            {
                Logger.Information("Resetting State");

                var stateFolder = Path.Join(_targetConfigFolder, "state");
                if (Directory.Exists(stateFolder))
                {
                    Logger.Information("Deleting EVM State");
                    Directory.Delete(stateFolder, true);
                }

                var codeFolder = Path.Join(_targetConfigFolder, "code");
                if (Directory.Exists(codeFolder))
                {
                    Logger.Information("Deleting EVM Code");
                    Directory.Delete(codeFolder, true);
                }

                var blockFolder = Path.Join(_targetConfigFolder, "dfs", "blocks");
                if (Directory.Exists(blockFolder))
                {
                    Logger.Information("Deleting DFS Blocks");
                    Directory.Delete(blockFolder, true);
                }

                var pinFolder = Path.Join(_targetConfigFolder, "dfs", "pins");
                if (Directory.Exists(pinFolder))
                {
                    Logger.Information("Deleting DFS Pins");
                    Directory.Delete(pinFolder, true);
                }

                ContainerBuilder.RegisterBuildCallback(buildCallback =>
                {
                    var deltaIndexes = buildCallback.Resolve<IRepository<DeltaIndexDao, string>>();
                    var transactionReceipts = buildCallback.Resolve<IRepository<TransactionReceipts, string>>();
                    var transactionToDeltas = buildCallback.Resolve<IRepository<TransactionToDelta, string>>();
                    var mempool = buildCallback.Resolve<IRepository<PublicEntryDao, string>>();

                    Logger.Information("Deleting DeltaIndexes");
                    foreach (var deltaIndex in deltaIndexes.GetAll())
                    {
                        deltaIndexes.Delete(deltaIndex);
                    }

                    Logger.Information("Deleting transactionReceipts");
                    foreach (var transactionReceipt in transactionReceipts.GetAll())
                    {
                        transactionReceipts.Delete(transactionReceipt);
                    }

                    Logger.Information("Deleting transactionToDeltas");
                    foreach (var transactionToDelta in transactionToDeltas.GetAll())
                    {
                        transactionToDeltas.Delete(transactionToDelta);
                    }

                    Logger.Information("Deleting mempool");
                    foreach (var mempoolItem in mempool.GetAll())
                    {
                        mempool.Delete(mempoolItem);
                    }
                });
            }

            return this;
        }
        
        public Kernel Uninstall(bool shouldUninstall)
        {
            if (shouldUninstall)
            {
                Logger.Information("Uninstalling Catalyst Node");
                
                new FileSystem.FileSystem().GetCatalystDataDir().Delete(true);
            }

            return this;
        }
    }
}
