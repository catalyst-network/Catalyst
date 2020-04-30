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
using Catalyst.TestUtils;
using Catalyst.Core.Lib.DAO;
using Catalyst.Core.Lib.DAO.Peer;
using Catalyst.Core.Lib.Service;
using Catalyst.Protocol.Peer;
using SharpRepository.Repository;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Catalyst.TestUtils.Repository;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NUnit.Framework;

namespace Catalyst.Core.Lib.Tests.IntegrationTests.P2P
{
    [TestFixture]
    [Category(Traits.IntegrationTest)] 
    public sealed class PeerRepositoryTests : FileSystemBasedTest
    {
        private TestMapperProvider _mapperProvider;

        public static IEnumerable<object[]> ModulesList => 
            new List<object[]>
            {
                new object[] {new InMemoryTestModule<PeerDao>()},
                new object[] {new MongoDbTestModule<PeerDao>()}
            };

        [SetUp]
        public void Init()
        {
            _mapperProvider = new TestMapperProvider();
        }

        private void PeerRepo_Can_Save_And_Retrieve()
        {
            using (var scope = ContainerProvider.Container.BeginLifetimeScope(CurrentTestName))
            {
                var peerRepo = PopulatePeerRepo(scope, out var peerDao);

                peerRepo.Get(peerDao.Id).Id.Should().Be(peerDao.Id);
                peerRepo.Get(peerDao.Id).PeerIdentifier.PublicKey.Should().Be(peerDao.PeerIdentifier.PublicKey);
                peerRepo.Get(peerDao.Id).PeerIdentifier.Ip.Should().Be(peerDao.PeerIdentifier.Ip);
            }
        }

        private void PeerRepo_Can_Update_And_Retrieve()
        {
            using (var scope = ContainerProvider.Container.BeginLifetimeScope(CurrentTestName))
            {
                var peerRepo = PopulatePeerRepo(scope, out var peerDao);

                var retrievedPeer = peerRepo.Get(peerDao.Id);
                retrievedPeer.Touch();
                peerRepo.Update(retrievedPeer);

                var retrievedPeerModified = peerRepo.Get(peerDao.Id);
                var now = DateTime.UtcNow.Date;

                if (retrievedPeerModified.Modified == null)
                {
                    return;
                }

                var dateComparer = retrievedPeerModified.Modified.Value.Date.ToString("MM/dd/yyyy");
                
                // ReSharper disable once SuspiciousTypeConversion.Global
                // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                dateComparer.Should()?.Equals(now.ToString("MM/dd/yyyy"));
            }
        }

        private IRepository<PeerDao, string> PopulatePeerRepo(ILifetimeScope scope, out PeerDao peerDaoOutput)
        {
            var peerRepo = scope.Resolve<IRepository<PeerDao, string>>();

            var peerDao = new PeerDao
            {
                Id = Guid.NewGuid().ToString()
            };
            
            var peerId = PeerIdHelper.GetPeerId(new Random().Next().ToString());
            peerDao.PeerIdentifier = peerId.ToDao<PeerId, PeerIdDao>(_mapperProvider);
            peerDao.PeerIdentifier.Id = Guid.NewGuid().ToString();

            peerRepo.Add(peerDao);
            peerDaoOutput = peerDao;

            return peerRepo;
        }
        
        [Ignore("Setup to run in pipeline only")]
        [Property(Traits.TestType, Traits.E2EMongoDb)]
        [TestCase(nameof(ModulesList))]
        public void PeerRepo_All_Dbs_Can_Update_And_Retrieve(Module dbModule)
        {
            RegisterModules(dbModule);

            PeerRepo_Can_Update_And_Retrieve();
        }

        [Ignore("Setup to run in pipeline only")]
        [Property(Traits.TestType, Traits.E2EMongoDb)]
        [TestCase(nameof(ModulesList))]
        public void PeerRepo_All_Dbs_Can_Save_And_Retrieve(Module dbModule)
        {
            RegisterModules(dbModule);

            PeerRepo_Can_Save_And_Retrieve();
        }

        [Ignore("Microsoft DBs yet to be completed")]
        [Property(Traits.TestType, Traits.E2EMssql)]
        public void PeerRepo_EfCore_Dbs_Update_And_Retrieve()
        {
            var connectionStr = ContainerProvider.ConfigurationRoot
               .GetSection("CatalystNodeConfiguration:PersistenceConfiguration:repositories:efCore:connectionString").Value;

            RegisterModules(new EfCoreDbTestModule(connectionStr));

            CheckForDatabaseCreation();

            PeerRepo_Can_Update_And_Retrieve();
        }

        [Ignore("Microsoft DBs yet to be completed")]
        [Property(Traits.TestType, Traits.E2EMssql)]
        public void PeerRepo_EfCore_Dbs_Can_Save_And_Retrieve()
        {
            var connectionStr = ContainerProvider.ConfigurationRoot
               .GetSection("CatalystNodeConfiguration:PersistenceConfiguration:repositories:efCore:connectionString").Value;

            RegisterModules(new EfCoreDbTestModule(connectionStr));

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

