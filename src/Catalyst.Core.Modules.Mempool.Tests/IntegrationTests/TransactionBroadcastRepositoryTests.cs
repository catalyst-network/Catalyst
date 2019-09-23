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

using Autofac;
using Catalyst.Abstractions.DAO;
using Catalyst.Core.Lib.DAO;
using Catalyst.Core.Lib.Repository;
using Catalyst.TestUtils;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SharpRepository.InMemoryRepository;
using SharpRepository.MongoDbRepository;
using SharpRepository.Repository;
using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;
namespace Catalyst.Core.Modules.Mempool.Tests.IntegrationTests
{
    public sealed class TransactionBroadcastRepositoryTests : FileSystemBasedTest
    {
        public static IEnumerable<object[]> ModulesList =>
            new List<object[]>
            {
                new object[] {new InMemoryModule()},
                new object[] {new MongoDbModule()}
            };

        private sealed class MongoDbModule : Module
        {
            protected override void Load(ContainerBuilder builder)
            {
                builder.RegisterType<MongoDbRepository<TransactionBroadcastDao>>().As<IRepository<TransactionBroadcastDao, string>>().SingleInstance();
            }
        }

        private sealed class InMemoryModule : Module
        {
            protected override void Load(ContainerBuilder builder)
            {
                builder.RegisterType<InMemoryRepository<TransactionBroadcastDao, string>>().As<IRepository<TransactionBroadcastDao, string>>().SingleInstance();
            }
        }

        private sealed class ModuleAzureSqlTypes : Module
        {
            private readonly string _connectionString;
            public ModuleAzureSqlTypes(string connectionString) { _connectionString = connectionString; }

            protected override void Load(ContainerBuilder builder)
            {
                builder.Register(c => new EfCoreContext(_connectionString)).AsImplementedInterfaces().AsSelf()
                   .InstancePerLifetimeScope();

                builder.RegisterType<MempoolEfCoreRepository>().As<IRepository<TransactionBroadcastDao, string>>().SingleInstance();
            }
        }

        public TransactionBroadcastRepositoryTests(ITestOutputHelper output) : base(output)
        {
            var mappers = new IMapperInitializer[]
            {
                new ProtocolMessageDao(),
                new ConfidentialEntryDao(),
                new ProtocolErrorMessageSignedDao(),
                new PeerIdDao(),
                new SigningContextDao(),
                new CoinbaseEntryDao(),
                new PublicEntryDao(),
                new ConfidentialEntryDao(),
                new TransactionBroadcastDao(),
                new RangeProofDao(),
                new ContractEntryDao(),
                new SignatureDao(),
                new BaseEntryDao(),
            };

            var map = new MapperProvider(mappers);
            map.Start();
        }

        private void TransactionBroadcastRepo_Can_Save_And_Retrieve()
        {
            using (var scope = ContainerProvider.Container.BeginLifetimeScope(CurrentTestName))
            {
                var criteriaId = string.Empty;
                var peerRepo = PopulateTransactBroadcastRepo(scope, out criteriaId);

                peerRepo.Get(criteriaId).Id.Should().Be(criteriaId);
            }
        }

        private IRepository<TransactionBroadcastDao, string> PopulateTransactBroadcastRepo(ILifetimeScope scope, out string Id)
        {
            var transactBroadcastRepo = scope.Resolve<IRepository<TransactionBroadcastDao, string>>();

            var transactionBroadcastDao = new TransactionBroadcastDao().ToDao(TransactionHelper.GetPublicTransaction());
            transactionBroadcastDao.Id = Guid.NewGuid().ToString();
            Id = transactionBroadcastDao.Id;

            transactBroadcastRepo.Add(transactionBroadcastDao);

            return transactBroadcastRepo;
        }

        [Theory(Skip = "To be run in the pipeline only")]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        [MemberData(nameof(ModulesList))]
        public void TransactionBroadcastRepo_All_Dbs_Can_Update_And_Retrieve(Module dbModule)
        {
            RegisterModules(dbModule);
        }

        [Theory(Skip = "To be run in the pipeline only")]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        [MemberData(nameof(ModulesList))]
        public void TransactionBroadcastRepo_All_Dbs_Can_Save_And_Retrieve(Module dbModule)
        {
            RegisterModules(dbModule);

            TransactionBroadcastRepo_Can_Save_And_Retrieve();
        }

        [Fact(Skip = "Microsoft DBs yet to be completed")]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void TransactionBroadcastRepo_Microsoft_SQLTypes_Dbs_Update_And_Retrieve()
        {
            var connectionStr = ContainerProvider.ConfigurationRoot
               .GetSection("CatalystNodeConfiguration:PersistenceConfiguration:repositories:efCore:connectionString").Value;

            RegisterModules(new ModuleAzureSqlTypes(connectionStr));

            CheckForDatabaseCreation();
        }

        [Fact(Skip = "Microsoft DBs yet to be completed")]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void TransactionBroadcastRepo_Microsoft_SQLTypes_Dbs_Can_Save_And_Retrieve()
        {
            var connectionStr = ContainerProvider.ConfigurationRoot
               .GetSection("CatalystNodeConfiguration:PersistenceConfiguration:repositories:efCore:connectionString").Value;

            RegisterModules(new ModuleAzureSqlTypes(connectionStr));

            CheckForDatabaseCreation();

            TransactionBroadcastRepo_Can_Save_And_Retrieve();
        }

        private void CheckForDatabaseCreation()
        {
            using (var scope = ContainerProvider.Container.BeginLifetimeScope(CurrentTestName))
            {
                var contextDb = scope.Resolve<IDbContext>();

                ((DbContext) contextDb).Database.EnsureCreated();
            }
        }

        private void RegisterModules(Module module)
        {
            ContainerProvider.ConfigureContainerBuilder();

            ContainerProvider.ContainerBuilder.RegisterModule(module);
        }
    }
}
