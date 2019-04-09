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
 using System.Threading.Tasks;
using Autofac;
 using Catalyst.Node.Common.Helpers;
 using Catalyst.Node.Common.Helpers.Config;
using Catalyst.Node.Common.Interfaces.Modules.Mempool;
using Catalyst.Node.Common.UnitTests.TestUtils;
 using Catalyst.Node.Core.UnitTest.TestUtils;
 using FluentAssertions;
 using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Node.Core.UnitTest.Modules.Mempool
{
    public class MempoolIntegrationTests : ConfigFileBasedTest
    {
        public MempoolIntegrationTests(ITestOutputHelper output) : base(output) { }

        private async Task Mempool_can_save_and_retrieve(FileInfo mempoolModuleFile)
        {
            var alteredComponentsFile = await CreateAlteredConfigForMempool(mempoolModuleFile);

            var config = new ConfigurationBuilder()
               .AddJsonFile(alteredComponentsFile)
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.NetworkConfigFile(Network.Dev)))
               .Build();

            ConfigureContainerBuilder(config);

            var container = ContainerBuilder.Build();

            using (var scope = container.BeginLifetimeScope())
            {
                var mempool = container.Resolve<IMempool>();

                var guid = Guid.NewGuid().ToString();
                var transactionToSave = TransactionHelper.GetTransaction(signature: guid);
                var signature = transactionToSave.Signature;

                mempool.SaveTransaction(transactionToSave);

                var retrievedTransaction = mempool.GetTransaction(transactionToSave.Signature);

                retrievedTransaction.Should().Be(transactionToSave);
                retrievedTransaction.Signature.SchnorrSignature.Should().BeEquivalentTo(guid.ToUtf8ByteString());
            }
        }


        private async Task<string> CreateAlteredConfigForMempool(FileInfo mempoolConfigFile)
        {
            var originalContent = await File.ReadAllTextAsync(mempoolConfigFile.FullName);
            var newContent =
                originalContent.Replace("\"Config/Modules/mempool.json\"",
                    JsonConvert.ToString(mempoolConfigFile.FullName));
            var newJsonPath = Path.Combine(FileSystem.GetCatalystHomeDir().FullName,
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
                originalContent.Replace("[@replace-this@]", FileSystem.GetCatalystHomeDir().Name);
            var jsonTestingFile = Path.Combine(FileSystem.GetCatalystHomeDir().FullName, mempoolConfigFile);
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
