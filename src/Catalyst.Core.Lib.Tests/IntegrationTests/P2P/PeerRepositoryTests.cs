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
using Autofac;
using Catalyst.Abstractions.DAO;
using Catalyst.TestUtils;
using Xunit;
using Xunit.Abstractions;
using Catalyst.Core.Lib.DAO;
using Catalyst.Core.Lib.P2P.Models;
using Catalyst.Core.Lib.Repository;
using Catalyst.Modules.Repository.MongoDb;
using SharpRepository.Repository;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Catalyst.Core.Lib.Tests.IntegrationTests.P2P
{
    public sealed class PeerRepositoryTests : FileSystemBasedTest
    {
        public static IEnumerable<object[]> ModulesList => 
            new List<object[]>
            {
                new object[] {new InMemoryTestModule<Peer, PeerDao>()},
                new object[] {new MongoDbModule<Peer, PeerDao>()}
            };

        private sealed class ModuleAzureSqlTypes : Module
        {
            private readonly string _connectionString;
            public ModuleAzureSqlTypes(string connectionString) { _connectionString = connectionString; }

            protected override void Load(ContainerBuilder builder)
            {
                builder.Register(c => new EfCoreContext(_connectionString)).AsImplementedInterfaces().AsSelf()
                   .InstancePerLifetimeScope();

                builder.RegisterType<PeerEfCoreRepository>().As<IRepository<PeerDao, string>>().SingleInstance();
            }
        }

        public PeerRepositoryTests(ITestOutputHelper output) : base(output)
        {
            var mappers = new IMapperInitializer[]
            {
                new PeerIdDao(),
                new PeerDao()
            };

            var map = new MapperProvider(mappers);
            map.Start();
        }

        private void PeerRepo_Can_Save_And_Retrieve()
        {
            using (var scope = ContainerProvider.Container.BeginLifetimeScope(CurrentTestName))
            {
                var criteriaId = string.Empty;
                var peerRepo = PopulatePeerRepo(scope, out criteriaId);

                peerRepo.Get(criteriaId).Id.Should().Be(criteriaId);
                peerRepo.Get(criteriaId).PeerIdentifier.PublicKey.Should().Be(peerRepo.Get(criteriaId).PeerIdentifier.PublicKey);
                peerRepo.Get(criteriaId).PeerIdentifier.Ip.Should().Be(peerRepo.Get(criteriaId).PeerIdentifier.Ip);
            }
        }

        private void PeerRepo_Can_Update_And_Retrieve()
        {
            using (var scope = ContainerProvider.Container.BeginLifetimeScope(CurrentTestName))
            {
                var criteriaId = string.Empty;

                var peerRepo = PopulatePeerRepo(scope, out criteriaId);

                var retrievedPeer = peerRepo.Get(criteriaId);
                retrievedPeer.Touch();
                peerRepo.Update(retrievedPeer);

                var retrievedPeerModified = peerRepo.Get(criteriaId);
                var now = DateTime.UtcNow.Date;

                if (retrievedPeerModified.Modified == null)
                {
                    return;
                }

                var dateComparer = retrievedPeerModified.Modified.Value.Date.ToString("MM/dd/yyyy");
                dateComparer.Should().Equals(now.ToString("MM/dd/yyyy"));
            }
        }

        private IRepository<PeerDao, string> PopulatePeerRepo(ILifetimeScope scope, out string Id)
        {
            var peerRepo = scope.Resolve<IRepository<PeerDao, string>>();

            var peerDao = new PeerDao().ToDao(new Peer {PeerId = PeerIdHelper.GetPeerId(new Random().Next().ToString()) });
            peerDao.Id = Guid.NewGuid().ToString();
            Id = peerDao.Id;

            peerDao.PeerIdentifier = new PeerIdDao().ToDao(PeerIdHelper.GetPeerId(new Random().Next().ToString()));
            peerDao.PeerIdentifier.Id = Guid.NewGuid().ToString();

            peerRepo.Add(peerDao);

            return peerRepo;
        }
        
        [Theory(Skip = "Setup to run in pipeline only")]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        [MemberData(nameof(ModulesList))]
        public void PeerRepo_All_Dbs_Can_Update_And_Retrieve(Module dbModule)
        {
            RegisterModules(dbModule);

            PeerRepo_Can_Update_And_Retrieve();
        }

        [Theory(Skip = "Setup to run in pipeline only")]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        [MemberData(nameof(ModulesList))]
        public void PeerRepo_All_Dbs_Can_Save_And_Retrieve(Module dbModule)
        {
            RegisterModules(dbModule);

            PeerRepo_Can_Save_And_Retrieve();
        }

        [Fact(Skip = "Microsoft DBs yet to be completed")]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void PeerRepo_Microsoft_SQLTypes_Dbs_Update_And_Retrieve()
        {
            var connectionStr = ContainerProvider.ConfigurationRoot
               .GetSection("CatalystNodeConfiguration:PersistenceConfiguration:repositories:efCore:connectionString").Value;

            RegisterModules(new ModuleAzureSqlTypes(connectionStr));

            CheckForDatabaseCreation();

            PeerRepo_Can_Update_And_Retrieve();
        }

        [Fact(Skip = "Microsoft DBs yet to be completed")]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void PeerRepo_Microsoft_SQLTypes_Dbs_Can_Save_And_Retrieve()
        {
            var connectionStr = ContainerProvider.ConfigurationRoot
               .GetSection("CatalystNodeConfiguration:PersistenceConfiguration:repositories:efCore:connectionString").Value;

            RegisterModules(new ModuleAzureSqlTypes(connectionStr));

            CheckForDatabaseCreation();

            PeerRepo_Can_Save_And_Retrieve();
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

