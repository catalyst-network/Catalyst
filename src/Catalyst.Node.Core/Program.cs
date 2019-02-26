using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using Autofac;
using Autofac.Configuration;
using Autofac.Extensions.DependencyInjection;
using AutofacSerilogIntegration;
using Catalyst.Node.Common.Modules.Gossip;
using Catalyst.Node.Core.Config;
using Catalyst.Node.Core.Helpers;
using Catalyst.Node.Core.Helpers.Shell;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Catalyst.Node.Core
{
    public static class Program
    {
        private static readonly ILogger Logger;
        private static readonly string LifetimeTag;
        private static readonly string ExecutionDirectory;

        static Program()
        {
            var declaringType = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType;
            Logger = Log.Logger.ForContext(declaringType);
            LifetimeTag = declaringType.AssemblyQualifiedName;
            ExecutionDirectory = Path.GetDirectoryName(declaringType.Assembly.Location);
        }
        private static CatalystNode CatalystNode { get; set; }

        /// <summary>
        ///     Main cli loop
        /// </summary>
        /// <param name="args"></param>
        public static int Main_Old(string[] args)
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
                                  // override or get default data dir
                                  var dataDir = nodeDataDir.Value() != null
                                                    ? nodeDataDir.Value()
                                                    : new Fs().GetCatalystHomeDir().ToString();

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

                                  new ConfigCopier().RunConfigStartUp(dataDir, networkOption);
                                  // conditionally build NodeOptions object with enabled modules
                                  var nodeOptions =
                                      new NodeOptionsBuilder(env, dataDir, network)
                                         .LoadPeerSettings()
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

        public static int Main(string[] args)
        {
            try
            {
                //Enable after checking safety implications, if plugins become important.
                //AssemblyLoadContext.Default.Resolving += TryLoadAssemblyFromExecutionDirectory;

                //TODO: allow targeting different folder using CommandLine
                var targetConfigFolder = new Fs().GetCatalystHomeDir().FullName;
                var network = NodeOptions.Networks.devnet;

                var configCopier = new ConfigCopier();
                configCopier.RunConfigStartUp(targetConfigFolder, network, overwrite:true);

                var config = new ConfigurationBuilder()
                   .AddJsonFile(Path.Combine(targetConfigFolder, Constants.ComponentsJsonConfigFile))
                   .AddJsonFile(Path.Combine(targetConfigFolder, Constants.SerilogJsonConfigFile))
                   .Build();

                //.Net Core service collection
                var serviceCollection = new ServiceCollection();
                //Add .Net Core services (if any) first
                //serviceCollection.AddLogging().AddDistributedMemoryCache();

                // register components from config file
                var configurationModule = new ConfigurationModule(config);
                var containerBuilder = new ContainerBuilder();
                containerBuilder.RegisterModule(configurationModule);

                var loggerConfiguration = new LoggerConfiguration().ReadFrom.Configuration(configurationModule.Configuration);
                Log.Logger = loggerConfiguration.WriteTo
                   .File(Path.Combine(targetConfigFolder, "Catalyst.Node..log"), rollingInterval: RollingInterval.Day)
                   .CreateLogger();
                containerBuilder.RegisterLogger();

                var container = containerBuilder.Build();
                using (var scope = container.BeginLifetimeScope(LifetimeTag, 
                    //Add .Net Core serviceCollection to the Autofac container.
                    b => { b.Populate(serviceCollection, LifetimeTag); }))
                {
                    var serviceProvider = new AutofacServiceProvider(scope);
                    var gossipSingleton = serviceProvider.GetService<IGossip>();
                    Log.Logger.Information("Gossip singleton is named {0}", gossipSingleton.Name);
                }
                Environment.ExitCode = 0;
            }
            catch (Exception e)
            {
                Log.Logger.Error(e, "Catalyst.Node failed to start.");
                Environment.ExitCode = 1;
            }

            return Environment.ExitCode;
        }

        public static Assembly TryLoadAssemblyFromExecutionDirectory(AssemblyLoadContext context,
            AssemblyName assemblyName)
        {
            try
            {
                var assemblyFilePath = Path.Combine(ExecutionDirectory, $"{assemblyName.Name}.dll");
                Logger.Debug("Resolving assembly {0} from file {1}", assemblyName, assemblyFilePath);
                var assembly = context.LoadFromAssemblyPath(assemblyFilePath);
                return assembly;
            }
            catch (Exception e)
            {
                Logger.Warning(e, "Failed to load assembly {0} from file {1}.", e);
                return null;
            }
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