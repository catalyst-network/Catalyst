using System;
using ADL.ADLCli.Services;
using ADL.Node;

namespace ADL.Cli.Shell
{
    public class MainService : ConsoleServiceBase
    {
        private LevelDBStore store;
        private AtlasSystem system;
        
        protected override string Prompt => "atlas";
        public override string ServiceName => "ATLAS-CLI";
        
        protected override bool OnCommand(string[] args)
        {
            switch (args[0].ToLower())
            {
                case "help":
                    return OnHelpCommand(args);
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
        
        protected internal override void OnStart(string[] args)
        {
            store = new LevelDBStore(Path.GetFullPath(Settings.Default.Paths.Chain));
            system = new AtlasSystem(store);
            system.StartNode(Settings.Default.P2P.Port, Settings.Default.P2P.WsPort);
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
            system.StartConsensus(Program.Wallet);
            return true;
        }

        protected internal override void OnStop()
        {
            system.Dispose();
            store.Dispose();
        }
    }
}