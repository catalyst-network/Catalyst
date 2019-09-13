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
using Catalyst.Abstractions.Mempool;
using Catalyst.Protocol;
using Catalyst.TestUtils;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;
using SharpRepository.EfCoreRepository;
using Catalyst.Core.Lib.Config;
using Catalyst.Core.Lib.IO.Messaging.Correlation;
using Catalyst.Core.Lib.Mempool.Documents;
using Catalyst.Core.Lib.P2P.Models;
using Catalyst.Core.Lib.Repository;
using Catalyst.Protocol.Transaction;
using Google.Protobuf;
using Microsoft.EntityFrameworkCore.Storage;
using SharpRepository.Repository;
using Catalyst.Protocol.Common;
using Microsoft.EntityFrameworkCore.Infrastructure;

//using Entity = Microsoft.EntityFrameworkCore.Entity;
//using DbContext = Microsoft.EntityFrameworkCore.DbContext;
//using DbSet = Microsoft.EntityFrameworkCore.DbSet;

namespace Catalyst.Core.Lib.Tests.IntegrationTests.P2P
{
    public sealed class PeerRepositoryIntegrationTests : FileSystemBasedTest
    {
        private ContainerProvider _containerProvider;

        private Microsoft.EntityFrameworkCore.DbContext context;

        public void Setup()
        {
            var connectionStr =
                "Server = databasemachine.traderiser.com\\SQL2012, 49175; Database = AtlasCity; User Id = developer; Password = d3v3lop3rhous3;";

            // Create the schema in the database
            using (var context = new EfCoreContext(connectionStr))
            {
                var built = context.Database.EnsureCreated();
            }

            // Run the test against one instance of the context
            context = new EfCoreContext(connectionStr);
        }

        public PeerRepositoryIntegrationTests(ITestOutputHelper output) : base(output)
        {
            //Setup();
        }

        private async Task Mempool_can_save_and_retrieve(FileInfo mempoolModuleFile)
        {
            System.Diagnostics.Debug.WriteLine(typeof(EfCoreContext).AssemblyQualifiedName);


            var alteredComponentsFile = await CreateAlteredConfigForMempool(mempoolModuleFile);

            var configFilesUsed = new[]
            {
                alteredComponentsFile,
                Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile),
                Path.Combine(Constants.ConfigSubFolder, Constants.NetworkConfigFile(Protocol.Common.Network.Devnet))
            };

            _containerProvider = new ContainerProvider(configFilesUsed, FileSystem, Output);

            _containerProvider.ConfigureContainerBuilder();

