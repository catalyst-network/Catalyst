#region LICENSE

/**
* Copyright (c) 2019 Catalyst Network
*
* This file is part of Catalyst.Node <https://github.com/catalyst-network/Catalyst.Node>
*
* Catalyst.Node is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 2 of the License, or
* (at your option) any later version.
*
* Catalyst.Node is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with Catalyst.Node. If not, see <https://www.gnu.org/licenses/>.
*/

#endregion

using Autofac;
using Autofac.Core;
using Catalyst.Abstractions;
using Catalyst.Abstractions.Cli;
using Catalyst.Abstractions.DAO;
using Catalyst.Abstractions.IO.Observers;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Lib;
using Catalyst.Core.Lib.Cli;
using Catalyst.Core.Lib.DAO;
using Catalyst.Core.Lib.Kernel;
using Catalyst.Core.Modules.Authentication;
using Catalyst.Core.Modules.Consensus;
using Catalyst.Core.Modules.Cryptography.BulletProofs;
using Catalyst.Core.Modules.Dfs;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Core.Modules.KeySigner;
using Catalyst.Core.Modules.Keystore;
using Catalyst.Core.Modules.Kvm;
using Catalyst.Core.Modules.Ledger;
using Catalyst.Core.Modules.Mempool;
using Catalyst.Core.Modules.P2P.Discovery.Hastings;
using Catalyst.Core.Modules.Sync;
using Catalyst.Core.Modules.Web3;
using Catalyst.Core.Modules.Web3.Options;
using Catalyst.Modules.Network.LibP2P;
using Catalyst.Modules.POA.Consensus;
using Catalyst.Modules.POA.P2P;
using Catalyst.Protocol.Network;
using CommandLine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Core.Lib.FileSystem;

namespace Catalyst.Node.POA.CE
{
    internal class Options
    {
        [Option("ssl-cert-password", HelpText = "The password for ssl cert.  Defaults to prompting for the password.")]
        public string SslCertPassword { get; set; }

        [Option("node-password", HelpText = "The password for the node.  Defaults to prompting for the password.")]
        public string NodePassword { get; set; }

        [Option('o', "overwrite-config", HelpText = "Overwrite the data directory configs.")]
        public bool OverwriteConfig { get; set; }

        [Option( "network-type", HelpText = "The network type")]
        public NetworkType NetworkType { get; set; }
        
        [Option("network-file", HelpText = "The name of the network file")]
        public string OverrideNetworkFile { get; set; }


        [Option('r', "reset", HelpText = "Reset the state")]
        public bool Reset { get; set; }

        [Option('u', "uninstall", HelpText = "Uninstall the node after execution")]
        public bool Uninstall { get; set; }
    }

    public static class Program
    {
        private static readonly Kernel Kernel;

        static Program()
        {
            Kernel = Kernel.Initramfs();

            AppDomain.CurrentDomain.UnhandledException += Kernel.LogUnhandledException;
            AppDomain.CurrentDomain.ProcessExit += Kernel.CurrentDomain_ProcessExit;
        }

        /// <summary>
        ///     For ref what passing custom boot logic looks like, this is the same as Kernel.StartNode()
        /// </summary>
        /// <param name="kernel"></param>
        /// <returns></returns>
        private static async Task CustomBootLogicAsync(Kernel kernel)
        {
            RegisterNodeDependencies(Kernel.ContainerBuilder);
            kernel.StartContainer();
            await kernel.Instance.Resolve<ICatalystNode>().RunAsync(new CancellationToken());
        }

