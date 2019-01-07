using System;
using System.Net;
using ADL.Exceptions;
using McMaster.Extensions.CommandLineUtils;
using ADL.Shell;
using Akka.Actor;

namespace ADL.Node
{
    public sealed class Program
    {        
        /// <summary>
        /// Main cli loop
        /// </summary>
        /// <param name="args"></param>
        public static int Main(string[] args)
        {   
            AppDomain.CurrentDomain.UnhandledException += Unhandled.UnhandledException;

            var app = new CommandLineApplication();
            
            app.Command("", config =>
            {
                var dfsOption = app.Option("--disable-dfs", "disable dfs", CommandOptionType.NoValue);
                var rpcOption = app.Option("--disable-rpc", "disable rpc", CommandOptionType.NoValue);
                var hostOption = app.Option("-h|--host", "daemon host", CommandOptionType.SingleValue);
                var daemonOption = app.Option("-d|--daemon", "Run as daemon", CommandOptionType.NoValue);
                var envOption = app.Option("-e|--Env", "Specify environment", CommandOptionType.SingleValue);
                var networkOption = app.Option("-n|--Net", "Specify network", CommandOptionType.SingleValue);
                var p2POption = app.Option("--disable-p2p", "disable p2p network", CommandOptionType.NoValue);
                var gossipOption = app.Option("--disable-gossip", "disable gossip", CommandOptionType.NoValue);
                var consensusOption = app.Option("--disable-consensus", "disable consensus", CommandOptionType.NoValue);
                var dataDirOption = app.Option("--data-dir", "Specify a data directory", CommandOptionType.SingleValue);
                var publicKeyOption = app.Option("--public-key", "Specify a public key", CommandOptionType.SingleValue);
                var contractOption = app.Option("--disable-contract", "disable smart contracts", CommandOptionType.NoValue);
                var walletRpcIpOption = app.Option("--data-dir", "Specify a data directory", CommandOptionType.SingleValue);
                var walletRpcPortOption = app.Option("--data-dir", "Specify a data directory", CommandOptionType.SingleValue);
                var payoutAddressOption = app.Option("--payout-address", "Specify a payout address", CommandOptionType.SingleValue);
                
                app.OnExecute(() =>
                {
                    NodeOptions options = new NodeOptions();
                    
                    if (dfsOption.HasValue())
                    {
                        options.Dfs = false;
                    }
                    
                    if (rpcOption.HasValue())
                    {
                        options.Rpc = false;
                    }

                    if (hostOption.HasValue())
                    {
                        options.Host = IPAddress.Parse(hostOption.Value());
                    }
                    
                    if (envOption.HasValue())
                    {
                        var envParam = envOption.Value();

                        switch (envParam)
                        {
                            case "debug":
                                options.Env = 1;
                                break;
                            case "test":
                                options.Env = 2;
                                break;
                            case "benchmark":
                                options.Env = 3;
                                break;
                            case "simulation":
                                options.Env = 4;
                                break;
                            case "prod":
                                options.Env = 5;
                                break;
                            default:
                                options.Env = 1;
                                break;
                        }
                    }
                    
                    if (networkOption.HasValue())
                    {
                        string networkParam = networkOption.Value();

                        switch (networkParam)
                        {
                            case "devnet":
                                options.Network = "devnet";
                                break;
                            case "testnet":
                                options.Network = "testnet";
                                break;
                            case "mainnet":
                                options.Network = "mainnet";
                                break;
                            default:
                                options.Network = "devnet";
                                break;
                        }
                    }
                    
                    if (p2POption.HasValue())
                    {
                        options.Peer = false;
                    }
                    
                    if (gossipOption.HasValue())
                    {
                        options.Gossip = false;
                    }
                    
                    if (consensusOption.HasValue())
                    {
                        options.Consensus = false;
                    }
                    
                    if (dataDirOption.HasValue())
                    {
                        options.DataDir = dataDirOption.Value();
                    }
                    
                    if (publicKeyOption.HasValue())
                    {
                        options.PublicKey = publicKeyOption.Value();
                    }
                    
                    if (contractOption.HasValue())
                    {
                        options.Contract = false;
                    }
                    
                    if (walletRpcIpOption.HasValue())
                    {
                        options.WalletRpcIp = IPAddress.Parse(walletRpcIpOption.Value());
                    }
                    
                    if (walletRpcPortOption.HasValue())
                    {
                        options.WalletRpcPort = uint.Parse(walletRpcPortOption.Value());
                    }
                    
                    if (payoutAddressOption.HasValue())
                    {
                        options.PayoutAddress = payoutAddressOption.Value();
                    }
                    
                    if (daemonOption.HasValue())
                    {
                        RunNodeDemon(options);       
                    }
                    else
                    {
                        RunNodeInteractive(options);                        
                    }
                });
                app.Execute(args); 
            });
            return 1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        private static void RunNodeDemon(NodeOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            AtlasSystem.GetInstance(options);
			Console.WriteLine("daemon Mode");
			while (true){}  
        }

        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        private static void RunNodeInteractive(NodeOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (!options.Daemon)
            {
                AtlasSystem.GetInstance(options);
                var basicShell = new BasicShell();                
                while (basicShell.Run())
                {
                    Console.WriteLine("Shell Mode");
                }   
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        private class BasicShell : ShellBase
        {
            public bool Run()
            {
                return RunConsole();
            }

            /// <inheritdoc />
            /// <summary>
            /// </summary>
            /// <returns></returns>
            public override bool OnGetConfig()
            {
//            Console.WriteLine(Atlas.Kernel.Settings.SerializeSettings());
                return true;
            }
        
            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public override bool OnGetInfo()
            {
                return true;
            }
        
            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public override bool OnGetVersion()
            {
                return true;
            }
            
            /// <inheritdoc />
            /// <summary>
            /// </summary>
            public override bool OnStop(string[] args)
            {
//            Atlas.Dispose();
                return false;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public override bool OnStart(string[] args)
            {
                return true;
            }
        
            /// <summary>
            /// 
            /// </summary>
            /// <param name="args"></param>
            /// <returns></returns>
            public override bool OnStartNode(string[] args)
            {
                return true;
            }
            
            /// <summary>
            /// 
            /// </summary>
            /// <param name="args"></param>
            /// <returns></returns>
            public override bool OnStartWork(string[] args)
            {
                return true;
            }
        
            /// <summary>
            /// 
            /// </summary>
            /// <param name="args"></param>
            /// <returns></returns>
            public override bool OnStopNode(string[] args)
            {
                return true;
            }
            
            /// <summary>
            /// 
            /// </summary>
            /// <param name="args"></param>
            /// <returns></returns>
            public override bool OnStopWork(string[] args)
            {
                return true;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public override bool OnGetMempool()
            {
                return true;
            }
        }
    }
}
