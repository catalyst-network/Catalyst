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
using System.IO;
using System.Reflection;
using Autofac;
using Catalyst.Common.Config;
using Catalyst.Common.Container;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.Util;

namespace Catalyst.Cli
{
    internal static class Program
    {
        private static readonly KernelBuilder KernelBuilder;

        static Program()
        {
            KernelBuilder = KernelBuilder.GetContainerBuilder(default, "Catalyst.Cli..log");

            AppDomain.CurrentDomain.UnhandledException +=
                (sender, args) => ConsoleProgram.LogUnhandledException(KernelBuilder.Logger, sender, args);
        }

        /// <summary>
        ///     Main cli loop
        /// </summary>
        public static int Main()
        {
            KernelBuilder.Logger.Information("Catalyst.Cli started with process id {0}",
                System.Diagnostics.Process.GetCurrentProcess().Id.ToString());

            const int bufferSize = 1024 * 67 + 128;

            try
            {
                Console.SetIn(
                    new StreamReader(
                        Console.OpenStandardInput(bufferSize),
                        Console.InputEncoding, false, bufferSize
                    )
                );

                var kernel = KernelBuilder
                   .WithDataDirectory()
                   .WithComponentsConfigFile(Constants.ShellComponentsJsonConfigFile)
                   .WithSerilogConfigFile()
                   .WithConfigCopier(new CliConfigCopier())
                   .WithConfigurationFile(Constants.ShellNodesConfigFile)
                   .WithConfigurationFile(Constants.ShellConfigFile)
                   .BuildContainer();
                
                using (kernel.BeginLifetimeScope(MethodBase.GetCurrentMethod().DeclaringType.AssemblyQualifiedName))
                {
                    var shell = kernel.Resolve<ICatalystCli>();

                    shell.RunConsole(KernelBuilder.CancellationTokenProvider.CancellationTokenSource.Token);
                }

                Environment.ExitCode = 0;

                return 0;
            }
            catch (Exception e)
            {
                KernelBuilder.Logger.Fatal(e, "Catalyst.Cli stopped unexpectedly");
                Environment.ExitCode = 1;
            }

            return Environment.ExitCode;
        }

        static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            KernelBuilder.CancellationTokenProvider.CancellationTokenSource.Cancel();
        }
    }
}
