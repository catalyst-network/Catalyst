using System;
using System.IO;
using System.Net;
using System.Threading;
using Autofac;
using Catalyst.Node.Core.Config;
using Catalyst.Node.Core.Helpers;
using Catalyst.Node.Core.Helpers.Platform;
using Catalyst.Node.Core.Helpers.Shell;
using McMaster.Extensions.CommandLineUtils;
using Serilog;

namespace Catalyst.Node.Core
{
    public static class Program
    {
        private static readonly ILogger Logger = Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static CatalystNode CatalystNode { get; set; }

        /// <summary>
        ///     Main cli loop
        /// </summary>
        /// <param name="args"></param>
        public static int Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += LogUnhandledException;

            var cli = new CommandLineApplication();
            var cts = new CancellationTokenSource();

            try
            {
                cli.Command("", config =>
                    {
                        // Disable services
                        var disableDfs = cli.Option("--disable-dfs", "disable dfs service",
                            CommandOptionType.NoValue);
                        var disableGossip = cli.Option("--disable-gossip", "disable gossip service",
                            CommandOptionType.NoValue);
                        var disableLedger = cli.Option("--disable-ledger", "disable ledger service",
                            CommandOptionType.NoValue);
                        var disableWallet = cli.Option("--disable-wallet", "disable wallet service",
                            CommandOptionType.NoValue);
                        var disableMempool = cli.Option("--disable-mempool", "disable mempool service",
                            CommandOptionType.NoValue);
                        var disbleContract = cli.Option("--disable-contract",
                            "disable smart contract service",
                            CommandOptionType.NoValue);
                        var disbleConsensus = cli.Option("--disable-consensus", "disable consensus service",
                            CommandOptionType.NoValue);
                        var disableRpc = cli.Option("--disable-rpc", "disable rpc service",
                            CommandOptionType.NoValue);
                        // node override options
                        var nodeDaemon = cli.Option("-d|--node-daemon", "Run as daemon",
                            CommandOptionType.NoValue);
                        var nodeEnv = cli.Option("-e|--node-env", "Specify environment",
                            CommandOptionType.SingleValue);
                        var nodeDataDir = cli.Option("--node-data-dir", "Specify a data directory",
                            CommandOptionType.SingleValue);
                        var nodeNetwork = cli.Option("-n|--node-net", "Specify network",
                            CommandOptionType.SingleValue);
                        // peer override options
                        var peerBindAddress = cli.Option("-h|--peer-bind-address", "daemon host",
                            CommandOptionType.SingleValue);
                        var peerSeedServers = cli.Option("--peer-seed-servers", "Specify seed servers",
                            CommandOptionType.MultipleValue);
                        var peerKnownNodes = cli.Option("--peer-known-nodes", "Specify known nodes",
                            CommandOptionType.MultipleValue);
                        var peerPublicKey = cli.Option("--peer-public-key", "Specify a public key",
                            CommandOptionType.SingleValue);
                        var peerPayoutAddress = cli.Option("--peer-payout-address",
                            "Specify a payout address",
                            CommandOptionType.SingleValue);
                        // wallet override options
                        var walletRpcIpOption = cli.Option("--wallet-ip", "Specify a data directory",
                            CommandOptionType.SingleValue);
                        var walletRpcPortOption = cli.Option("--wallet-port", "Specify a data directory",
                            CommandOptionType.SingleValue);

                        cli.OnExecute(() =>
                              {
                                  // get some basic required params
                                  var platform = Detection.Os();

                                  // override or get default data dir
                                  var dataDir = nodeDataDir.Value() != null
                                                    ? nodeDataDir.Value()
                                                    : (new Fs()).GetCatalystHomeDir().ToString();

                                  // override or get default nv
                                  var env = nodeEnv.Value() != null
                                                ? nodeEnv.Value()
                                                : "debug";

                                  // override or get default network
                                  var network = nodeNetwork.Value() != null
                                                    ? nodeNetwork.Value()
                                                    : "devnet";
                                  if (!Enum.TryParse(network,
                                      out NodeOptions.Networks networkOption))
                                  {
                                      networkOption = NodeOptions.Networks.devnet;
                                  }

                                  var fs = new Fs();
                                  new ConfigCopier(fs).RunConfigStartUp(dataDir, networkOption);
                                  // conditionally build NodeOptions object with enabled modules
                                  var nodeOptions =
                                      new NodeOptionsBuilder(env, dataDir, network, platform)
                                         .LoadPeerSettings()
                                         .LoadDfsSettings()
                                             .When(() => !disableDfs.HasValue())
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
                                  if (peerPublicKey.HasValue())
                                  {
                                      nodeOptions.PeerSettings.PublicKey = peerPublicKey.Value();
                                  }

                                  if (peerBindAddress.HasValue())
                                  {
                                      nodeOptions.PeerSettings.BindAddress = IPAddress.Parse(peerBindAddress.Value());
                                  }

                                  if (peerPayoutAddress.HasValue())
                                  {
                                      nodeOptions.PeerSettings.PayoutAddress = peerPayoutAddress.Value();
                                  }

                                  if (walletRpcIpOption.HasValue())
                                  {
                                      nodeOptions.WalletSettings.WalletRpcIp = IPAddress.Parse(walletRpcIpOption.Value());
                                  }

                                  if (walletRpcPortOption.HasValue())
                                  {
                                      nodeOptions.WalletSettings.WalletRpcPort = int.Parse(walletRpcPortOption.Value());
                                  }

                                  if (peerSeedServers.HasValue())
                                  {
                                      nodeOptions.PeerSettings.SeedServers.InsertRange(0, peerSeedServers.Values);
                                  }

                                  if (peerKnownNodes.HasValue())
                                  {
                                      nodeOptions.PeerSettings.KnownNodes.InsertRange(0, peerKnownNodes.Values);
                                  }

                                  using (var kernel = new KernelBuilder(nodeOptions)
                                                     .WithDfsModule()
                                                     .When(() => !disableDfs.HasValue())
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
                                                     .Build()
                                  )
                                  {
                                      CatalystNode = CatalystNode.GetInstance(kernel);
                                      using (CatalystNode.Kernel.Container.BeginLifetimeScope())
                                      {
                                          while (nodeDaemon.HasValue()
                                                     ? !cts.Token.IsCancellationRequested
                                                     : new Shell().RunConsole()
                                          ) //@TODO get a list of loaded modules and pass in here so we can enable/disable menu options.
                                          {
                                              if (cts.Token.IsCancellationRequested)
                                              {
                                                  CatalystNode.Kernel.Dispose();
                                                  cts.Token.ThrowIfCancellationRequested();
                                              }
#if DEBUG
                                              Console.Write(".");
#endif
                                              Thread.Sleep(100);
                                          }
                                      }
                                  }

                                  return 1;
                              });
                        cli.Execute(args);
                    });
            }
            catch (Exception e)
            {
                Logger.Error(e, "main app command");
                cts.Cancel();
                CatalystNode?.Dispose();
                return 0;
            }

            return 1;
        }

        public static void LogUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                Log.Logger.Fatal("Unhandled exception, Terminating", e);
            }
            catch
            {
                using (var fs = new FileStream("error.log", FileMode.Create, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(fs))
                {
                    writer.WriteLine(e.ExceptionObject.ToString());
                    writer.WriteLine($"IsTerminating: {e.IsTerminating}");
                }
            }
        }
    }
}