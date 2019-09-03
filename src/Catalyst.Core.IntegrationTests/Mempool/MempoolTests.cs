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

using System.IO;
using System.Threading.Tasks;
using Autofac;
using Catalyst.Abstractions.Mempool;
using Catalyst.Core.Config;
using Catalyst.Core.IO.Messaging.Correlation;
using Catalyst.Core.Mempool.Documents;
using Catalyst.Protocol;
using Catalyst.TestUtils;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.IntegrationTests.Mempool
{
    public sealed class MempoolIntegrationTests : FileSystemBasedTest
    {
        private ContainerProvider _containerProvider;
        public MempoolIntegrationTests(ITestOutputHelper output) : base(output) { }

        private async Task Mempool_can_save_and_retrieve(FileInfo mempoolModuleFile)
        {
            var alteredComponentsFile = await CreateAlteredConfigForMempool(mempoolModuleFile);

            var configFilesUsed = new[]
            {
                alteredComponentsFile,
                Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile),
                Path.Combine(Constants.ConfigSubFolder, Constants.NetworkConfigFile(Protocol.Common.Network.Devnet))
            };

            _containerProvider = new ContainerProvider(configFilesUsed, FileSystem, Output);

            _containerProvider.ConfigureContainerBuilder();

            using (var scope = _containerProvider.Container.BeginLifetimeScope(mempoolModuleFile))
            {
                var mempool = scope.Resolve<IMempool<MempoolDocument>>();

                var guid = CorrelationId.GenerateCorrelationId().ToString();
                
                // var mempoolDocument = new MempoolDocument {Transaction = TransactionHelper.GetTransaction(signature: guid)};

                mempool.Repository.CreateItem(TransactionHelper.GetTransaction(signature: guid));

                var retrievedTransaction = mempool.Repository.ReadItem(TransactionHelper.GetTransaction(signature: guid).Signature);

                retrievedTransaction.Transaction.Should().Be(TransactionHelper.GetTransaction(signature: guid));
                retrievedTransaction.Transaction.Signature.SchnorrSignature.Should().BeEquivalentTo(guid.ToUtf8ByteString());
            }
        }

        private async Task<string> CreateAlteredConfigForMempool(FileInfo mempoolConfigFile)
        {
            var originalContent = await File.ReadAllTextAsync(mempoolConfigFile.FullName);
            var newContent =
                originalContent.Replace("\"Config.mempool.json\"",
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

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
            {
                return;
            }

            _containerProvider?.Dispose();
        }
    }
}
