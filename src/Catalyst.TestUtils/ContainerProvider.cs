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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using Autofac;
using Autofac.Configuration;
using AutofacSerilogIntegration;
using Catalyst.Abstractions.Cli;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.FileSystem;
using Catalyst.Abstractions.Keystore;
using Catalyst.Core.Lib;
using Catalyst.Core.Lib.Cryptography;
using Catalyst.Core.Lib.FileSystem;
using Catalyst.Core.Modules.Cryptography.BulletProofs;
using Catalyst.Core.Modules.Dfs;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Core.Modules.KeySigner;
using Catalyst.Core.Modules.Keystore;
using DotNetty.Common.Internal.Logging;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Serilog.Filters;
using SharpRepository.Ioc.Autofac;
using SharpRepository.Repository;

namespace Catalyst.TestUtils
{
    public class ContainerProvider : IDisposable
    {
        private readonly IEnumerable<string> _configFilesUsed;
        private readonly IFileSystem _fileSystem;
        private readonly TestContext _output;
        private IConfigurationRoot _configRoot;
        public ContainerBuilder ContainerBuilder { get; } = new ContainerBuilder();
        private IContainer _container;

        public ContainerProvider(IEnumerable<string> configFilesUsed,
            IFileSystem fileSystem,
            TestContext output)
        {
            _configFilesUsed = configFilesUsed;
            _fileSystem = fileSystem;
            _output = output;
        }

        protected string LogOutputTemplate { get; set; } =
            "{Timestamp:HH:mm:ss} [{Level:u3}] ({ThreadId}) {Message} ({SourceContext}){NewLine}{Exception}";

        protected LogEventLevel LogEventLevel { get; set; } = LogEventLevel.Verbose;

        public IConfigurationRoot ConfigurationRoot
        {
            get
            {
                if (_configRoot != null)
                {
                    return _configRoot;
                }

                var configBuilder = new ConfigurationBuilder();
                _configFilesUsed.ToList().ForEach(f => configBuilder.AddJsonFile(f));

                _configRoot = configBuilder.Build();
                return _configRoot;
            }
        }

        public IContainer Container => _container ?? (_container = ContainerBuilder.Build());

        public void ConfigureContainerBuilder(bool writeLogsToTestOutput = false,
            bool writeLogsToFile = false,
            bool logDotNettyTraffic = false)
        {
            SocketPortHelper.AlterConfigurationToGetUniquePort(ConfigurationRoot);

            var configurationModule = new ConfigurationModule(ConfigurationRoot);
            ContainerBuilder.RegisterModule(configurationModule);
            ContainerBuilder.RegisterModule(new CoreLibProvider());
            ContainerBuilder.RegisterInstance(ConfigurationRoot).As<IConfigurationRoot>();

            var repoFactory =
                RepositoryFactory.BuildSharpRepositoryConfiguation(
                    ConfigurationRoot.GetSection("CatalystNodeConfiguration:PersistenceConfiguration"));
            ContainerBuilder.RegisterSharpRepository(repoFactory);

            var passwordReader = new TestPasswordReader();
            ContainerBuilder.RegisterInstance(passwordReader).As<IPasswordReader>();

            var certificateStore = new TestCertificateStore();
            ContainerBuilder.RegisterInstance(certificateStore).As<ICertificateStore>();
            ContainerBuilder.RegisterInstance(_fileSystem).As<IFileSystem>();

            var keyRegistry = TestKeyRegistry.MockKeyRegistry();
            ContainerBuilder.RegisterInstance(keyRegistry).As<IKeyRegistry>();

            ContainerBuilder.RegisterModule(new DfsModule());
            ContainerBuilder.RegisterModule(new BulletProofsModule());
            ContainerBuilder.RegisterModule(new KeystoreModule());
            ContainerBuilder.RegisterModule(new KeySignerModule());
            ContainerBuilder.RegisterModule(new HashingModule());

            //var keyStore = TestKeyRegistry.MockKeyFileStore();
            //ContainerBuilder.RegisterInstance(keyStore).As<IStore<string, EncryptedKey>>().SingleInstance();

            var inMemoryStore = new InMemoryStore<string, EncryptedKey>();
            ContainerBuilder.RegisterInstance(inMemoryStore).As<IStore<string, EncryptedKey>>().SingleInstance();

            //ContainerBuilder.RegisterBuildCallback(async x =>
            //{
            //    var keyApi = x.Resolve<IKeyApi>();
            //    await keyApi.SetPassphraseAsync(new SecureString());
            //    var key = await keyApi.GetKeyAsync(KeyRegistryTypes.DefaultKey);
            //    if (key == null)
            //    {
            //        await keyApi.CreateAsync(KeyRegistryTypes.DefaultKey, "ed25519", 0);
            //    }
            //});
            

            ConfigureLogging(writeLogsToTestOutput, writeLogsToFile, logDotNettyTraffic);
        }

        private void ConfigureLogging(bool writeLogsToTestOutput, bool writeLogsToFile, bool logDotNettyTraffic = false, bool logAspTraffic = false)
        {
            var loggerConfiguration = new LoggerConfiguration()
               .ReadFrom.Configuration(ConfigurationRoot).MinimumLevel.Verbose()
               .Enrich.WithThreadId();

            if (!logAspTraffic)
            {
                loggerConfiguration = loggerConfiguration.Filter.ByExcluding(Matching.FromSource("Microsoft"));
            }

            if (writeLogsToTestOutput)
            {
                if (_output == null)
                {
                    throw new NullReferenceException(
                        $"An instance of {typeof(TestContext)} is needed in order to log to the test output");
                }

                loggerConfiguration = loggerConfiguration.WriteTo.NUnitOutput(LogEventLevel, null, null, LogOutputTemplate);
            }

            var logFile = Path.Combine(_fileSystem.GetCatalystDataDir().FullName, "Catalyst.Node.log");
            if (writeLogsToFile)
            {
                loggerConfiguration = loggerConfiguration.WriteTo.File(logFile, LogEventLevel, LogOutputTemplate);
            }

            var logger = loggerConfiguration.CreateLogger();
            ContainerBuilder.RegisterLogger(logger);

            if (Log.Logger == Logger.None)
            {
                Log.Logger = logger;
            }

            if (logDotNettyTraffic)
            {
                InternalLoggerFactory.DefaultFactory.AddProvider(new SerilogLoggerProvider(logger));
            }

            if (writeLogsToFile)
            {
                logger.Information(logFile);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _container?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
