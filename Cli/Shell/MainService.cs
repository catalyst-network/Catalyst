using System;
using ADL.ADLCli.Services;
using ADL.Node;

namespace ADL.Cli.Shell
{
    public class MainService : ConsoleServiceBase
    {
//        private LevelDBStore store;
        private AtlasSystem system;
        
        protected override string Prompt => "atlas";
        public override string ServiceName => "Atlas Distributed Ledger";
        
        protected internal override void OnStart(string[] args)
        {
//            store = new LevelDBStore(Path.GetFullPath(Settings.Default.Paths.Chain));
            system = new AtlasSystem();
//            system.StartNode(Settings.Default.P2P.Port, Settings.Default.P2P.WsPort);
        }
       
        protected override bool OnCommand(string[] args)
        {
            switch (args[0].ToLower())
            {
                case "start":
                    return OnStartCommand(args);
                default:
                    return base.OnCommand(args);
            }
        }
        
        private bool OnHelpCommand(string[] args)
        {
            Console.Write(
                "Normal Commands:\n" +
                "\tversion\n" +
                "\thelp\n" +
                "\tclear\n" +
                "\texit\n" +
                "Wallet Commands:\n" +
                "Node Commands:\n" +
                "Advanced Commands:\n" +
                "\tstart consensus\n");
            return true;
        }

        private bool OnStartCommand(string[] args)
        {
            switch (args[1].ToLower())
            {
                case "consensus":
                    return OnStartConsensusCommand(args);
                default:
                    return base.OnCommand(args);
            }
        }

        private bool OnStartConsensusCommand(string[] args)
        {
            ShowPrompt = false;
//            system.StartConsensus(Program.Wallet);
            return true;
        }

        protected internal override void OnStop()
        {
//            system.Dispose();
//            store.Dispose();
        }
    }
}