using System;
using ADL.Cli.Shell.Commands;
using ADL.Node;

namespace ADL.Cli.Shell
{
    internal class Koopa : ShellBase
    {
//        private LevelDBStore store;
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
                case "config":
                    return PrintConfiguration(args);
                default:
                    return base.OnCommand(args);
            }
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
//            system.StartNode(Settings.Default.P2P.Port, Settings.Default.P2P.WsPort);
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
                "\tversion\n" +
                "\tget info\n" +
                "\thelp\n" +
                "\tclear\n" +
                "\texit\n" +
                "Wallet Commands:\n" +
                "\tcreate wallet\n" +
                "\tlist wallets\n" +
                "\texport wallet\n" +
                "\tcreate addresses\n" +
                "\tget addresses\n" +
                "\tlist addresses\n" +
                "\tvalidate addresses\n" +
                "\tget balance\n" +
                "\timport privatekey\n" +
                "\texport privatekey\n" +
                "\tcreate raw transaction\n" +
                "\tsign raw transaction\n" +
                "\tdecode raw tansaction\n" +
                "\tbroadcast raw transaction\n" +
                "\tsend to\n" +
                "\tsend to from\n" +
                "\tsend many\n" +
                "\tSend from many\n" +
                "Node Commands:\n" +
                "\tadd node\n" +
                "\tget peer list\n" +
                "\tget peer info\n" +
                "\tget connection count\n" +
                "Advanced Commands:\n" +
                "\tboot\n" +
                "\tshutdown\n" +
                "\tget delta\n" +
                "\tget mempool\n" +
                "\tsign message\n" +
                "\tverify message\n" +
                "\tstart gossip\n" +
                "\tstart consensus\n" +
                "\tstart rpc\n" +
                "\tstart dfs\n" +
                "\tservice status\n"
            );
            return true;
        }

        /// <summary>
        /// Prints the current loaded settings.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static bool PrintConfiguration(string[] args)
        {
//            var config = Kernel.Container.Resolve<INodeConfiguration>();

            var printConfig = new PrintConfig();
//            printConfig.Print(config);
            return true;
        }
        
        /// <summary>
        /// Service router for a start based commmnd
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
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