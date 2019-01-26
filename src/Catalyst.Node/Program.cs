using Dawn;
using System;
using System.Net;
using System.Threading;
using Catalyst.Helpers.Logger;
using Catalyst.Helpers.Platform;
using Catalyst.Helpers.Exceptions;
using Catalyst.Helpers.FileSystem;
using McMaster.Extensions.CommandLineUtils;

namespace Catalyst.Node
{
    public sealed class Program : IDisposable
    {
        private static Kernel CatalystNodeKernel { get; set; }

        /// <summary>
        ///     Main cli loop
        /// </summary>
        /// <param name="args"></param>
        public static int Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += Unhandled.UnhandledException;

            var app = new CommandLineApplication();

            try
            {
                app.Command("", config =>
                {
                    // Disable services
                    var disableDfs = app.Option("--disable-dfs", "disable dfs service",
                        CommandOptionType.NoValue);
                    var disablePeer = app.Option("--disable-peer", "disable peer service",
                        CommandOptionType.NoValue);
                    var disableGossip = app.Option("--disable-gossip", "disable gossip service",
                        CommandOptionType.NoValue);
                    var disableLedger = app.Option("--disable-ledger", "disable ledger service",
                        CommandOptionType.NoValue);
                    var disableWallet = app.Option("--disable-wallet", "disable wallet service",
                        CommandOptionType.NoValue);
                    var disableMempool = app.Option("--disable-mempool", "disable mempool service",
                        CommandOptionType.NoValue);
                    var disbleContract = app.Option("--disable-contract", "disable smart contract service",
                        CommandOptionType.NoValue);
                    var disbleConsensus = app.Option("--disable-consensus", "disable consensus service",
                        CommandOptionType.NoValue);
                    var disableRpc = app.Option("--disable-rpc", "disable rpc service",
                        CommandOptionType.NoValue);

                    // node override options
                    var nodeDaemon = app.Option("-d|--node-daemon", "Run as daemon",
                        CommandOptionType.NoValue);
                    var nodeEnv = app.Option("-e|--node-env", "Specify environment",
                        CommandOptionType.SingleValue);
                    var nodeDataDir = app.Option("--node-data-dir", "Specify a data directory",
                        CommandOptionType.SingleValue);
                    var nodeNetwork = app.Option("-n|--node-net", "Specify network",
                        CommandOptionType.SingleValue);

                    // peer override options
                    var peerBindAddress = app.Option("-h|--peer-bind-address", "daemon host",
                        CommandOptionType.SingleValue);
                    var peerSeedServers = app.Option("--peer-seed-servers", "Specify seed servers",
                        CommandOptionType.MultipleValue);
                    var peerKnownNodes = app.Option("--peer-known-nodes", "Specify known nodes",
                        CommandOptionType.MultipleValue);
                    var peerPublicKey = app.Option("--peer-public-key", "Specify a public key",
                        CommandOptionType.SingleValue);
                    var peerPayoutAddress = app.Option("--peer-payout-address", "Specify a payout address",
                        CommandOptionType.SingleValue);

                    // wallet override options
                    var walletRpcIpOption = app.Option("--wallet-ip", "Specify a data directory",
                        CommandOptionType.SingleValue);
                    var walletRpcPortOption = app.Option("--wallet-port", "Specify a data directory",
                        CommandOptionType.SingleValue);

                    app.OnExecute(() =>
                    {
                        // get some basic required params
                        int platform = Detection.Os();
                        
                        // override or get default data dir
                        string dataDir = nodeDataDir.Value() != null
                            ? nodeDataDir.Value()
                            : $"/{Fs.GetUserHomeDir()}/.Catalyst";
                        
                        // override or get default nv
                        string env = nodeEnv.Value() != null 
                            ? nodeEnv.Value()
                            : "debug";
                        
                        // override or get default network
                        string network = nodeNetwork.Value() != null
                            ? nodeNetwork.Value()
                            : "devnet";
                        
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
                        if (peerPublicKey.HasValue()) nodeOptions.PeerSettings.PublicKey = peerPublicKey.Value();
                        if (peerBindAddress.HasValue())
                            nodeOptions.PeerSettings.BindAddress = IPAddress.Parse(peerBindAddress.Value());
                        if (peerPayoutAddress.HasValue())
                            nodeOptions.PeerSettings.PayoutAddress = peerPayoutAddress.Value();
                        if (walletRpcIpOption.HasValue())
                            nodeOptions.WalletSettings.WalletRpcIp = IPAddress.Parse(walletRpcIpOption.Value());
                        if (walletRpcPortOption.HasValue())
                            nodeOptions.WalletSettings.WalletRpcPort = int.Parse(walletRpcPortOption.Value());
                        if (peerSeedServers.HasValue())
                            nodeOptions.PeerSettings.SeedServers.InsertRange(0, peerSeedServers.Values);
                        if (peerKnownNodes.HasValue())
                            nodeOptions.PeerSettings.KnownNodes.InsertRange(0, peerKnownNodes.Values);

                        using (CatalystNodeKernel = new KernelBuilder(nodeOptions)
                            .WithDfsModule()
                            .When(() => !disableDfs.HasValue())
                            .WithPeerModule()
                            .When(() => !disablePeer.HasValue())
                            .WithGossipModule()
                            .When(() => !disableGossip.HasValue())
                            .WithLedgerModule()
                            .When(() => !disableLedger.HasValue())
                            .WithMempoolModule()
                            .When(() => !disableMempool.HasValue())
                            .WithContractModule()
                            .When(() => !disbleContract.HasValue())
                            .WithConsensusModule()
                            .When(() => !disbleConsensus.HasValue())
                            .Build())
                        {
                            CatalystNodeKernel.StartUp();
                            CancellationTokenSource cts = new CancellationTokenSource();

                            while (nodeDaemon.HasValue() ? !cts.Token.IsCancellationRequested : new BasicShell().Run())
                            {
                                if (cts.Token.IsCancellationRequested)
                                {
                                    CatalystNodeKernel.Dispose();
                                    cts.Token.ThrowIfCancellationRequested();
                                }
                                Console.Write(".");
                                Thread.Sleep(100);
                            }                            
                        }
                        return 1;
                    });
                    app.Execute(args);
                });
            }
            catch (Exception e)
            {
                LogException.Message("main app command", e);
                CatalystNodeKernel.Shutdown();
                return 0;
            }
            return 1;
        }
        
        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            CatalystNodeKernel.Shutdown();
        }
    }
}
