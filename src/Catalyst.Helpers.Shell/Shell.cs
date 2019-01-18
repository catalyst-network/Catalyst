using System.Reflection;
using System.Text.RegularExpressions;
using Catalyst.Helpers.Logger;
using Catalyst.Protocol.Rpc.Node;
using Grpc.Core;

namespace Catalyst.Helpers.Shell
{
    internal sealed class Shell : ShellBase, IAds
    {
        private static RpcServer.RpcServerClient _rpcClient;

        /// <summary>
        /// </summary>
        public Shell()
        {
            Log.Message("Koopa Catalyst.Helpers.Shell Start");
            RunConsole();
        }

        private uint sessionType => 0;
        public override string Prompt => "koopa";
        private Channel SessionHost { set; get; }
        private static string ServiceName => "ADS Advanced Catalyst.Helpers.Shell";

        /// <summary>
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool OnServiceCommand(string[] args)
        {
            switch (args[1].ToLower())
            {
                case "rpc":
                    return OnRpcCommand(args);
                case "dfs":
                    return OnDfsCommand(args);
                case "wallet":
                    return OnWalletCommand(args);
                case "peer":
                    return OnPeerCommand(args);
                case "gossip":
                    return OnGossipCommand(args);
                case "consensus":
                    return OnConsensusCommand(args);
                default:
                    return CommandNotFound(args);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool OnRpcCommand(string[] args)
        {
            switch (args[2].ToLower())
            {
                case "start":
//                    Atlas.StartRpc();
                    return true;
                case "stop":
//                    Atlas.StopRpc();
                    return true;
                case "status":
                    return false;
//                    return RpcStatus(args);
                case "restart":
//                    Catalyst.StopRpc();
//                    Catalyst.StartRpc();
                    return true;
                default:
                    return CommandNotFound(args);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool OnDfsCommand(string[] args)
        {
            switch (args[2].ToLower())
            {
                case "start":
//                    Catalyst.StartDfs();
                    return true;
                case "stop":
                    return false;
//                    return DfsStop(args);
                case "status":
                    return false;
//                    return DfsStatus(args);
                case "restart":
                    return false;
//                    return DfsTestart(args);
                default:
                    return CommandNotFound(args);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool OnWalletCommand(string[] args)
        {
            switch (args[2].ToLower())
            {
                case "start":
                    return false;
//                    return WalletStart(args);
                case "stop":
                    return false;
//                    return WalletStop(args);
                case "status":
                    return false;
//                    return WalletStatus(args);
                case "restart":
                    return false;
//                    return WalletRestart(args);
                default:
                    return CommandNotFound(args);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool OnPeerCommand(string[] args)
        {
            switch (args[2].ToLower())
            {
                case "start":
//                    Catalyst.StartPeer();
                    return true;
                case "stop":
                    return false;
//                    return PeerStop(args);
                case "status":
                    return false;
//                    return PeerStatus(args);
                case "restart":
                    return false;
//                    return PeerRestart(args);
                default:
                    return CommandNotFound(args);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool OnGossipCommand(string[] args)
        {
            switch (args[2].ToLower())
            {
                case "start":
                    return false;
//                    return GossipStart(args);
                case "stop":
                    return false;
//                    return GossipStop(args);
                case "status":
                    return false;
//                    return GossipStatus(args);
                case "restart":
                    return false;
//                    return GossipRestart(args);
                default:
                    return CommandNotFound(args);
            }
        }

        /// <summary>
        ///     Starts the Catalyst consensus module.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool OnConsensusCommand(string[] args)
        {
            switch (args[2].ToLower())
            {
                case "start":
                    ShowPrompt = true;
                    //system.StartConsensus(Program.Wallet);
                    return false;
//                    return ConsensusStart(args);
                case "stop":
                    return false;
//                    return ConsensusStop(args);
                case "status":
                    return false;
//                    return ConsensusStatus(args);
                case "restart":
                    return false;
//                    return ConsensusRestart(args);
                default:
                    return CommandNotFound(args);
            }
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public bool OnHelpCommand()
        {
            var advancedCmds =
                "Advanced Commands:\n" +
                "\tconnect node\n" +
                "\tget delta\n" +
                "\tget mempool\n" +
                "\tregenerate cert\n" +
                "\tmessage sign\n" +
                "\tmessage verify\n" +
                "RPC Commands:\n" +
                "\tservice rpc start\n" +
                "\tservice rpc stop\n" +
                "\tservice rpc status\n" +
                "\tservice rpc restart\n" +
                "Dfs Commands:\n" +
                "\tdfs file put\n" +
                "\tdfs file get\n" +
                "\tservice dfs start\n" +
                "\tservice dfs stop\n" +
                "\tservice dfs status\n" +
                "\tservice dfs restart\n" +
                "Wallet Commands:\n" +
                "\twallet create\n" +
                "\twallet list\n" +
                "\twallet export\n" +
                "\twallet balance\n" +
                "\twallet addresses create\n" +
                "\twallet addresses get\n" +
                "\twallet addresses list\n" +
                "\twallet addresses validate\n" +
                "\twallet privkey import\n" +
                "\twallet privkey export\n" +
                "\twallet transaction create\n" +
                "\twallet transaction sign\n" +
                "\twallet transaction decode \n" +
                "\twallet send to\n" +
                "\twallet send to from\n" +
                "\twallet send many\n" +
                "\twallet send many from\n" +
                "Peer Commands:\n" +
                "\tpeer node crawl\n" +
                "\tpeer node add\n" +
                "\tpeer node remove\n" +
                "\tpeer node blacklist\n" +
                "\tpeer node check health\n" +
                "\tpeer node request\n" +
                "\tpeer node list\n" +
                "\tpeer node info\n" +
                "\tpeer node count\n" +
                "\tpeer node connect\n" +
                "\tservice peer start\n" +
                "\tservice peer stop\n" +
                "\tservice peer status\n" +
                "\tservice peer restart\n" +
                "Gossip Commands:\n" +
                "\tgossip broadcast transaction\n" +
                "\tgossip broadcast delta\n" +
                "\tservice gossip start\n" +
                "\tservice gossip stop\n" +
                "\tservice gossip status\n" +
                "\tservice gossip restart\n" +
                "Consensus Commands:\n" +
                "\tvote fee transaction\n" +
                "\tvote fee dfs\n" +
                "\tvote fee contract\n" +
                "\tservice consensus start\n" +
                "\tservice consensus stop\n" +
                "\tservice consensus status\n" +
                "\tservice consensus restart\n";
            return base.OnHelpCommand(advancedCmds);
        }

        /// <summary>
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public override bool OnCommand(string[] args)
        {
            switch (args[0].ToLower())
            {
                case "connect":
                    return OnConnectNode(args);
                case "start":
                    return OnStart(args);
                case "help":
                    return OnHelpCommand();
                case "message":
                    return OnMessageCommand(args);
                case "service":
                    return OnServiceCommand(args);
                case "rpc":
                    return OnRpcCommand(args);
                case "dfs":
                    return OnDfsCommand(args);
                case "wallet":
                    return OnWalletCommand(args);
                case "peer":
                    return OnPeerCommand(args);
                case "gossip":
                    return OnGossipCommand(args);
                case "consensus":
                    return OnConsensusCommand(args);
                default:
                    return base.OnCommand(args);
            }
        }

        /// <summary>
        ///     @TODO need some auth against the node
        /// </summary>
        /// <returns></returns>
        private bool OnConnectNode(string[] args)
        {
            var ip = args[2];
            var port = args[3];

            var channelTarget = ip + ":" + port;

            SessionHost = new Channel(channelTarget, ChannelCredentials.Insecure);
            _rpcClient = new RpcServer.RpcServerClient(SessionHost);

            return true;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override bool OnStart(string[] args)
        {
            switch (args[1].ToLower())
            {
                case "node":
                    return OnStartNode(args);
                case "work":
                    return OnStartWork(args);
                default:
                    return CommandNotFound(args);
            }
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override bool OnStartNode(string[] args)
        {
            switch (args[1].ToLower())
            {
                case "local":
                    return OnStartNodeLocal(args);
                case "remote":
                    return OnStartNodeRemote(args);
                default:
                    return CommandNotFound(args);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool OnStartNodeLocal(string[] args)
        {
            Log.Message("Not implemented.");
            return false;
        }

        /// <summary>
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool OnStartNodeRemote(string[] args)
        {
            Log.Message("Not implemented.");
            return false;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override bool OnStartWork(string[] args)
        {
            Log.Message("Not implemented.");
            return false;
        }

        /// <summary>
        /// </summary>
        public override bool OnStop(string[] args)
        {
            switch (args[1].ToLower())
            {
                case "node":
                    return OnStopNode(args);
                case "work":
                    return OnStopWork(args);
                default:
                    return base.OnCommand(args);
            }
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override bool OnStopNode(string[] args)
        {
            //            Atlas.Dispose();
            return false;
        }


        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override bool OnStopWork(string[] args)
        {
            Log.Message("Not implemented.");
            return false;
        }

        /// <summary>
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool OnGetCommand(string[] args)
        {
            switch (args[1].ToLower())
            {
                case "delta":
                    return OnGetDelta(args);
                case "mempool":
                    return OnGetMempool();
                default:
                    return base.OnCommand(args);
            }
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override bool OnGetInfo()
        {
            return true;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override bool OnGetVersion()
        {
            return true;
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override bool OnGetConfig()
        {
//            Log.Message(Atlas.Kernel.Settings.SerializeSettings());
            return true;
        }

        /// <inheritdoc />
        /// <summary>
        ///     Parses flags passed with commands.
        /// </summary>
        /// <param name="args"></param>
        /// <param name="regExPattern"></param>
        protected string ParseCmdArgs(string[] args, string regExPattern)
        {
            string argValue = null;
            for (var i = 0; i < args.Length; i++)
                switch (args[i])
                {
                    case var dataDir when new Regex(@"[regExPattern]+").IsMatch(args[i]):
                        argValue = args[i].Replace(regExPattern, "");
                        break;
                    default:
                        return null;
                }
            return argValue;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        protected bool OnBoot()
        {
#if DEBUG
            //@TODO put this in a start node local function
            var assembly = Assembly.LoadFile(@"../../Catalyst.Node/bin/**/CatalystSystem.dll");

//         var atlasSystem =  CatalystSystem.GetInstance(new NodeOptions());
#endif
            return true;
        }

        /// <summary>
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static bool OnGetDelta(string[] args)
        {
            return false;
        }

        /// <summary>
        ///     Get stats about the underlying mempool implementation
        /// </summary>
        /// <returns>Boolean</returns>
        public override bool OnGetMempool()
        {
            var response = _rpcClient.GetMempool(new GetMempoolRequest {Query = true});
            Log.Message(response.ToString());
            return true;
        }

        /// <summary>
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static bool OnMessageCommand(string[] args)
        {
            return false;
        }
    }
}