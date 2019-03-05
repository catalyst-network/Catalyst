using System.Reflection;
using System.Text.RegularExpressions;
using Serilog;

namespace Catalyst.Node.Common.Helpers.Shell
{
    public sealed class Shell : ShellBase
    {
        private static readonly ILogger Logger = Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// </summary>
        public Shell()
        {
            Logger.Information("Koopa Shell Start");
            RunConsole();
        }

        /// <summary>
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static bool OnServiceCommand(string[] args)
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
                default:
                    return CommandNotFound(args);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static bool OnRpcCommand(string[] args)
        {
            switch (args[2].ToLower())
            {
                case "start":
                    return true;
                case "stop":
                    return true;
                case "status":
                    return false;
                case "restart":
                    return true;
                default:
                    return CommandNotFound(args);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static bool OnDfsCommand(string[] args)
        {
            switch (args[2].ToLower())
            {
                case "start":
                    return true;
                case "stop":
                    return false;
                case "status":
                    return false;
                case "restart":
                    return false;
                default:
                    return CommandNotFound(args);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static bool OnWalletCommand(string[] args)
        {
            switch (args[2].ToLower())
            {
                case "start":
                    return false;
                case "stop":
                    return false;
                case "status":
                    return false;
                case "restart":
                    return false;
                default:
                    return CommandNotFound(args);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static bool OnPeerCommand(string[] args)
        {
            switch (args[2].ToLower())
            {
                case "start":
                    return true;
                case "stop":
                    return false;
                case "status":
                    return false;
                case "restart":
                    return false;
                default:
                    return CommandNotFound(args);
            }
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        private bool OnHelpCommand()
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
                "IpfsDfs Commands:\n" +
                "\tdfs file put\n" +
                "\tdfs file get\n" +
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
                "Consensus Commands:\n" +
                "\tvote fee transaction\n" +
                "\tvote fee dfs\n" +
                "\tvote fee contract\n";
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
                default:
                    return base.OnCommand(args);
            }
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        private static bool OnConnectNode(string[] args)
        {
            var ip = args[2];
            var port = args[3];
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
        private static bool OnStartNodeLocal(string[] args)
        {
            Logger.Error("Not implemented.");
            return false;
        }

        /// <summary>
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static bool OnStartNodeRemote(string[] args)
        {
            Logger.Error("Not implemented.");
            return false;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override bool OnStartWork(string[] args)
        {
            Logger.Error("Not implemented.");
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
        public override bool OnStopNode(string[] args) { return false; }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override bool OnStopWork(string[] args)
        {
            Logger.Error("Not implemented.");
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
        protected override bool OnGetInfo() { return true; }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        protected override bool OnGetVersion() { return true; }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <returns></returns>
        protected override bool OnGetConfig() { return false; }

        /// <summary>
        ///     Parses flags passed with commands.
        /// </summary>
        /// <param name="args"></param>
        /// <param name="regExPattern"></param>
        private string ParseCmdArgs(string[] args, string regExPattern)
        {
            string argValue = null;
            foreach (var t in args)
                switch (t)
                {
                    case var dataDir when new Regex(@"[regExPattern]+").IsMatch(t):
                        argValue = t.Replace(regExPattern, "");
                        break;
                    default:
                        return null;
                }
            return argValue;
        }

        /// <summary>
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static bool OnGetDelta(string[] args) { return false; }

        /// <summary>
        ///     Get stats about the underlying mempool implementation
        /// </summary>
        /// <returns>Boolean</returns>
        protected override bool OnGetMempool() { return false; }

        /// <summary>
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static bool OnMessageCommand(string[] args) { return false; }
    }
}