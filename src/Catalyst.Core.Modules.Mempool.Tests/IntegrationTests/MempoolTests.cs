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
using System.Linq;
using Autofac;
using Catalyst.Abstractions.Mempool;
using Catalyst.Abstractions.Mempool.Repositories;
using Catalyst.Abstractions.P2P;
using Catalyst.Core.Lib.DAO;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Messaging.Correlation;
using Catalyst.Core.Lib.Mempool.Documents;
using Catalyst.Core.Modules.Mempool.Repositories;
using Catalyst.Protocol.Deltas;
using Catalyst.TestUtils;
using Catalyst.Modules.Repository.CosmosDb;
//using Catalyst.Modules.Repository.MongoDb;
using FluentAssertions;
using NSubstitute;
using SharpRepository.EfCoreRepository;
using SharpRepository.InMemoryRepository;
using SharpRepository.MongoDbRepository;
using SharpRepository.Repository;
using SharpRepository.Repository.Caching;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.Modules.Mempool.Tests.IntegrationTests
{
    public sealed class MempoolTests : FileSystemBasedTest
    {
        public static IEnumerable<object[]> ModulesList =>
            new List<object[]>
            {
                new object[] {new MempoolModuleAzureSqlTypes()}
               //,
               // new object[] {new MempoolModuleCosmosDb()},
               // new object[] {new MempoolModuleMongoDb()}
            };

        private class MempoolModuleInMemory : Module
        {
            protected override void Load(ContainerBuilder builder)
            {
                builder.Register(c => new InMemoryRepository<MempoolDocument, string>())
                   .As<IRepository<MempoolDocument, string>>()
                   .SingleInstance();
                builder.RegisterType<MempoolDocumentRepository>().As<IMempoolRepository<MempoolDocument>>().SingleInstance();
                builder.RegisterType<Mempool>().As<IMempool<MempoolDocument>>().SingleInstance();
            }
        }

        private sealed class MempoolModuleMongoDb : Module
        {
            protected override void Load(ContainerBuilder builder)
            {
                builder.Register(c => new MongoDbRepository<MempoolDocument>(
                    c.ResolveOptional<ICachingStrategy<MempoolDocument, string>>()
                )).As<IRepository<MempoolDocument, string>>().SingleInstance();
            }
        }

        private sealed class MempoolModuleCosmosDb : Module
        {
            protected override void Load(ContainerBuilder builder)
            {
                builder.Register(c => 
                        new CosmosDbRepository<MempoolDocument>("", "", "", true))
                   .As<IRepository<MempoolDocument, string>>()
                   .SingleInstance();
                builder.RegisterType<MempoolDocumentRepository>().As<IMempoolRepository<MempoolDocument>>().SingleInstance();
                builder.RegisterType<Mempool>().As<IMempool<MempoolDocument>>().SingleInstance();
            }
        }

        private sealed class MempoolModuleAzureSqlTypes : Module
        {
            protected override void Load(ContainerBuilder builder)
            {
                builder.Register(c => new InMemoryRepository<MempoolDocument, string>())
                   .As<IRepository<MempoolDocument, string>>()
                   .SingleInstance();
                builder.RegisterType<MempoolDocumentRepository>().As<IMempoolRepository<MempoolDocument>>().SingleInstance();
                builder.RegisterType<Mempool>().As<IMempool<MempoolDocument>>().SingleInstance();
            }
        }

        public MempoolTests(ITestOutputHelper output) : base(output) { }

        private void Mempool_Can_Save_And_Retrieve(Module mempoolModule)
        {
            ContainerProvider.ConfigureContainerBuilder();
            ContainerProvider.ContainerBuilder.RegisterModule(mempoolModule);

            using (var scope = ContainerProvider.Container.BeginLifetimeScope(CurrentTestName))
            {
                var mempool = scope.Resolve<IMempool<MempoolDocument>>();

                var guid = CorrelationId.GenerateCorrelationId().ToString();

                //mempool.Repository.CreateItem(TransactionHelper.GetTransaction(signature: guid));

                //var retrievedTransaction = mempool.Repository.ReadItem(TransactionHelper.GetTransaction(signature: guid).Signature);

                //retrievedTransaction.Transaction.Should().Be(TransactionHelper.GetTransaction(signature: guid));
                //retrievedTransaction.Transaction.Signature.SequenceEqual(guid.ToUtf8ByteString()).Should().BeTrue();
            }
        }
        
        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void Mempool_with_InMemoryRepo_Can_Save_And_Retrieve()
        {
            Mempool_Can_Save_And_Retrieve(new MempoolModuleInMemory());
        }

        //[Theory(Skip = "To be run in the pipeline only")]
        [Theory]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        [MemberData(nameof(ModulesList))]
        public void Mempool_ExternalDbs_Can_Save_And_Retrieve(Module dbModule)
        {
            Mempool_Can_Save_And_Retrieve(dbModule);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) { }
        }
    }
}
