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
using System.IO;
using System.Threading.Tasks;
using Autofac;
using Catalyst.Abstractions.Cli;
using Catalyst.Core.Lib.Kernel;
using Catalyst.Protocol.Network;
using CommandLine;

namespace Catalyst.Cli
{
    internal static class Program
    {
        private static readonly Kernel Kernel;

        internal class Options
        {
            [Option("network-file", HelpText = "The name of the network file")]
            public string OverrideNetworkFile { get; set; }
        }

        static Program()
        {
            Kernel = Kernel.Initramfs(false, "Catalyst.Cli..log");

            AppDomain.CurrentDomain.UnhandledException += Kernel.LogUnhandledException;
            AppDomain.CurrentDomain.ProcessExit += Kernel.CurrentDomain_ProcessExit;
        }

        /// <summary>
        ///     Main cli loop
        /// </summary>
        public static async Task<int> Main(string[] args)
        {
            // Parse the arguments.
            var result = await Parser.Default.ParseArguments<Options>(args)
               .MapResult(async options => await RunAsync(options).ConfigureAwait(false),
                    response => Task.FromResult(1)).ConfigureAwait(false);
            return Environment.ExitCode = result;
        }

        private static async Task<int> RunAsync(Options options)
        {
            Kernel.Logger.Information("Catalyst.Cli started with process id {0}",
                Process.GetCurrentProcess().Id.ToString());

            try
            {
                await Kernel.WithDataDirectory()
                   .WithSerilogConfigFile()
                   .WithConfigCopier(new CliConfigCopier())
                   .WithConfigurationFile(CliConstants.ShellNodesConfigFile)
                   .WithConfigurationFile(CliConstants.ShellConfigFile)
                   .WithNetworksConfigFile(NetworkType.Devnet, options.OverrideNetworkFile)
                   .BuildKernel()
                   .StartCustomAsync(StartCliAsync);

                return 0;
            }
            catch (Exception e)
            {
                Kernel.Logger.Fatal(e, "Catalyst.Cli stopped unexpectedly");
                return 1;
            }
        }

        private static void StartCli(Kernel kernel)
        {
            const int bufferSize = 1024 * 67 + 128;

            Console.SetIn(
                new StreamReader(
                    Console.OpenStandardInput(bufferSize),
                    Console.InputEncoding, false, bufferSize
                )
            );

            var containerBuilder = kernel.ContainerBuilder;

            CatalystCliBase.RegisterCoreModules(containerBuilder);
            CatalystCliBase.RegisterClientDependencies(containerBuilder);

            kernel.StartContainer();

            kernel.Instance.Resolve<ICatalystCli>()
               .RunConsole(kernel.CancellationTokenProvider.CancellationTokenSource.Token);
        }

        private static async Task StartCliAsync(Kernel kernel)
        {
            await Task.Run(() => StartCli(kernel)).ConfigureAwait(false);
        }
    }
}
