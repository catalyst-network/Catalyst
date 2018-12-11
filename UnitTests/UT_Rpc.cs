using ADL.Node;
using ADL.Protocol.Rpc.Node;
using ADL.Redis;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ADL.UnitTests
{
    [TestClass]
    public class UT_Rpc
    {        
        private static RpcServer.RpcServerClient _rpcClient;

        [ClassInitialize]
        public static void SetUp(TestContext testContext)
        {
            NodeOptions options = new NodeOptions();
            options.Dfs = false;
            options.Env = 1;
            options.Network = "devnet";
            options.PublicKey = "lol";
            options.PayoutAddress = "kek";
                        
            AtlasSystem.GetInstance(options); // run node                                    
            var SessionHost = new Channel("127.0.0.1:42042", ChannelCredentials.Insecure);
            _rpcClient = new RpcServer.RpcServerClient(SessionHost);            
        }

        [TestMethod]
        public void GetMempoolInfo()
        {
            for (int i = 0; i < 1000; i++)
            {
                GetMempoolResponse response = _rpcClient.GetMempool(new GetMempoolRequest {Query = true});
                Assert.IsTrue(int.Parse(response.Info["used_memory_rss"]) > 800000);
            }
        }
    }
}