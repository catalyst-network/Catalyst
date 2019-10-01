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
using SharpRepository.Repository;
using FluentAssertions;
using Catalyst.TestUtils.Repository;

namespace Catalyst.Core.Lib.Tests.IntegrationTests.P2P
{
    public sealed class PeerRepositoryTests : FileSystemBasedTest
    {
        public static IEnumerable<object[]> ModulesList => 
            new List<object[]>
            {
                new object[] {new InMemoryTestModule<Peer, PeerDao>()},
                new object[] {new MongoDbTestModule<Peer, PeerDao>()}
            };

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
                var peerRepo = PopulatePeerRepo(scope, out var peerDao);

                peerRepo.Get(peerDao.DocumentId).DocumentId.Should().Be(peerDao.DocumentId);
                peerRepo.Get(peerDao.DocumentId).PeerIdentifier.PublicKey.Should().Be(peerDao.PeerIdentifier.PublicKey);
                peerRepo.Get(peerDao.DocumentId).PeerIdentifier.Ip.Should().Be(peerDao.PeerIdentifier.Ip);
            }
        }

        private void PeerRepo_Can_Update_And_Retrieve()
        {
            using (var scope = ContainerProvider.Container.BeginLifetimeScope(CurrentTestName))
            {
                var peerRepo = PopulatePeerRepo(scope, out var peerDao);

                var retrievedPeer = peerRepo.Get(peerDao.DocumentId);
                retrievedPeer.Touch();
                peerRepo.Update(retrievedPeer);

                var retrievedPeerModified = peerRepo.Get(peerDao.DocumentId);
                var now = DateTime.UtcNow.Date;

                if (retrievedPeerModified.Modified == null)
                {
                    return;
                }

                var dateComparer = retrievedPeerModified.Modified.Value.Date.ToString("MM/dd/yyyy");
                dateComparer.Should().Equals(now.ToString("MM/dd/yyyy"));
            }
        }

        private IRepository<PeerDao, string> PopulatePeerRepo(ILifetimeScope scope, out PeerDao peerDaoOutput)
        {
            var peerRepo = scope.Resolve<IRepository<PeerDao, string>>();

            var peerDao = new PeerDao().ToDao(new Peer {PeerId = PeerIdHelper.GetPeerId(new Random().Next().ToString())});
            peerDao.DocumentId = Guid.NewGuid().ToString();

            peerDao.PeerIdentifier = new PeerIdDao().ToDao(PeerIdHelper.GetPeerId(new Random().Next().ToString()));
            peerDao.PeerIdentifier.DocumentId = Guid.NewGuid().ToString();

            peerRepo.Add(peerDao);
            peerDaoOutput = peerDao;

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

        private void RegisterModules(Module module)
        {
            ContainerProvider.ConfigureContainerBuilder();

            ContainerProvider.ContainerBuilder.RegisterModule(module);
        }
    }
}

