using System;
using System.Reflection;
using System.Security;
using System.Text;

namespace ADL.Shell
{
    public abstract class ShellBase : IShell
    {
        public virtual string Prompt => "ADS";
        protected bool ShowPrompt { private get; set; } = true;
        private static string ServiceName => "Atlas Distributed Shell";

        /// <summary>
        /// Prints a list of available cli commands.
        /// </summary>
        /// <returns></returns>
        public bool OnHelpCommand(string advancedCmds="")
        {
            var normalCmds = 
                "Normal Commands:\n" +
                    "\tstart node\n" +
                    "\tstart work\n" +
                    "\tstop node\n" +
                    "\tstop work\n" +
                    "\tget info\n" +
                    "\tget config\n" +
                    "\tget version\n" +
                    "\thelp\n" +
                    "\tclear\n" +
                    "\texit\n";
            
            Console.Write(normalCmds);
            
            if (advancedCmds != "")
            {
                Console.Write(advancedCmds);
            }
            return true;
        }      
        
        /// <summary>
        /// Cli command service router.
        /// </summary>
        /// <param name="args"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public virtual bool OnCommand(string[] args)
        {
            switch (args[0].ToLower())
            {
                case "get":
                    return OnGetCommand(args);
                case "help":
                    return OnHelpCommand();
                case "clear":
                    Console.Clear();
                    return true;
                case "exit":
                    Console.WriteLine("exit trace");
                    return false;
                default:
                    return CommandNotFound(args);
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public abstract bool OnStart(string[] args);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public abstract bool OnStartNode(string[] args);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public abstract bool OnStartWork(string[] args);

        /// <summary>
        /// 
        /// </summary>
        public abstract bool OnStop(string[] args);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public abstract bool OnStopNode(string[] args);


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public abstract bool OnStopWork(string[] args);
        
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
                    return OnGetInfo();
                case "config":
                    return OnGetConfig();
                case "version":
                    return OnGetVersion();
                default:
                    return CommandNotFound(args);
            }
        }
        
        /// <summary>
        /// </summary>
        /// <returns></returns>
        abstract public bool OnGetInfo();
        
        /// <summary>
        /// Prints the current loaded settings.
        /// </summary>
        /// <returns></returns>
        public abstract bool OnGetConfig();
        
        /// <summary>
        /// Prints the current node version.
        /// </summary>
        /// <returns></returns>
        abstract public bool OnGetVersion();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="prompt"></param>
        /// <returns></returns>
        public string ReadPassword(string prompt)
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
        public SecureString ReadSecureString(string prompt)
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
        
        /// <summary>
        /// Runs the main cli ui.
        /// </summary>
        /// <returns></returns>
        public bool RunConsole()
        {
            var running = true;
            
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

#if DEBUG
                foreach(var item in args)
                {
                    Console.Write(item.ToString());
                }          
#endif
                
                try
                {
                    running = OnCommand(args);
                }
                catch (SystemException ex)
                {
                    Console.WriteLine($"error: {ex}");
                    Console.WriteLine("error");
                }
            }
            Console.ResetColor();
            return running;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool CommandNotFound(string[] args)
        {
            Console.WriteLine("error: command not found " + args);
            return true;
        }  
    }
}