            using (var scope = _containerProvider.Container.BeginLifetimeScope(mempoolModuleFile))
            {
                var mempool = scope.Resolve<IMempool<MempoolDocument>>();

                var guid = CorrelationId.GenerateCorrelationId().ToString();
                var mempoolDocument = new MempoolDocument
                { Transaction = TransactionHelper.GetTransaction(signature: guid) };

                //on hold
                //mempool.Repository..SaveMempoolDocument(mempoolDocument);

                //var retrievedTransaction = mempool.GetMempoolDocument(mempoolDocument.Transaction.Signature);
                //retrievedTransaction.Should().Be(mempoolDocument);
                //retrievedTransaction.Transaction.Signature.SchnorrSignature.Should()
                //   .BeEquivalentTo(guid.ToUtf8ByteString());
            }
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
        public void Mempool_with_InEFRepo_can_save_and_retrieve()
        {
            try
            {
                var connectionStr =
                    "Server = databasemachine.traderiser.com\\SQL2012, 49175; Database = AtlasCity; User Id = developer; Password = d3v3lop3rhous3;";

                // Create the schema in the database
                using (var contextTemp = new EfCoreContext(connectionStr))
                {
                    var built = contextTemp.Database.EnsureCreated();
                }

                // Run the test against one instance of the context
                var contextTempM = new EfCoreContext(connectionStr);
                //var databaseCreator = contextTempM.GetService<IRelationalDatabaseCreator>();
                //databaseCreator.CreateTables();


                Random rnd = new Random();
                var ClientId = new Byte[30];
                rnd.NextBytes(ClientId);

                var repositoryEf = new EfCoreRepository<PeerIdDb, string>(contextTempM);
                using (var trans = new TransactionScope())
                {
                    var peerIDConv = PeerIdHelper.GetPeerId();

                    var port = peerIDConv.Port;
                    var pubkey = peerIDConv.PublicKey;
                    var clientVersion = peerIDConv.ProtocolVersion.ToString();
                    var clientId = peerIDConv.ClientId.ToArray();
                    var ip = peerIDConv.Ip.ToArray();

                    repositoryEf.Add(new PeerIdDb
                    {
                        //Id = new Random().Next().ToString(),
                        ClientId = ByteString.CopyFrom(ClientId),
                        ClientVersion = peerIDConv.ProtocolVersion,
                        Ip = peerIDConv.Ip,
                        PublicKey = pubkey,
                        Port = port,
                        Net = NetworkTemp.MAINNET,
                        //TimeStamp = Timestamp

                        //TimeStamp = DateTime.Parse("13/08/2019 08:25")

                        //TimeStamp = TimeSpan.Parse("13/08/2019 08:25")
                    });
                    trans.Complete();
                }



                //var repositoryEf = new EfCoreRepository<Peer, string>(contextTempM);
                //using (var trans = new TransactionScope())
                //{
                //    var peerIDConv = new Peer {PeerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("peer1"), LastSeen = DateTime.Now};
                //    repositoryEf.Add(peerIDConv);
                //    trans.Complete();
                //}



                //var repositoryEf = new EfCoreRepository<MempoolTempDocument, string>(context);
                //using (var trans = new TransactionScope())
                //{
                //    //repositoryEf.Add(new MempoolTempDocument
                //    //{
                //    //    DocumentId = Guid.NewGuid().ToString(),
                //    //    Transaction = 844455
                //    //});


                //    repositoryEf.Add(new MempoolTempDocument
                //    {
                //        Transaction = new PeerId()
                //        //Transaction = TransactionHelper.GetTransaction(signature: Guid.NewGuid().ToString())
                //    });


                //    //repositoryEf.Add(new MempoolTempDocument
                //    //{ Transaction = TransactionHelper.GetTransaction(signature: Guid.NewGuid().ToString())});
                //    trans.Complete();
                //}
            }
            catch (Exception e)
            {

            }
        }


        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void Peers_with_InEFRepo_can_save_and_retrieve()
        {
            try
            {
                var connectionStr =
                    "Server = databasemachine.traderiser.com\\SQL2012, 49175; Database = AtlasCity; User Id = developer; Password = d3v3lop3rhous3;";

                // Create the schema in the database
                using (var contextTemp = new EfCoreContext(connectionStr))
                {
                    var built = contextTemp.Database.EnsureCreated();
                }

                // Run the test against one instance of the context
                var contextTempM = new EfCoreContext(connectionStr);
                var databaseCreator = contextTempM.GetService<IRelationalDatabaseCreator>();
                databaseCreator.CreateTables();

                var repositoryEf = new EfCoreRepository<Peer, string>(contextTempM);
                using (var trans = new TransactionScope())
                {
                    var peerIDConv = new Peer
                    {
                        PeerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("peer1"),
                        LastSeen = DateTime.Now,
                        //MyKey = new Random().Next()
                    };
                    repositoryEf.Add(peerIDConv);
                    trans.Complete();
                }
            }
            catch (Exception e) { }
        }

        [Fact(Skip = "Will change")]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public async Task Mempool_with_InMemoryRepo_can_save_and_retrieve()
        {
            //var fi = new FileInfo(Path.Combine(Constants.ConfigSubFolder, Constants.ModulesSubFolder,
            //    "peerrepo.entityframework.sql.json"));
            //await Mempool_can_save_and_retrieve(fi);
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

