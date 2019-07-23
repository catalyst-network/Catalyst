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

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Autofac;
using Catalyst.Common.Config;
using Catalyst.Common.Interfaces.Modules.Mempool;
using Catalyst.Common.IO.Messaging.Correlation;
using Catalyst.Common.Modules.Mempool;
using Catalyst.Protocol;
using Catalyst.TestUtils;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.Lib.IntegrationTests.Modules.Mempool
{
    public sealed class MempoolIntegrationTests : ConfigFileBasedTest
    {
        private IEnumerable<string> _configFilesUsed;
        protected override IEnumerable<string> ConfigFilesUsed => _configFilesUsed;

        public MempoolIntegrationTests(ITestOutputHelper output) : base(output) { }

        private async Task Mempool_can_save_and_retrieve(FileInfo mempoolModuleFile)
        {
            var alteredComponentsFile = await CreateAlteredConfigForMempool(mempoolModuleFile);

            _configFilesUsed = new[]
            {
                alteredComponentsFile,
                Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile),
                Path.Combine(Constants.ConfigSubFolder, Constants.NetworkConfigFile(Network.Dev))
            };

            ConfigureContainerBuilder();

            var container = ContainerBuilder.Build();
            using (container.BeginLifetimeScope())
            {
                var mempool = container.Resolve<IMempool>();

                var guid = CorrelationId.GenerateCorrelationId().ToString();
                var mempoolDocument = new MempoolDocument {Transaction = TransactionHelper.GetTransaction(signature: guid)};

                mempool.SaveMempoolDocument(mempoolDocument);

                var retrievedTransaction = mempool.GetMempoolDocument(mempoolDocument.Transaction.Signature);

                retrievedTransaction.Should().Be(mempoolDocument);
                retrievedTransaction.Transaction.Signature.SchnorrSignature.Should().BeEquivalentTo(guid.ToUtf8ByteString());
            }
        }

        private async Task<string> CreateAlteredConfigForMempool(FileInfo mempoolConfigFile)
        {
            var originalContent = await File.ReadAllTextAsync(mempoolConfigFile.FullName);
            var newContent =
                originalContent.Replace("\"Config/Modules/mempool.json\"",
                    JsonConvert.ToString(mempoolConfigFile.FullName));
            var newJsonPath = Path.Combine(FileSystem.GetCatalystDataDir().FullName,
                $"components.{mempoolConfigFile.Name}");
            File.WriteAllText(newJsonPath, newContent);
            return newJsonPath;
        }

        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public async Task Mempool_with_InMemoryRepo_can_save_and_retrieve()
        {
            var fi = new FileInfo(Path.Combine(Constants.ConfigSubFolder, Constants.ModulesSubFolder,
                "mempool.inmemory.json"));
            await Mempool_can_save_and_retrieve(fi);
        }
    }
}
