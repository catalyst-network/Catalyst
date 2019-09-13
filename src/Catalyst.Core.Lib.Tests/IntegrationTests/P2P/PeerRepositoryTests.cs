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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Autofac;
using Catalyst.Abstractions.DAO;
using Catalyst.TestUtils;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;
using SharpRepository.EfCoreRepository;
using Catalyst.Core.Lib.DAO;
using Catalyst.Core.Lib.DAO.Deltas;
using Catalyst.Core.Lib.Repository;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Catalyst.Core.Lib.Tests.IntegrationTests.P2P
{
    public sealed class PeerRepositoryIntegrationTests : FileSystemBasedTest
    {
        private ContainerProvider _containerProvider;

        private Microsoft.EntityFrameworkCore.DbContext context;

        private readonly IMapperInitializer[] _mappers;
        
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
            Setup();

            _mappers = new IMapperInitializer[]
            {
                new ProtocolMessageDao(),
                new CfTransactionEntryDao(),
                new CandidateDeltaBroadcastDao(),
                new ProtocolErrorMessageSignedDao(),
                new PeerIdDao(),
                new SigningContextDao(),
                new DeltaDao(),
                new CandidateDeltaBroadcastDao(),
                new DeltaDfsHashBroadcastDao(),
                new FavouriteDeltaBroadcastDao(),
                new CoinbaseEntryDao(),
                new StTransactionEntryDao(),
                new CfTransactionEntryDao(),
                new TransactionBroadcastDao(),
                new EntryRangeProofDao(),
            };

            var map = new MapperProvider(_mappers);
            map.Start();
        }

        private async Task<string> CreateAlteredConfigForMempool(FileInfo mempoolConfigFile)
        {
            var originalContent = await File.ReadAllTextAsync(mempoolConfigFile.FullName);
            var newContent =
                originalContent.Replace("\"Config/Modules/mempool.json\"",
                    JsonConvert.ToString(mempoolConfigFile.FullName));
            var newJsonPath = Path.Combine(FileSystem.GetCatalystDataDir().FullName,
                $"components.{mempoolConfigFile.Name}");
            File.WriteAllText(newJsonPath, newContent);
            return newJsonPath;
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

                var beforeAdd = repositoryEf.GetAll();

                using (var trans = new TransactionScope())
                {
                    repositoryEf.Add(peer);
                    trans.Complete();
                }

                var dataCollection = repositoryEf.GetAll();
                var temp = dataCollection.FirstOrDefault();
            }
            catch (Exception e)
            {
                //Console.WriteLine(e);
            }

            //var reconverted = transactionEntryDao.ToProtoBuff();
            //reconverted.Should().Be(original);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
            {
                return;
            }

            _containerProvider?.Dispose();
        }
    }
}

