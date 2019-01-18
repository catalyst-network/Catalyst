using Akka.TestKit.VsTest;
using Catalyst.Protocol.Rpc.Node;
using Grpc.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Catalyst.Node.Modules.Core.Rpc.UnitTests
{
    [TestClass]
    public class UT_Rpc : TestKit
    {
        private static RpcServer.RpcServerClient _rpcClient;

        [ClassInitialize]
        public static void SetUp(TestContext testContext)
        {
            var options = new NodeOptions();
            options.Dfs = false;
            options.Env = 1;
            options.Network = "devnet";
            options.PublicKey = "lol";
            options.PayoutAddress = "kek";
            options.WalletRpcPort = 0;

            CatalystNode.GetInstance(options);
            var SessionHost = new Channel("127.0.0.1:42042", ChannelCredentials.Insecure);
            _rpcClient = new RpcServer.RpcServerClient(SessionHost);
        }

        [TestCleanup]
        public void Cleanup()
        {
            Shutdown();
        }

        [TestMethod]
        public void GetMempoolInfo()
        {
            for (var i = 0; i < 1000; i++)
            {
                var response = _rpcClient.GetMempool(new GetMempoolRequest {Query = true});
                Assert.IsTrue(int.Parse(response.Info["used_memory_rss"]) > 800000);
            }
        }

//        [TestMethod]
//        public void TaskHandlerAsk()
//        {
//            var actor = ActorOfAsTestActorRef<RpcTaskHandlerActor>();
//            var task = actor.Ask<GetMempoolResponse>(new GetMempoolRequest {Query = true});
//            var response = task.Result;
//            Assert.IsTrue(int.Parse(response.Info["used_memory_rss"]) > 800000);
//        }
//        
//        [TestMethod]
//        public void TaskHandlerPoisonPill()
//        {
//            var actor = ActorOfAsTestActorRef<RpcTaskHandlerActor>();
//            var probe = CreateTestProbe();
//            probe.Watch(actor);            
//            actor.Tell(PoisonPill.Instance);
//            var msg = probe.ExpectMsg<Terminated>();
//            Assert.AreEqual(msg.ActorRef, actor);
//        }
    }
}