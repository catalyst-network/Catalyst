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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Autofac;
using Catalyst.Abstractions.DAO;
using Catalyst.Abstractions.Mempool;
using Catalyst.Abstractions.Mempool.Repositories;
using Catalyst.Abstractions.P2P.Discovery;
using Catalyst.Abstractions.Repository;
using Catalyst.Abstractions.Types;
using Catalyst.TestUtils;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;
using SharpRepository.EfCoreRepository;
using Catalyst.Core.Lib.DAO;
using Catalyst.Core.Lib.DAO.Deltas;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Messaging.Correlation;
using Catalyst.Core.Lib.Mempool.Documents;
using Catalyst.Core.Lib.P2P.Models;
using Catalyst.Core.Lib.P2P.Repository;
using Catalyst.Core.Lib.Repository;
using Catalyst.Core.Modules.Mempool.Repositories;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Infrastructure;
using SharpRepository.InMemoryRepository;
using SharpRepository.Repository;
using SharpRepository.Repository.Caching;
using Catalyst.Modules.Repository.CosmosDb;
//using Catalyst.Modules.Repository.MongoDb;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;


namespace Catalyst.Core.Lib.Tests.IntegrationTests.P2P
{
    public sealed class PeerRepositoryIntegrationTests : FileSystemBasedTest
    {
        public static string _connectionString;

        private Microsoft.EntityFrameworkCore.DbContext context;

        private readonly IMapperInitializer[] _mappers;

        public static IEnumerable<object[]> ModulesList => 
            new List<object[]>
            {
                //new object[] {new ModuleAzureSqlTypes()},
                new object[] {new MempoolModuleCosmosDb()},
                new object[] {new MempoolModuleMongoDb()}
            };

        private class PeerRepositoryModuleInMemory : Module
        {
            protected override void Load(ContainerBuilder builder)
            {
                builder.Register(c => new InMemoryRepository<MempoolDocument, string>())
                   .As<IRepository<MempoolDocument, string>>()
                   .SingleInstance();
                builder.RegisterType<MempoolDocumentRepository>().As<IMempoolRepository<MempoolDocument>>().SingleInstance();
                builder.RegisterType<Modules.Mempool.Mempool>().As<IMempool<MempoolDocument>>().SingleInstance();
            }
        }

        private sealed class MempoolModuleMongoDb : Module
        {
            protected override void Load(ContainerBuilder builder)
            {
                //builder.Register(c => new MongoDbRepository<PeerIdDao>(
                //    c.ResolveOptional<ICachingStrategy<PeerIdDao, string>>()
                //)).As<IRepository<MempoolDocument, string>>().SingleInstance();
            }
        }

        private sealed class MempoolModuleCosmosDb : Module
        {
            protected override void Load(ContainerBuilder builder)
            {
                builder.Register(c =>
                        new CosmosDbRepository<PeerIdDao>("", "", "", true))
                   .As<IRepository<MempoolDocument, string>>()
                   .SingleInstance();
                builder.RegisterType<MempoolDocumentRepository>().As<IMempoolRepository<MempoolDocument>>().SingleInstance();
                builder.RegisterType<Modules.Mempool.Mempool>().As<IMempool<MempoolDocument>>().SingleInstance();
            }
        }

        private sealed class ModuleAzureSqlTypes : Module
        {
            private readonly string _connectionString;
            public ModuleAzureSqlTypes(string connectionString) { _connectionString = connectionString; }

            protected override void Load(ContainerBuilder builder)
            {
                builder.Register(c => new EfCoreContext(_connectionString)).As<IDbContext>();

                builder.RegisterType<EnhancedEfCoreRepository>().As<IRepository<Peer, string>>().SingleInstance();

                builder.RegisterType<PeerRepository>().As<IPeerRepository>().SingleInstance();
            }
        }

        public void Setup()
        {
            var connectionStr =
                "Server = databasemachine.traderiser.com\\SQL2012, 49175; Database = AtlasCity; User Id = developer; Password = d3v3lop3rhous3;";

            // Run the test against one instance of the context
            context = new EfCoreContext(connectionStr);

            if (!context.GetService<IRelationalDatabaseCreator>().Exists())
            {
                var databaseCreator = context.GetService<IRelationalDatabaseCreator>();
                databaseCreator.CreateTables();
            }
        }

