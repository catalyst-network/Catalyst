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
using System.Linq;
using Autofac;
using Catalyst.Abstractions.Mempool;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Messaging.Correlation;
using Catalyst.Core.Lib.Mempool.Documents;
using Catalyst.TestUtils;
using FluentAssertions;
using Xunit.Abstractions;

namespace Catalyst.Core.Modules.Mempool.Tests.IntegrationTests
{
    public sealed class MempoolTests : FileSystemBasedTest
    {
        public MempoolTests(ITestOutputHelper output) : base(output) { }

        private void Mempool_can_save_and_retrieve(FileInfo mempoolModuleFile)
        {
            ContainerProvider.ConfigureContainerBuilder();

            using (var scope = ContainerProvider.Container.BeginLifetimeScope(mempoolModuleFile))
            {
                var mempool = scope.Resolve<IMempool<MempoolDocument>>();

                var guid = CorrelationId.GenerateCorrelationId().ToString();
                
                // var mempoolDocument = new MempoolDocument {Transaction = TransactionHelper.GetPublicTransaction(signature: guid)};

                mempool.Repository.CreateItem(TransactionHelper.GetPublicTransaction(signature: guid));

                var retrievedTransaction = mempool.Repository.ReadItem(TransactionHelper.GetPublicTransaction(signature: guid).Signature.RawBytes);

                retrievedTransaction.Transaction.Should().Be(TransactionHelper.GetPublicTransaction(signature: guid));
                retrievedTransaction.Transaction.Signature.RawBytes.SequenceEqual(guid.ToUtf8ByteString()).Should().BeTrue();
            }
        }

        // [Fact]
        // [Trait(Traits.TestType, Traits.IntegrationTest)]
        // public void Mempool_with_InMemoryRepo_can_save_and_retrieve()
        // {
        //     var fi = new FileInfo(Path.Combine(Constants.ConfigSubFolder, Constants.ModulesSubFolder,
        //         "mempool.inmemory.json"));
        //     Mempool_can_save_and_retrieve(fi);
        // }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) { }
        }
    }
}
