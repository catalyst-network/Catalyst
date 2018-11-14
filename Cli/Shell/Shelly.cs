using System;
using System.Reflection;
using System.Security;
using System.Text;
using ADL.Cli.Interfaces;
using ADL.Cli.Shell.Commands;
using ADL.Node;
using Autofac;

namespace ADL.Cli.Shell
{
    internal class Shelly : IShellBase
    {
//        private LevelDBStore store;
        private AtlasSystem system;

        private static IContainer Kernel { get; set; }

        private bool ShowPrompt { get; set; } = true;
               
        private string Prompt => "shelly";

        private string ServiceName => "Atlas Distributed Ledger";
        
        internal Shelly(IContainer kernel)
        {
            Kernel = kernel;
        }
        
        public void Run(string[] args)
        {
            OnStart(args);
            RunConsole();
            OnStop();
        }
        
        private void OnStart(string[] args)
        {
//            store = new LevelDBStore(Path.GetFullPath(Settings.Default.Paths.Chain));
            system = new AtlasSystem();
//            system.StartNode(Settings.Default.P2P.Port, Settings.Default.P2P.WsPort);
        }
       
        private bool OnCommand(string[] args)
        {
            switch (args[0].ToLower())
            {
                case "help":
                    return OnHelpCommand(args);
                case "print":
                    return PrintConfiguration(args);
                case "start":
                    return OnStartCommand(args);
                case "clear":
                    Console.Clear();
                    return true;
                case "exit":
                    return false;
                case "version":
                    Console.WriteLine(Assembly.GetEntryAssembly().GetName().Version);
                    return true;
                default:
                    Console.WriteLine("error: command not found " + args[0]);
                    return true;
            }
        }

        private bool PrintConfiguration(string[] args)
        {
            var config = Kernel.Resolve<INodeConfiguration>();

            var printConfig = new PrintConfig();
            printConfig.Print(config);
            return true;
        }
        
        private void RunConsole()
        {
            bool running = true;
#if NET461
            Console.Title = ServiceName;
#endif
            Console.OutputEncoding = Encoding.Unicode;

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Version ver = Assembly.GetEntryAssembly().GetName().Version;
            Console.WriteLine($"{ServiceName} Version: {ver}");
            Console.WriteLine();

            while (running)
            {
                if (ShowPrompt)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write($"{Prompt}> ");
                }

                Console.ForegroundColor = ConsoleColor.Yellow;
                string line = Console.ReadLine()?.Trim();
                if (line == "is it friday?")
                {
                    if (System.DateTime.Now.DayOfWeek.ToString() == "Friday")
                    {
                        Console.WriteLine("yes");
                    }
                    else
                    {
                        Console.WriteLine("no");
                    }
                    continue;
                }
                if (line == null) break;
                Console.ForegroundColor = ConsoleColor.White;
                string[] args = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (args.Length == 0)
                    continue;
                try
                {
                    running = OnCommand(args);
                }
                catch (Exception ex)
                {
#if DEBUG
                    Console.WriteLine($"error: {ex.Message}");
#else
                    Console.WriteLine("error");
#endif
                }
            }

            Console.ResetColor();
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
                    return OnCommand(args);
            }
        }

        private bool OnStartConsensusCommand(string[] args)
        {
            ShowPrompt = false;
//            system.StartConsensus(Program.Wallet);
            return true;
        }

        private void OnStop()
        {
//            system.Dispose();
//            store.Dispose();
        }
    }
}