        private static readonly Dictionary<Type, Func<IModule>> DefaultModulesByTypes =
            new Dictionary<Type, Func<IModule>>
            {
                {typeof(CoreLibProvider), () => new CoreLibProvider()},
                {typeof(MempoolModule), () => new MempoolModule()},
                {typeof(ConsensusModule), () => new ConsensusModule()},
                {typeof(SynchroniserModule), () => new SynchroniserModule()},
                {typeof(KvmModule), () => new KvmModule()},
                {typeof(LedgerModule), () => new LedgerModule()},
                {typeof(HashingModule), () => new HashingModule()},
                {typeof(DiscoveryHastingModule), () => new DiscoveryHastingModule()},
                {typeof(BulletProofsModule), () => new BulletProofsModule()},
                {typeof(KeystoreModule), () => new KeystoreModule()},
                {typeof(KeySignerModule), () => new KeySignerModule()},
                {typeof(DfsModule), () => new DfsModule()},
                {typeof(AuthenticationModule), () => new AuthenticationModule()},
                {
                    typeof(ApiModule),
                    () => new ApiModule(new HttpOptions(new IPEndPoint(IPAddress.Any, 5005)), new HttpsOptions(new IPEndPoint(IPAddress.Any, 2053), "cert.pfx"), new List<string> {"Catalyst.Core.Modules.Web3", "Catalyst.Core.Modules.Dfs"})
                },
                {typeof(PoaConsensusModule), () => new PoaConsensusModule()},
                {typeof(PoaP2PModule), () => new PoaP2PModule()},
                {typeof(LibP2PNetworkModule), () => new LibP2PNetworkModule()}
            };

        public static void RegisterNodeDependencies(ContainerBuilder containerBuilder,
            List<IModule> extraModuleInstances = default,
            List<Type> excludedModules = default)
        {
            // core modules
            containerBuilder.RegisterType<CatalystNodePoa>().As<ICatalystNode>();
            containerBuilder.RegisterType<ConsoleUserOutput>().As<IUserOutput>();
            containerBuilder.RegisterType<ConsoleUserInput>().As<IUserInput>();

            // message handlers
            containerBuilder.RegisterAssemblyTypes(typeof(CoreLibProvider).Assembly)
               .AssignableTo<IP2PMessageObserver>().As<IP2PMessageObserver>();

            // DAO MapperInitialisers
            containerBuilder.RegisterAssemblyTypes(typeof(CoreLibProvider).Assembly)
               .AssignableTo<IMapperInitializer>().As<IMapperInitializer>();
            containerBuilder.RegisterType<MapperProvider>().As<IMapperProvider>()
               .SingleInstance();

            var modulesToRegister = DefaultModulesByTypes
               .Where(p => excludedModules == null || !excludedModules.Contains(p.Key))
               .Select(p => p.Value())
               .Concat(extraModuleInstances ?? new List<IModule>());

            foreach (var module in modulesToRegister)
            {
                containerBuilder.RegisterModule(module);
            }
        }

        public static async Task<int> Main(string[] args)
        {
            // Parse the arguments.
            var result = await Parser.Default
               .ParseArguments<Options>(args)
               .MapResult(async options => await RunAsync(options).ConfigureAwait(false),
                    response => Task.FromResult(1)).ConfigureAwait(false);

            return Environment.ExitCode = result;
        }

        private static async Task<int> RunAsync(Options options)
        {
            // uncomment to speed up node launching
            // options.IpfsPassword ??= "ipfs";
            // options.NodePassword ??= "node";
            // options.SslCertPassword ??= "cert";

            Kernel.Logger.Information("Catalyst.Node started with process id {0}",
                Process.GetCurrentProcess().Id.ToString());

            try
            {
                await Kernel
                   .WithDataDirectory()
                   .WithNetworkType(options.NetworkType)
                   .WithNetworksConfigFile(options.OverrideNetworkFile)
                   .WithSerilogConfigFile()
                   .WithValidatorSetFile()
                   .WithConfigCopier(new PoaConfigCopier())
                   .WithPersistenceConfiguration()
                   .BuildKernel(options.OverwriteConfig)
                   .WithPassword(PasswordRegistryTypes.DefaultNodePassword, options.NodePassword)
                   .WithPassword(PasswordRegistryTypes.CertificatePassword, options.SslCertPassword)
                   .Reset(options.Reset)
                   .Uninstall(options.Uninstall)
                   .StartCustomAsync(CustomBootLogicAsync);
                if (options.Uninstall)
                {
                    Kernel.Logger.Debug("Uninstalling the Catalyst Node"); 
                    new FileSystem().GetCatalystDataDir().Delete(true);
                }

                return 0;
            }
            catch (Exception e)
            {
                Kernel.Logger.Fatal(e, "Catalyst.Node stopped unexpectedly");
                return 1;
            }
        }
    }
}
