using System;
using System.Text;
using System.Security;
using System.Reflection;
using System.Collections.Generic;
using ADL.Cli.Shell.Commands.Normal;

namespace ADL.Cli.Shell
{
    public abstract class ShellBase
    {
        protected bool ShowPrompt { private get; set; } = true;

        private static string Prompt => "koopa";

        private static string ServiceName => "Atlas Distributed Shell";
        
        protected abstract void OnStart();
        
        protected abstract void OnStop();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected abstract bool OnBoot(string[] args);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        protected abstract bool OnShutdown(string[] args);
        
        /// <summary>
        /// Cli command service router.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public virtual bool OnCommand(string[] args)
        {
            switch (args[0].ToLower())
            {
                case "boot":
                    return OnBoot(args);
                case "shutdown":
                    return OnShutdown(args);
                case "get":
                    return OnGetCommand(args);
                case "help":
                    return OnHelpCommand(args);
                case "clear":
                    Console.Clear();
                    return true;
                case "exit":
                    return false;
                default:
                    return CommandNotFound(args);
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private bool OnGetCommand(string[] args)
        {
            switch (args[1].ToLower())
            {
                case "info":
                    return OnGetInfo(args);
                case "config":
                    return OnGetConfig(args);
                case "version":
                    return OnGetVersion(args);
                default:
                    return CommandNotFound(args);
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
                "\tget version\n" +
                "\thelp\n" +
                "\tclear\n" +
                "\texit\n" +
                "Advanced Commands:\n" +
                "\tget delta\n" +
                "\tget mempool\n" +
                "\tmessage sign\n" +
                "\tmessage verify\n" +  
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
                "\tservice consensus restart\n"
            );
            return true;
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

        /// <summary>
        /// Prints the current node version.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static bool OnGetVersion(string[] args)
        {
            Console.WriteLine(Assembly.GetEntryAssembly().GetName().Version);
            return true;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="prompt"></param>
        /// <returns></returns>
        private static string ReadPassword(string prompt)
        {
            const string t = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";
            StringBuilder sb = new StringBuilder();
            ConsoleKeyInfo key;
            Console.Write(prompt);
            Console.Write(": ");

            Console.ForegroundColor = ConsoleColor.Yellow;

            do
            {
                key = Console.ReadKey(true);
                if (t.IndexOf(key.KeyChar) != -1)
                {
                    sb.Append(key.KeyChar);
                    Console.Write('*');
                }
                else if (key.Key == ConsoleKey.Backspace && sb.Length > 0)
                {
                    sb.Length--;
                    Console.Write(key.KeyChar);
                    Console.Write(' ');
                    Console.Write(key.KeyChar);
                }
            } while (key.Key != ConsoleKey.Enter);

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();
            return sb.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="prompt"></param>
        /// <returns></returns>
        private static SecureString ReadSecureString(string prompt)
        {
            const string t = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";
            var securePwd = new SecureString();
            ConsoleKeyInfo key;
            Console.Write(prompt);
            Console.Write(": ");

            Console.ForegroundColor = ConsoleColor.Yellow;

            do
            {
                key = Console.ReadKey(true);
                if (t.IndexOf(key.KeyChar) != -1)
                {
                    securePwd.AppendChar(key.KeyChar);
                    Console.Write('*');
                }
                else if (key.Key == ConsoleKey.Backspace && securePwd.Length > 0)
                {
                    securePwd.RemoveAt(securePwd.Length - 1);
                    Console.Write(key.KeyChar);
                    Console.Write(' ');
                    Console.Write(key.KeyChar);
                }
            } while (key.Key != ConsoleKey.Enter);

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();
            securePwd.MakeReadOnly();
            return securePwd;
        }

        public void Run()
        {
            Console.WriteLine("run trace");
            OnStart();
            RunConsole();
            OnStop();
        }
        
        /// <summary>
        /// Runs the main cli ui.
        /// </summary>
        /// <returns></returns>
        private void RunConsole()
        {
            var running = true;
#if NET461
            Console.Title = ServiceName;
#endif
            Console.OutputEncoding = Encoding.Unicode;

            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            Version ver = Assembly.GetEntryAssembly().GetName().Version;
            Console.WriteLine($"{ServiceName} Version: {ver}");
            Console.WriteLine();

            while (running)
            {
                if (ShowPrompt)
                {
                    Console.ForegroundColor = ConsoleColor.DarkBlue;
                    Console.Write($"{Prompt}> ");
                }

                Console.ForegroundColor = ConsoleColor.DarkYellow;
                var line = Console.ReadLine()?.Trim();
                if (line == null) break;
                Console.ForegroundColor = ConsoleColor.White;
                var args = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected bool CommandNotFound(string[] args)
        {
            Console.WriteLine("error: command not found " + args);
            return true;
        }
    }
}
