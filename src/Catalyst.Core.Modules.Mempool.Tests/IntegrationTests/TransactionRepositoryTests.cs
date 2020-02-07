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
using Catalyst.Core.Lib.Service;
using Catalyst.Protocol.Cryptography;
using Catalyst.Protocol.Network;
using Catalyst.Protocol.Transaction;
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
    public sealed class TransactionRepositoryTests : FileSystemBasedTest
    {
        private readonly IMapperProvider _mapperProvider;

        public static IEnumerable<object[]> ModulesList =>
            new List<object[]>
            {
                new object[] {new InMemoryTestModule<PublicEntryDao>()},
                new object[] {new MongoDbTestModule<PublicEntryDao>()}
            };

        public TransactionRepositoryTests(ITestOutputHelper output) : base(output)
        {
            _mapperProvider = new TestMapperProvider();
        }

        private void TransactionRepository_Can_Save_And_Retrieve()
        {
            using (var scope = ContainerProvider.Container.BeginLifetimeScope(CurrentTestName))
            {
                var transactBroadcastRepo = PopulateTransactionRepository(scope, out var criteriaId, out var publicEntryDaoList);

                transactBroadcastRepo.Get(criteriaId).Id.Should().Be(criteriaId);

                transactBroadcastRepo.Get(criteriaId).Amount.Should().Be(publicEntryDaoList.FirstOrDefault().Amount);
            }
        }

        private IRepository<PublicEntryDao, string> PopulateTransactionRepository(ILifetimeScope scope,
            out string id,
            out IEnumerable<PublicEntryDao> publicEntryDaoList)
        {
            var transactionRepository = scope.Resolve<IRepository<PublicEntryDao, string>>();

            var transaction = TransactionHelper.GetPublicTransaction().PublicEntry.ToDao<PublicEntry, PublicEntryDao>(_mapperProvider);
            id = transaction.Id;

            publicEntryDaoList = PublicEntryHelper.GetPublicEntriesDao(10);

            transaction = publicEntryDaoList.First();

            var signingContextDao = new SigningContextDao
            {
                NetworkType = NetworkType.Devnet,
                SignatureType = SignatureType.TransactionPublic
            };

            transaction.Signature = new SignatureDao
            {
                RawBytes = "mplwifwfjfw", SigningContext = signingContextDao
            };

            transactionRepository.Add(transaction);

            return transactionRepository;
        }

        private void Transaction_Update_And_Retrieve()
        {
            using (var scope = ContainerProvider.Container.BeginLifetimeScope(CurrentTestName))
            {
                var transactionRepository = PopulateTransactionRepository(scope, out var criteriaId, out _);

                var retrievedTransactionDao = transactionRepository.Get(criteriaId);
                retrievedTransactionDao.TimeStamp = new DateTime(1999, 2, 2);
                transactionRepository.Update(retrievedTransactionDao);

                var retrievedTransactionDaoModified = transactionRepository.Get(criteriaId);

                var dateComparer = retrievedTransactionDaoModified.TimeStamp.Date.ToString("MM/dd/yyyy");

                // ReSharper disable once SuspiciousTypeConversion.Global
                // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                dateComparer.Should()?.Equals("02/02/1999");
            }
        }

        [Theory(Skip = "Setup to run in pipeline only")]
        [Trait(Traits.TestType, Traits.E2EMongoDb)]
        [MemberData(nameof(ModulesList))]
        public void TransactionRepository_All_Dbs_Can_Update_And_Retrieve(Module dbModule)
        {
            RegisterModules(dbModule);

            Transaction_Update_And_Retrieve();
        }

        [Theory(Skip = "Setup to run in pipeline only")]
        [Trait(Traits.TestType, Traits.E2EMongoDb)]
        [MemberData(nameof(ModulesList))]
        public void TransactionBroadcastRepository_All_Dbs_Can_Save_And_Retrieve(Module dbModule)
        {
            RegisterModules(dbModule);

            TransactionRepository_Can_Save_And_Retrieve();
        }

        [Fact(Skip = "Microsoft DBs yet to be completed")]
        [Trait(Traits.TestType, Traits.E2EMssql)]
        public void TransactionRepository_EfCore_Dbs_Update_And_Retrieve()
        {
            var connectionStr = ContainerProvider.ConfigurationRoot
               .GetSection("CatalystNodeConfiguration:PersistenceConfiguration:repositories:efCore:connectionString")
               .Value;

            RegisterModules(new EfCoreDbTestModule(connectionStr));

            CheckForDatabaseCreation();

            Transaction_Update_And_Retrieve();
        }

        [Fact(Skip = "Microsoft DBs yet to be completed")]
        [Trait(Traits.TestType, Traits.E2EMssql)]
        public void TransactionBroadcastRepository_EfCore_Dbs_Can_Save_And_Retrieve()
        {
            var connectionStr = ContainerProvider.ConfigurationRoot
               .GetSection("CatalystNodeConfiguration:PersistenceConfiguration:repositories:efCore:connectionString")
               .Value;

            RegisterModules(new EfCoreDbTestModule(connectionStr));

            CheckForDatabaseCreation();

            TransactionRepository_Can_Save_And_Retrieve();
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
