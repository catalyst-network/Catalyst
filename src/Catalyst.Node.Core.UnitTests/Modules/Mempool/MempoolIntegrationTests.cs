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

﻿using System;
using System.IO;
using System.Threading.Tasks;
using Autofac;
using Autofac.Configuration;
using AutofacSerilogIntegration;
using Catalyst.Node.Common.Helpers.Config;
 using Catalyst.Node.Common.Interfaces.Modules.Mempool;
 using Catalyst.Node.Common.UnitTests.TestUtils;
using Catalyst.Node.Core.Modules.Mempool;
using Catalyst.Protocols.Transaction;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Serilog;
using SharpRepository.Ioc.Autofac;
using SharpRepository.Repository;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Node.Core.UnitTest.Modules.Mempool
{
    public class MempoolIntegrationTests : FileSystemBasedTest
    {
        public MempoolIntegrationTests(ITestOutputHelper output) : base(output) { }
        private ContainerBuilder _containerBuilder;

        private async Task Mempool_can_save_and_retrieve(FileInfo mempoolModuleFile)
        {
            var alteredComponentsFile = await CreateAlteredConfigForMempool(mempoolModuleFile);

            var config = new ConfigurationBuilder()
               .AddJsonFile(alteredComponentsFile)
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.NetworkConfigFile(Network.Dev)))
               .Build();
            ConfigureContainerBuilder(config);

            var container = _containerBuilder.Build();

            using (var scope = container.BeginLifetimeScope())
            {
                var mempool = container.Resolve<IMempool>();

                var guid = Guid.NewGuid().ToString();
                var transactionToSave = GetStTxModel(signature: guid);

                mempool.SaveTx(transactionToSave.Key, transactionToSave.Transaction);

                var retrievedTransaction = mempool.GetTx(transactionToSave.Key);

                retrievedTransaction.Should().Be(transactionToSave.Transaction);
                retrievedTransaction.Signature.Should().Be(guid);
            }
        }

        private StTxModel GetStTxModel(uint amount = 1, string signature = "signature")
        {
            var key = new Key {HashedSignature = "hashed_signature"};
            var transaction = new StTx
            {
                Amount = amount,
                Signature = signature,
                AddressDest = "address_dest",
                AddressSource = "address_source",
                Updated = new StTx.Types.Timestamp {Nanos = 100, Seconds = 30}
            };
            return new StTxModel {Key = key, Transaction = transaction};
        }

        private void ConfigureContainerBuilder(IConfigurationRoot config)
        {
            var configurationModule = new ConfigurationModule(config);
            _containerBuilder = new ContainerBuilder();
            _containerBuilder.RegisterModule(configurationModule);

            var loggerConfiguration = new LoggerConfiguration().ReadFrom.Configuration(config);
            Log.Logger = loggerConfiguration.CreateLogger();
            _containerBuilder.RegisterLogger();

            var repoFactory =
                RepositoryFactory.BuildSharpRepositoryConfiguation(config.GetSection("PersistenceConfiguration"));
            _containerBuilder.RegisterSharpRepository(repoFactory);
        }

        private async Task<string> CreateAlteredConfigForMempool(FileInfo mempoolConfigFile)
        {
            var originalContent = await File.ReadAllTextAsync(mempoolConfigFile.FullName);
            var newContent =
                originalContent.Replace("\"Config/Modules/mempool.json\"",
                    JsonConvert.ToString(mempoolConfigFile.FullName));
            var newJsonPath = Path.Combine(_fileSystem.GetCatalystHomeDir().FullName,
                $"components.{mempoolConfigFile.Name}");
            File.WriteAllText(newJsonPath, newContent);
            return newJsonPath;
        }

        private async Task<string> PointXmlConfigToLocalTestFolder(string mempoolConfigFile)
        {
            var originalContent = await
                File.ReadAllTextAsync(Path.Combine(Constants.ConfigSubFolder, Constants.ModulesSubFolder,
                    mempoolConfigFile));
            var newContent =
                originalContent.Replace("[@replace-this@]", _fileSystem.GetCatalystHomeDir().Name);
            var jsonTestingFile = Path.Combine(_fileSystem.GetCatalystHomeDir().FullName, mempoolConfigFile);
            File.WriteAllText(jsonTestingFile, newContent);
            return jsonTestingFile;
        }

        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public async Task Mempool_with_InMemoryRepo_can_save_and_retrieve()
        {
            var fi = new FileInfo(Path.Combine(Constants.ConfigSubFolder, Constants.ModulesSubFolder,
                "mempool.inmemory.json"));
            await Mempool_can_save_and_retrieve(fi);
        }

        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public async Task Mempool_with_XmlRepo_can_save_and_retrieve()
        {
            var mempoolConfigFile = "mempool.xml.json";
            var resultFile = await PointXmlConfigToLocalTestFolder(mempoolConfigFile);
            await Mempool_can_save_and_retrieve(new FileInfo(resultFile));
        }
    }
}