        public PeerRepositoryIntegrationTests(ITestOutputHelper output) : base(output)
        {
            //_connectionString = ContainerProvider.ConfigurationRoot
            //   .GetSection("CatalystNodeConfiguration:PersistenceConfiguration:repositories:efCore:connectionString").Value;

            //Setup();

            _mappers = new IMapperInitializer[]
            {
                new ProtocolMessageDao(),
                //new CfTransactionEntryDao(),
                new CandidateDeltaBroadcastDao(),
                new ProtocolErrorMessageSignedDao(),
                new PeerIdDao(),
                new SigningContextDao(),
                new DeltaDao(),
                new CandidateDeltaBroadcastDao(),
                new DeltaDfsHashBroadcastDao(),
                new FavouriteDeltaBroadcastDao(),
                new CoinbaseEntryDao(),
                //new StTransactionEntryDao(),
                //new CfTransactionEntryDao(),
                new TransactionBroadcastDao(),
                //new EntryRangeProofDao(),
            };

            var map = new MapperProvider(_mappers);
            map.Start();
        }

        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void Save_And_Retrieve_Peer_From_Repository()
        {
            try
            {
                var peerIdDao = new PeerIdDao();
                var original = PeerIdentifierHelper.GetPeerIdentifier("MyPeerId_Testing").PeerId;

                var peer = peerIdDao.ToDao(original);

                var repositoryEf = new EfCoreRepository<PeerIdDao, string>(context);
                //var repositoryEf = new EnhancedEfCoreRepository((EfCoreContext) context);

                var beforeAdd = repositoryEf.GetAll();

                using (var trans = new TransactionScope())
                {
                    repositoryEf.Add(peer);
                    trans.Complete();
                }
            }
            catch (Exception e)
            {
                //Console.WriteLine(e);
            }

            //var reconverted = transactionEntryDao.ToProtoBuff();
            //reconverted.Should().Be(original);
        }

        private void PeerRepo_Can_Save_And_Retrieve_M(Module mempoolModule)
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
        
        private void PeerRepo_Can_Save_And_Retrieve(Module mempoolModule)
        {
            ContainerProvider.ConfigureContainerBuilder();
            ContainerProvider.ContainerBuilder.RegisterModule(mempoolModule);

            try
            {
                using (var scope = ContainerProvider.Container.BeginLifetimeScope(CurrentTestName))
                {
                    //var res = scope.Resolve<IRepository<PeerIdDao, string>>();

                    //var res = new EnhancedEfCoreRepository(scope.Resolve<IDbContext>());

                    var peerRepo = scope.Resolve<IPeerRepository>();

                    var contextDb = scope.Resolve<IDbContext>();

                    if (!((DbContext) contextDb).GetService<IRelationalDatabaseCreator>().Exists())
                    {
                        var databaseCreator = context.GetService<IRelationalDatabaseCreator>();
                        databaseCreator.CreateTables();
                    }

                    var peer = new Peer {PeerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("Test")};

                    peerRepo.Add(peer);

                    //var peerIdDao = new PeerIdDao();
                    //var original = PeerIdentifierHelper.GetPeerIdentifier("MyPeerId_Testing");
                    //var peer = peerIdDao.ToDao(original);

                    //using (var trans = new TransactionScope())
                    //{
                    //    peerRepo.Add(peer);
                    //    trans.Complete();
                    //}

                    //var temp = scope.Resolve<IPeerRepository>();
                    //temp.Add();

                    //var lop = new PeerRepository(new EfCoreRepository<PeerIdDao, string>());

                    //var mempool = scope.Resolve<IMempool<MempoolDocument>>();

                    //var guid = CorrelationId.GenerateCorrelationId().ToString();

                    //mempool.Repository.CreateItem(TransactionHelper.GetTransaction(signature: guid));

                    //var retrievedTransaction = mempool.Repository.ReadItem(TransactionHelper.GetTransaction(signature: guid).Signature);

                    //retrievedTransaction.Transaction.Should().Be(TransactionHelper.GetTransaction(signature: guid));
                    //retrievedTransaction.Transaction.Signature.SequenceEqual(guid.ToUtf8ByteString()).Should().BeTrue();

                    //var mempool = scope.Resolve<IMempool<MempoolDocument>>();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        //[Theory(Skip = "To be run in the pipeline only")]
        [Theory]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        [MemberData(nameof(ModulesList))]
        public void PeerRepo_ExternalDbs_Can_Save_And_Retrieve(Module dbModule)
        {
            PeerRepo_Can_Save_And_Retrieve(dbModule);
        }

        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void PeerRepo_AzureSQLTypes_Dbs_Can_Save_And_Retrieve()
        {
            var connectionStr = ContainerProvider.ConfigurationRoot
               .GetSection("CatalystNodeConfiguration:PersistenceConfiguration:repositories:efCore:connectionString").Value;

            PeerRepo_Can_Save_And_Retrieve(new ModuleAzureSqlTypes(connectionStr));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
            {
                return;
            }

            //_containerProvider?.Dispose();
        }
    }
}

