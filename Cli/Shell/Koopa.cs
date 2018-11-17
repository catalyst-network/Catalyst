using System;
using ADL.Node;
using ADL.Cli.Shell.Commands;

namespace ADL.Cli.Shell
{
    internal class Koopa : ShellBase
    {
        //private LevelDBStore store;
        private AtlasSystem Atlas { get; set; }

        /// <inheritdoc />
        /// <summary>
        /// Cli command service router.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected override bool OnCommand(string[] args)
        {
            switch (args[0].ToLower())
            {
                case "help":
                    return OnHelpCommand(args);
                case "start":
                    return OnStartCommand(args);
                case "get":
                    return OnGetCommand(args);
                default:
                    return base.OnCommand(args);
            }
        }

        /// <summary>
        /// Prints a list of available cli commands.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static bool OnHelpCommand(string[] args)
        {
            Console.Write(
                "Normal Commands:\n" +
                "\tboot\n" +
                "\tshutdown\n" +
                "\tget info\n" +
                "\tget config\n" +
                "\tversion\n" +
                "\thelp\n" +
                "\tclear\n" +
                "\texit\n" +
                "RPC Commands:\n" +
                "\tservice rpc start\n" +
                "\tservice rpc stop\n" +
                "\tservice rpc status\n" +
                "\tservice rpc restart\n" +
                "DFS Commands:\n" +
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
                "\twallet addresses create\n" +
                "\twallet addresses get\n" +
                "\twallet addresses list\n" +
                "\twallet addresses validate\n" +
                "\twallet balance\n" +
                "\twallet import privatekey\n" +
                "\twallet export privatekey\n" +
                "\twallet transaction create\n" +
                "\twallet transaction sign\n" +
                "\twallet transaction decode \n" +
                "\twallet send to\n" +
                "\twallet send to from\n" +
                "\twallet send many\n" +
                "\twallet send from many\n" +
                "Node Commands:\n" +
                "\tnode add\n" +
                "\tnode peer list\n" +
                "\tnode peer info\n" +
                "\tnode connection count\n" +
                "\tservice p2p start\n" +
                "\tservice p2p stop\n" +
                "\tservice p2p status\n" +
                "\tservice p2p restart\n" +
                "Gossip Commands:\n" +
                "\tgossip transaction broadcast\n" +
                "\tservice gossip start\n" +
                "\tservice gossip stop\n" +
                "\tservice gossip status\n" +
                "\tservice gossip restart\n" +
                "Consensus Commands:\n" +
                "\tservice consensus start\n" +
                "\tservice consensus stop\n" +
                "\tservice consensus status\n" +
                "\tservice consensus restart\n" +
                "Advanced Commands:\n" +
                "\tget delta\n" +
                "\tget mempool\n" +
                "\tmessage sign\n" +
                "\tmessage verify\n"
            );
            return true;
        }
        
        /// <summary>
        /// Starts the main ADL configuration.
        /// </summary>
        /// <param name="args"></param>
        protected override void OnStart(string[] args)
        {
//            store = new LevelDBStore(Path.GetFullPath(Settings.Default.Paths.Chain));
            Console.WriteLine("OnStart trace");
            Atlas = new AtlasSystem();
        }
        
        private bool OnStartCommand(string[] args)
        {
            switch (args[1].ToLower())
            {
                case "gossip":
                    return OnStartConsensusCommand(args);
                case "consensus":
                    return OnStartConsensusCommand(args);
                case "rpc":
                    return OnStartRpc(args);
                case "dfs":
                    return OnStartRpc(args);
                default:
                    return OnCommand(args);
            }
        }

        private bool OnGetCommand(string[] args)
        {
            switch (args[1].ToLower())
            {
                case "info":
                    return OnGetInfo(args);      
                case "config":
                    return OnGetConfig(args);          
                default:
                    return OnCommand(args);
            }
        }
        
        /// <summary>
        /// @TODO implement a method of printing node info.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static bool OnGetInfo(string[] args)
        {
            return true;
        }
        
        /// <summary>
        /// Prints the current loaded settings.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static bool OnGetConfig(string[] args)
        {
            GetConfig.Print();
            return true;
        }

        private bool OnStartRpc(string[] args)
        {
            Atlas.StartRcp();
            return true;
        }

        /// <summary>
        /// Starts the ADL consensus module.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private bool OnStartConsensusCommand(string[] args)
        {
            ShowPrompt = true;
            //system.StartConsensus(Program.Wallet);
            return true;
        }

        /// <summary>
        /// Handles a stop command
        /// </summary>
        protected override void OnStop()
        {
//            system.Dispose();
//            store.Dispose();
        }
    }
}