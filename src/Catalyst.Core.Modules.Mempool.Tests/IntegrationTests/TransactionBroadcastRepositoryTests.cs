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
using Catalyst.Protocol.Cryptography;
using Catalyst.Protocol.Network;
using Catalyst.Protocol.Wire;
using Catalyst.TestUtils.ProtocolHelpers;
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
            TestMappers.Start();
        }

        private void TransactionBroadcastRepo_Can_Save_And_Retrieve()
        {
            using (var scope = ContainerProvider.Container.BeginLifetimeScope(CurrentTestName))
            {
                var transactBroadcastRepo = PopulateTransactBroadcastRepo(scope, out var criteriaId, out var contractEntryDaoList, out var publicEntryDaoList);
                transactBroadcastRepo.Get(criteriaId).Id.Should().Be(criteriaId);

                transactBroadcastRepo.Get(criteriaId).ContractEntries.FirstOrDefault().Data
                   .Should().Be(contractEntryDaoList.FirstOrDefault().Data);

                transactBroadcastRepo.Get(criteriaId).PublicEntries.FirstOrDefault().Amount
                   .Should().Be(publicEntryDaoList.FirstOrDefault().Amount);
            }
        }

        private IRepository<TransactionBroadcastDao, string> PopulateTransactBroadcastRepo(ILifetimeScope scope, 
            out string Id, 
            out IEnumerable<ContractEntryDao> contractEntryDaoList, 
            out IEnumerable<PublicEntryDao> publicEntryDaoList)
        {
            var transactBroadcastRepo = scope.Resolve<IRepository<TransactionBroadcastDao, string>>();

            var transactionBroadcastDao = new TransactionBroadcastDao().ToDao(TransactionHelper.GetPublicTransaction());
            transactionBroadcastDao.Id = Guid.NewGuid().ToString();
            Id = transactionBroadcastDao.Id;

            transactionBroadcastDao.ContractEntries = ContractEntryHelper.GetContractEntriesDao(10);
            contractEntryDaoList = transactionBroadcastDao.ContractEntries;

            transactionBroadcastDao.PublicEntries = PublicEntryHelper.GetPublicEntriesDao(10);
            publicEntryDaoList = transactionBroadcastDao.PublicEntries;

            var signingContextDao = new SigningContextDao
            {
                NetworkType = NetworkType.Devnet,
                SignatureType = SignatureType.TransactionPublic
            };

            transactionBroadcastDao.ConfidentialEntries = ConfidentialEntryHelper.GetConfidentialEntriesDao(10);

            transactionBroadcastDao.Signature = new SignatureDao {RawBytes = "mplwifwfjfw", SigningContext = signingContextDao};

            transactBroadcastRepo.Add(transactionBroadcastDao);

            return transactBroadcastRepo;
        }

        private void TransactionBroadcast_Update_And_Retrieve()
        {
            using (var scope = ContainerProvider.Container.BeginLifetimeScope(CurrentTestName))
            {
                var transactBroadcastRepo = PopulateTransactBroadcastRepo(scope, out var criteriaId, out var contractEntryDaoList, out var publicEntryDaoList);

                var retievedTransactionDao = transactBroadcastRepo.Get(criteriaId);
                retievedTransactionDao.TimeStamp = new DateTime(1999, 2, 2);
                transactBroadcastRepo.Update(retievedTransactionDao);

                var retievedTranscantionDaoModified = transactBroadcastRepo.Get(criteriaId);

                var dateComparer = retievedTranscantionDaoModified.TimeStamp.Date.ToString("MM/dd/yyyy");
                dateComparer.Should().Equals("02/02/1999");
            }
        }

        [Theory(Skip = "Setup to run in pipeline only")]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        [MemberData(nameof(ModulesList))]
        public void TransactionBroadcastRepo_All_Dbs_Can_Update_And_Retrieve(Module dbModule)
        {
            RegisterModules(dbModule);

            TransactionBroadcast_Update_And_Retrieve();
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

            TransactionBroadcast_Update_And_Retrieve();
        }

        [Fact(Skip = "Microsoft DBs yet to be completed")]
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
