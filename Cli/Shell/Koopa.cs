using System;
using System.Reflection;
using System.Security;
using System.Text;
using ADL.Cli.Interfaces;
using ADL.Cli.Shell.Commands;
using ADL.Node;
using Akka.DI.Core;
using Autofac;
using Microsoft.Extensions.Configuration;
using ADL.Cli.Shell.Commands;

namespace ADL.Cli.Shell
{
    internal class Koopa : ShellBase
    {
//        private LevelDBStore store;
        private AtlasSystem system;

        private static IKernel Kernel { get; set; }

        internal Koopa(IKernel kernel)
        {
            Kernel = kernel;
        }
        
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

        protected internal override void OnStart(string[] args)
        {
//            store = new LevelDBStore(Path.GetFullPath(Settings.Default.Paths.Chain));
            system = new AtlasSystem();
//            system.StartNode(Settings.Default.P2P.Port, Settings.Default.P2P.WsPort);
        }
           
        private bool PrintConfiguration(string[] args)
        {
            var config = Kernel.Container.Resolve<INodeConfiguration>();

            var printConfig = new PrintConfig();
            printConfig.Print(config);
            return true;
        }
        
        private bool OnHelpCommand(string[] args)
        {
            Console.Write(
                "Normal Commands:\n" +
                "\tconfig\n" +
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
                    return OnCommand(args);
            }
        }

        private bool OnStartConsensusCommand(string[] args)
        {
            ShowPrompt = false;
            //system.StartConsensus(Program.Wallet);
            return true;
        }

        protected internal override void OnStop()
        {
//            system.Dispose();
//            store.Dispose();
        }
    }
}