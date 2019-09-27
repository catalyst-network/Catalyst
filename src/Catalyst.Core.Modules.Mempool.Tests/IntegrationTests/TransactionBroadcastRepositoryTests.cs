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
using SharpRepository.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using Catalyst.Protocol.Wire;
using Xunit;
using Xunit.Abstractions;
using Catalyst.TestUtils.Repository;

namespace Catalyst.Core.Modules.Mempool.Tests.IntegrationTests
{
    public sealed class TransactionBroadcastRepositoryTests : FileSystemBasedTest
    {
        public static IEnumerable<object[]> ModulesList =>
            new List<object[]>
            {
                new object[] {new InMemoryTestModule<TransactionBroadcast, TransactionBroadcastDao>()},
                new object[] {new MongoDbTestModule<TransactionBroadcast, TransactionBroadcastDao>()}
            };

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
                var transactBroadcastRepoRepo = PopulateTransactBroadcastRepo(scope, out var criteriaId, out var contractEntryDaoList, out var publicEntryDaoList);

                transactBroadcastRepoRepo.Get(criteriaId).Id.Should().Be(criteriaId);

                transactBroadcastRepoRepo.Get(criteriaId).ContractEntries.FirstOrDefault().Data
                   .Should().Be(contractEntryDaoList.FirstOrDefault().Data);

                transactBroadcastRepoRepo.Get(criteriaId).PublicEntries.FirstOrDefault().Amount
                   .Should().Be(publicEntryDaoList.FirstOrDefault().Amount);
            }
        }

        private IRepository<TransactionBroadcastDao, string> PopulateTransactBroadcastRepo(ILifetimeScope scope, out string Id, out IList<ContractEntryDao> contractEntryDaoList, out IList<PublicEntryDao> publicEntryDaoList)
        {
            var transactBroadcastRepo = scope.Resolve<IRepository<TransactionBroadcastDao, string>>();

            var transactionBroadcastDao = new TransactionBroadcastDao().ToDao(TransactionHelper.GetPublicTransaction());
            transactionBroadcastDao.Id = Guid.NewGuid().ToString();
            Id = transactionBroadcastDao.Id;

            //Data creation put into a helper function
            var contractList = new List<ContractEntryDao>();
            Enumerable.Range(0, 5).ToList().ForEach(i =>
            {
                contractList.Add(new ContractEntryDao() {Amount = "1585.2" + i, Data = "data gre" + Guid.NewGuid()});
            });
            transactionBroadcastDao.ContractEntries = contractList;
            contractEntryDaoList = contractList;

            var publicList = new List<PublicEntryDao>();
            Enumerable.Range(0, 5).ToList().ForEach(i =>
            {
                publicList.Add(new PublicEntryDao() {Amount = new Random().Next(2597563).ToString()});
            });
            transactionBroadcastDao.PublicEntries = publicList;
            publicEntryDaoList = publicList;

            transactBroadcastRepo.Add(transactionBroadcastDao);

            return transactBroadcastRepo;
        }

        [Theory(Skip = "Setup to run in pipeline only")]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        [MemberData(nameof(ModulesList))]
        public void TransactionBroadcastRepo_All_Dbs_Can_Update_And_Retrieve(Module dbModule)
        {
            RegisterModules(dbModule);
        }

        [Theory(Skip = "Setup to run in pipeline only")]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        [MemberData(nameof(ModulesList))]
        public void TransactionBroadcastRepo_All_Dbs_Can_Save_And_Retrieve(Module dbModule)
        {
            RegisterModules(dbModule);

            TransactionBroadcastRepo_Can_Save_And_Retrieve();
        }

        [Fact(Skip = "Microsoft DBs yet to be completed")]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void TransactionBroadcastRepo_EfCore_Dbs_Update_And_Retrieve()
        {
            var connectionStr = ContainerProvider.ConfigurationRoot
               .GetSection("CatalystNodeConfiguration:PersistenceConfiguration:repositories:efCore:connectionString").Value;

            RegisterModules(new EfCoreDbTestModule<TransactionBroadcast, TransactionBroadcastDao>(connectionStr));

            CheckForDatabaseCreation();
        }

        //[Fact(Skip = "Microsoft DBs yet to be completed")]
        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void TransactionBroadcastRepo_EfCore_Dbs_Can_Save_And_Retrieve()
        {
            var connectionStr = ContainerProvider.ConfigurationRoot
               .GetSection("CatalystNodeConfiguration:PersistenceConfiguration:repositories:efCore:connectionString").Value;

            RegisterModules(new EfCoreDbTestModule<TransactionBroadcast, TransactionBroadcastDao>(connectionStr));

            CheckForDatabaseCreation();

            TransactionBroadcastRepo_Can_Save_And_Retrieve();
        }

        private void CheckForDatabaseCreation()
        {
            try
            {
                using (var scope = ContainerProvider.Container.BeginLifetimeScope(CurrentTestName))
                {
                    var contextDb = scope.Resolve<IDbContext>();

                    ((DbContext) contextDb).Database.EnsureCreated();
                }
            }
            catch (Exception ex)
            {
                ex.ToString();
            }
        }

        private void RegisterModules(Module module)
        {
            ContainerProvider.ConfigureContainerBuilder();

            ContainerProvider.ContainerBuilder.RegisterModule(module);
        }
    }
}
