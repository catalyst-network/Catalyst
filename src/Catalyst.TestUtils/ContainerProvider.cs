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
using Autofac;
using Autofac.Configuration;
using AutofacSerilogIntegration;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.FileSystem;
using Catalyst.Abstractions.IO.Events;
using Catalyst.Abstractions.Keystore;
using Catalyst.Core.IO.Events;
using Catalyst.Protocol.Interfaces.Validators;
using Catalyst.Protocol.Validators;
using DotNetty.Common.Internal.Logging;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Logging;
using SharpRepository.Ioc.Autofac;
using SharpRepository.Repository;
using Xunit.Abstractions;

namespace Catalyst.TestUtils
{
    public class ContainerProvider : IDisposable
    {
        private readonly IEnumerable<string> _configFilesUsed;
        private readonly IFileSystem _fileSystem;
        private readonly ITestOutputHelper _output;
        private IConfigurationRoot _configRoot;
        public ContainerBuilder ContainerBuilder { get; } = new ContainerBuilder();
    
        private IContainer _container;

        public ContainerProvider(IEnumerable<string> configFilesUsed,
            IFileSystem fileSystem,
            ITestOutputHelper output = null)
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

        public void ConfigureContainerBuilder(bool writeLogsToTestOutput = false, bool writeLogsToFile = false, bool logDotNettyTraffic = false)
        {
            var configurationModule = new ConfigurationModule(ConfigurationRoot);
            ContainerBuilder.RegisterModule(configurationModule);
            ContainerBuilder.RegisterInstance(ConfigurationRoot).As<IConfigurationRoot>();

            var repoFactory =
                RepositoryFactory.BuildSharpRepositoryConfiguation(ConfigurationRoot.GetSection("CatalystNodeConfiguration:PersistenceConfiguration"));
            ContainerBuilder.RegisterSharpRepository(repoFactory);

            var passwordReader = new TestPasswordReader();
            ContainerBuilder.RegisterInstance(passwordReader).As<IPasswordReader>();

            var certificateStore = new TestCertificateStore();
            ContainerBuilder.RegisterInstance(certificateStore).As<ICertificateStore>();
            ContainerBuilder.RegisterInstance(_fileSystem).As<IFileSystem>();

            var keyRegistry = TestKeyRegistry.MockKeyRegistry();
            ContainerBuilder.RegisterInstance(keyRegistry).As<IKeyRegistry>();

            ContainerBuilder.RegisterType<TransactionValidator>().As<ITransactionValidator>();
            ContainerBuilder.RegisterType<TransactionReceivedEvent>().As<ITransactionReceivedEvent>();

            ConfigureLogging(writeLogsToTestOutput, writeLogsToFile, logDotNettyTraffic);
        }

        private void ConfigureLogging(bool writeLogsToTestOutput, bool writeLogsToFile, bool logDotNettyTraffic = false)
        {
            var loggerConfiguration = new LoggerConfiguration()
               .ReadFrom.Configuration(ConfigurationRoot).MinimumLevel.Verbose()
               .Enrich.WithThreadId();

            if (writeLogsToTestOutput)
            {
                if (_output == null)
                {
                    throw new NullReferenceException(
                        $"An instance of {typeof(ITestOutputHelper)} is needed in order to log to the test output");
                }

                loggerConfiguration = loggerConfiguration.WriteTo.TestOutput(_output, LogEventLevel, LogOutputTemplate);
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
                _output?.WriteLine(logFile);
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
