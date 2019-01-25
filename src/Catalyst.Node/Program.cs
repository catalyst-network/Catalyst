using System;
using System.Linq;
using System.Net;
using Catalyst.Helpers.Exceptions;
using Catalyst.Helpers.Logger;
using Catalyst.Helpers.Util;
using McMaster.Extensions.CommandLineUtils;
using Autofac;
using Catalyst.Helpers.FileSystem;
using Catalyst.Helpers.Platform;
using Dawn;

namespace Catalyst.Node
{
    public sealed class Program
    {
        private static CatalystNode Pid { get; set; }

        /// <summary>
        ///     Main cli loop
        /// </summary>
        /// <param name="args"></param>
        public static int Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += Unhandled.UnhandledException;

            var app = new CommandLineApplication();

            app.Command("", config =>
            {
                // Disable services
                var disableDfs = app.Option("--disable-dfs", "disable dfs service", CommandOptionType.NoValue);
                var disablePeer = app.Option("--disable-peer", "disable peer service", CommandOptionType.NoValue);
                var disableGossip = app.Option("--disable-gossip", "disable gossip service", CommandOptionType.NoValue);
                var disableLedger = app.Option("--disable-ledger", "disable ledger service", CommandOptionType.NoValue);
                var disableWallet = app.Option("--disable-wallet", "disable wallet service", CommandOptionType.NoValue);
                var disableMempool = app.Option("--disable-mempool", "disable mempool service", CommandOptionType.NoValue);
                var disbleContract = app.Option("--disable-contract", "disable smart contract service", CommandOptionType.NoValue);
                var disbleConsensus = app.Option("--disable-consensus", "disable consensus service", CommandOptionType.NoValue);
                var disableRpc = app.Option("--disable-rpc", "disable rpc service", CommandOptionType.NoValue);

                // node override options
                var nodeDaemon = app.Option("-d|--node-daemon", "Run as daemon", CommandOptionType.NoValue);
                var nodeEnv = app.Option("-e|--node-env", "Specify environment", CommandOptionType.SingleValue);
                var nodeDataDir = app.Option("--node-data-dir", "Specify a data directory", CommandOptionType.SingleValue);
                var nodeNetwork = app.Option("-n|--node-net", "Specify network", CommandOptionType.SingleValue);
                
                // peer override options
                var peerBindAddress = app.Option("-h|--peer-bind-address", "daemon host", CommandOptionType.SingleValue);
                var peerSeedServers = app.Option("--peer-seed-servers", "Specify seed servers", CommandOptionType.MultipleValue);
                var peerKnownNodes = app.Option("--peer-known-nodes", "Specify known nodes", CommandOptionType.MultipleValue);
                var peerPublicKey = app.Option("--peer-public-key", "Specify a public key", CommandOptionType.SingleValue);
                var peerPayoutAddress = app.Option("--peer-payout-address", "Specify a payout address", CommandOptionType.SingleValue);
                
                // wallet override options
                var walletRpcIpOption = app.Option("--wallet-ip", "Specify a data directory", CommandOptionType.SingleValue);
                var walletRpcPortOption = app.Option("--wallet-port", "Specify a data directory", CommandOptionType.SingleValue);

                app.OnExecute(() =>
                {
                    // get some basic required params
                    int platform = Detection.Os();
                    string dataDir = nodeDataDir.Value() != null ? nodeDataDir.Value() : $"/{Fs.GetUserHomeDir()}/.Catalyst";
                    int env = nodeEnv.Value() != null && ValidEnvPram(nodeEnv.Value()) > 0 ? ValidEnvPram(nodeEnv.Value()) : 1;
                    string network = nodeNetwork.Value() != null && ValidNetworkParam(nodeNetwork.Value()) ? nodeNetwork.Value() : "devnet";
                    
                    // conditionally build NodeOptions object with enabled modules
                    NodeOptions nodeOptions = new NodeOptionsBuilder(env, dataDir, network, platform)
                        .LoadDfsSettings()
                            .When(() => !disableDfs.HasValue())
                        .LoadPeerSettings()
                            .When(() => !disablePeer.HasValue())
                        .LoadGossipSettings()
                            .When(() => !disableGossip.HasValue())
                        .LoadLedgerSettings()
                            .When(() => !disableLedger.HasValue())
                        .LoadWalletSettings()
                            .When(() => !disableWallet.HasValue())
                        .LoadMempoolSettings()
                            .When(() => !disableMempool.HasValue())
                        .LoadContractSettings()
                            .When(() => !disbleContract.HasValue())
                        .LoadConsensusSettings()
                            .When(() => !disbleConsensus.HasValue())
                        .Build();

                    // override settings classes with cli params
//                    if (peerPublicKey.HasValue()) nodeOptions.PeerSettings.PublicKey = peerPublicKey.Value();
//                    if (peerBindAddress.HasValue()) nodeOptions.PeerSettings.BindAddress = IPAddress.Parse(peerBindAddress.Value());
//                    if (peerPayoutAddress.HasValue()) nodeOptions.PeerSettings.PayoutAddress = peerPayoutAddress.Value();
//                    if (walletRpcIpOption.HasValue()) nodeOptions.WalletSettings.WalletRpcIp = IPAddress.Parse(walletRpcIpOption.Value());
//                    if (walletRpcPortOption.HasValue()) nodeOptions.WalletSettings.WalletRpcPort = int.Parse(walletRpcPortOption.Value());
//                    if (peerSeedServers.HasValue()) nodeOptions.PeerSettings.SeedServers.InsertRange(0, peerSeedServers.Values);
//                    if (peerKnownNodes.HasValue()) nodeOptions.PeerSettings.KnownNodes.InsertRange(0, peerKnownNodes.Values);

                    if (nodeDaemon.HasValue())
                        RunNodeDemon(nodeOptions);
                    else
                        RunNodeInteractive(nodeOptions);
                });
                app.Execute(args);
            });
            return 1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="network"></param>
        /// <returns></returns>
        private static bool ValidNetworkParam(string network)
        {
            switch (network)
            {
                case "devnet":
                    return true;
                case "testnet":
                    return true;
                case "mainnet":
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="env"></param>
        /// <returns></returns>
        private static int ValidEnvPram(string env)
        {
            switch (env)
            {
                case "debug":
                    return 1;                    
                case "test":
                    return 2;
                case "benchmark":
                    return 3;
                case "simulation":
                    return 4;
                case "prod":
                    return 5;
                default:
                    return 1;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="options"></param>
        private static void RunNodeDemon(NodeOptions options)
        {
            Guard.Argument(options, nameof(options)).NotNull();
            try
            {
                Pid = CatalystNode.GetInstance(
                    Kernel.GetInstance(options)
                    );
                Pid.Start();
                while (true)
                {
                }
            }
            catch (Exception e)
            {
                LogException.Message("RunNodeDemon: CatalystNode.GetInstance", e);
                Pid.Dispose();
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="options"></param>
        private static void RunNodeInteractive(NodeOptions options)
        {
            Guard.Argument(options, nameof(options)).NotNull();
            Log.Message("Catalyst.Helpers.Shell Mode");

            try
            {
                Pid = CatalystNode.GetInstance(Kernel.GetInstance(options));
                var basicShell = new BasicShell();
                while (basicShell.Run())
                {
                }
            }
            catch (Exception e)
            {
                LogException.Message("RunNodeDemon: CatalystNode.GetInstance", e);
                Pid.Dispose();
            }
        }
    }
}
