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
using System.Reflection;
using Autofac;
using Catalyst.Common.Container;
using Catalyst.Common.Interfaces;
using Catalyst.Common.Util;

namespace Catalyst.Node
{
    internal static class Program
    {
        private static readonly Kernel Kernel;

        static Program()
        {
            Kernel = Kernel.Initramfs();
            AppDomain.CurrentDomain.UnhandledException += Kernel.LogUnhandledException;
            AppDomain.CurrentDomain.ProcessExit += Kernel.CurrentDomain_ProcessExit;
        }

        public static int Main(string[] args)
        {
            Kernel.Logger.Information("Catalyst.Node started with process id {0}",
                System.Diagnostics.Process.GetCurrentProcess().Id.ToString());
            
            try
            {
                Kernel
                   .WithDataDirectory()
                   .WithNetworksConfigFile()
                   .WithComponentsConfigFile()
                   .WithSerilogConfigFile()
                   .WithConfigCopier()
                   .WithPersistenceConfiguration()
                   .BuildKernel()
                   .StartNode();
                
                Environment.ExitCode = 0;
            }
            catch (Exception e)
            {
                Kernel.Logger.Fatal(e, "Catalyst.Node stopped unexpectedly");
                Environment.ExitCode = 1;
            }

            return Environment.ExitCode;
        }
    }
}
