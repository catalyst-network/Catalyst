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

using System;
using System.Diagnostics;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Kernel;
using Catalyst.Protocol.Common;
using CommandLine;

namespace Catalyst.Node.POA.CE
{
    internal class Options
    {
        [Option("ipfs-password", HelpText = "The password for IPFS.  Defaults to prompting for the password.")]
        public string IpfsPassword { get; set; }
        
        [Option("ssl-cert-password", HelpText = "The password for ssl cert.  Defaults to prompting for the password.")]
        public string SslCertPassword { get; set; }
        
        [Option("node-password", HelpText = "The password for the node.  Defaults to prompting for the password.")]
        public string NodePassword { get; set; }
        
        [Option('o', "overwrite-config", HelpText = "Overwrite the data directory configs.")]
        public bool OverwriteConfig { get; set; }

        [Option("network-file", HelpText = "The name of the network file")]
        public string OverrideNetworkFile { get; set; }
    }
    
    internal static class Program
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
        /// <param name="args"></param>
        /// <returns></returns>

        // private static void CustomBootLogic(Kernel kernel)
        // {
        //     using (var instance = kernel.ContainerBuilder.Build().BeginLifetimeScope(MethodBase.GetCurrentMethod().DeclaringType.AssemblyQualifiedName))
        //     {
        //         instance.Resolve<ICatalystNode>()
        //            .RunAsync(kernel.CancellationTokenProvider.CancellationTokenSource.Token)
        //            .Wait(kernel.CancellationTokenProvider.CancellationTokenSource.Token);
        //     }
        // }
        public static int Main(string[] args)
        {
            // Parse the arguments.
            Parser.Default
               .ParseArguments<Options>(args)
               .WithParsed(Run);

            return Environment.ExitCode;
        }
        
        private static void Run(Options options)
        {
            Kernel.Logger.Information("Catalyst.Node started with process id {0}",
                Process.GetCurrentProcess().Id.ToString());
            
            try
            {
                Kernel
                   .WithDataDirectory()
                   .WithNetworksConfigFile(Network.Devnet, options.OverrideNetworkFile)
                   .WithComponentsConfigFile()
                   .WithSerilogConfigFile()
                   .WithConfigCopier()
                   .WithPersistenceConfiguration()
                   .BuildKernel(options.OverwriteConfig)
                   .WithPassword(PasswordRegistryTypes.DefaultNodePassword, options.NodePassword)
                   .WithPassword(PasswordRegistryTypes.IpfsPassword, options.IpfsPassword)
                   .WithPassword(PasswordRegistryTypes.CertificatePassword, options.SslCertPassword)
                   .StartNode();

                // .StartCustom(CustomBootLogic);
                
                Environment.ExitCode = 0;
            }
            catch (Exception e)
            {
                Kernel.Logger.Fatal(e, "Catalyst.Node stopped unexpectedly");
                Environment.ExitCode = 1;
            }
        }
    }
}
