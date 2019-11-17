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
using System.Linq;
using Autofac;
using Catalyst.Core.Lib.DAO;
using Catalyst.Core.Lib.DAO.Cryptography;
using Catalyst.Core.Lib.DAO.Transaction;
using Catalyst.Core.Lib.Repository;
using Catalyst.Protocol.Cryptography;
using Catalyst.Protocol.Network;
using Catalyst.Protocol.Wire;
using Catalyst.TestUtils;
using Catalyst.TestUtils.ProtocolHelpers;
using Catalyst.TestUtils.Repository;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SharpRepository.Repository;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.Modules.Mempool.Tests.IntegrationTests
{
    public sealed class TransactionBroadcastRepositoryTests : FileSystemBasedTest
    {
        private readonly IMapperProvider _mapperProvider;

        public static IEnumerable<object[]> ModulesList =>
            new List<object[]>
            {
                new object[] {new InMemoryTestModule<TransactionBroadcastDao>()},
                new object[] {new MongoDbTestModule<TransactionBroadcastDao>()}
            };

        public TransactionBroadcastRepositoryTests(ITestOutputHelper output) : base(output)
        {
            _mapperProvider = new TestMapperProvider();
        }

        private void TransactionBroadcastRepo_Can_Save_And_Retrieve()
        {
            using (var scope = ContainerProvider.Container.BeginLifetimeScope(CurrentTestName))
            {
                var transactBroadcastRepo = PopulateTransactBroadcastRepo(scope, out var criteriaId,
                    out var contractEntryDaoList, out var publicEntryDaoList);

                transactBroadcastRepo.Get(criteriaId).Id.Should().Be(criteriaId);

                transactBroadcastRepo.Get(criteriaId).ContractEntries.FirstOrDefault().Data
                   .Should().Be(contractEntryDaoList.FirstOrDefault().Data);

                transactBroadcastRepo.Get(criteriaId).PublicEntries.FirstOrDefault().Amount
                   .Should().Be(publicEntryDaoList.FirstOrDefault().Amount);
            }
        }

        private IRepository<TransactionBroadcastDao, string> PopulateTransactBroadcastRepo(ILifetimeScope scope,
            out string id,
            out IEnumerable<ContractEntryDao> contractEntryDaoList,
            out IEnumerable<PublicEntryDao> publicEntryDaoList)
        {
            var transactBroadcastRepo = scope.Resolve<IRepository<TransactionBroadcastDao, string>>();

            var transactionBroadcastDao = TransactionHelper.GetPublicTransaction().ToDao<TransactionBroadcast, TransactionBroadcastDao>(_mapperProvider);
            id = transactionBroadcastDao.Id;

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

            transactionBroadcastDao.Signature = new SignatureDao
                {RawBytes = "mplwifwfjfw", SigningContext = signingContextDao};

            transactBroadcastRepo.Add(transactionBroadcastDao);

            return transactBroadcastRepo;
        }

        private void TransactionBroadcast_Update_And_Retrieve()
        {
            using (var scope = ContainerProvider.Container.BeginLifetimeScope(CurrentTestName))
            {
                var transactBroadcastRepo = PopulateTransactBroadcastRepo(scope, out var criteriaId,
                    out _, out _);

                var retrievedTransactionDao = transactBroadcastRepo.Get(criteriaId);
                retrievedTransactionDao.TimeStamp = new DateTime(1999, 2, 2);
                transactBroadcastRepo.Update(retrievedTransactionDao);

                var retrievedTransactionDaoModified = transactBroadcastRepo.Get(criteriaId);

                var dateComparer = retrievedTransactionDaoModified.TimeStamp.Date.ToString("MM/dd/yyyy");
                
                // ReSharper disable once SuspiciousTypeConversion.Global
                // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                dateComparer.Should()?.Equals("02/02/1999");
            }
        }

        [Theory(Skip = "Setup to run in pipeline only")]
        [Trait(Traits.TestType, Traits.E2EMongoDb)]
        [MemberData(nameof(ModulesList))]
        public void TransactionBroadcastRepo_All_Dbs_Can_Update_And_Retrieve(Module dbModule)
        {
            RegisterModules(dbModule);

            TransactionBroadcast_Update_And_Retrieve();
        }

        [Theory(Skip = "Setup to run in pipeline only")]
        [Trait(Traits.TestType, Traits.E2EMongoDb)]
        [MemberData(nameof(ModulesList))]
        public void TransactionBroadcastRepo_All_Dbs_Can_Save_And_Retrieve(Module dbModule)
        {
            RegisterModules(dbModule);

            TransactionBroadcastRepo_Can_Save_And_Retrieve();
        }

        [Fact(Skip = "Microsoft DBs yet to be completed")]
        [Trait(Traits.TestType, Traits.E2EMssql)]
        public void TransactionBroadcastRepo_EfCore_Dbs_Update_And_Retrieve()
        {
            var connectionStr = ContainerProvider.ConfigurationRoot
               .GetSection("CatalystNodeConfiguration:PersistenceConfiguration:repositories:efCore:connectionString")
               .Value;

            RegisterModules(new EfCoreDbTestModule(connectionStr));

            CheckForDatabaseCreation();

            TransactionBroadcast_Update_And_Retrieve();
        }

        [Fact(Skip = "Microsoft DBs yet to be completed")]
        [Trait(Traits.TestType, Traits.E2EMssql)]
        public void TransactionBroadcastRepo_EfCore_Dbs_Can_Save_And_Retrieve()
        {
            var connectionStr = ContainerProvider.ConfigurationRoot
               .GetSection("CatalystNodeConfiguration:PersistenceConfiguration:repositories:efCore:connectionString")
               .Value;

            RegisterModules(new EfCoreDbTestModule(connectionStr));

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
