using System;
using ADL.Node;

namespace ADL.Cli.Shell
{
    internal class KoopaShell : ShellBase, IShell
    {
        //private LevelDBStore store;
        private AtlasSystem Atlas { get; set; }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public override bool OnCommand(string[] args)
        {
            switch (args[0].ToLower())
            {
                case "boot":
                    return OnBoot(args);
                case "shutdown":
                    return OnShutdown(args);
                case "get":
                    return OnGetCommand(args);
                case "message":
                    return OnMessageCommand(args);
                case "service":
                    Console.WriteLine("trace1");
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
               
        /// <inheritdoc />
        /// <summary>
        /// Starts the main ADL configuration.
        /// </summary>
        /// <param name="args"></param>
        protected override bool OnBoot(string[] args)
        {
//            store = new LevelDBStore(Path.GetFullPath(Settings.Default.Paths.Chain));
            Console.WriteLine("OnStart trace");
            Atlas = new AtlasSystem();
            return true;
        }
        
        /// <inheritdoc />
        /// <summary>
        /// Shuts down the node
        /// </summary>
        /// <param name="args"></param>
        protected override bool OnShutdown(string[] args)
        {
//            system.Dispose();
//            store.Dispose();
            return false;
        }

        /// <summary>
        /// 
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
                    return OnGetMempool(args);
                default:
                    return base.OnCommand(args);
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static bool OnGetDelta(string[] args)
        {
            return false;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static bool OnGetMempool(string[] args)
        {
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static bool OnMessageCommand(string[] args)
        {
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool OnServiceCommand(string[] args)
        {
            switch (args[1].ToLower())
            {
                case "rpc":
                    Console.WriteLine("trace2");
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
                    return OnCommand(args);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool OnRpcCommand(string[] args)
        {
            switch (args[2].ToLower())
            {
                case "start":
                    Console.WriteLine("trace3");
                    Atlas.StartRcp();
                    return false;
//                    return RpcStart(args);
                case "stop":
                    return false;
//                    return RpcStop(args);
                case "status":
                    return false;
//                    return RpcStatus(args);
                case "restart":
                    return false;
//                    return RpcTestart(args);
            }
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool OnDfsCommand(string[] args)
        {
            switch (args[2].ToLower())
            {
                case "start":
                    return false;
//                    return DfsStart(args);
                case "stop":
                    return false;
//                    return DfsStop(args);
                case "status":
                    return false;
//                    return DfsStatus(args);
                case "restart":
                    return false;
//                    return DfsTestart(args);
            }
            return true;
        }
        
        /// <summary>
        /// 
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
            }
            return true;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool OnPeerCommand(string[] args)
        {
            switch (args[2].ToLower())
            {
                case "start":
                    return false;
//                    return PeerStart(args);
                case "stop":
                    return false;
//                    return PeerStop(args);
                case "status":
                    return false;
//                    return PeerStatus(args);
                case "restart":
                    return false;
//                    return PeerRestart(args);
            }
            return true;
        }
        
        /// <summary>
        /// 
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
            }
            return true;
        }
       
        /// <summary>
        /// Starts the ADL consensus module.
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
            }
            return true;
        }
    }
}
