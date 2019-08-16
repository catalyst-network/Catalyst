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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Registry;
using Catalyst.Common.Shell;
using Catalyst.Common.Types;
using Catalyst.Simulator.Helpers;
using CommandLine;
using Newtonsoft.Json;
using Serilog;

namespace Catalyst.Simulator
{
    internal static class Program
    {
        public static int Main(string[] args)
        {
            var logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();

            var consoleUserOutput = new ConsoleUserOutput();
            consoleUserOutput.WriteLine("Catalyst Network Simulator");

            var simulationNodesFile =
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "simulation.nodes.json");
            var simulationNodes =
                JsonConvert.DeserializeObject<List<SimulationNode>>(File.ReadAllText(simulationNodesFile));
            var simulationNodePeerIdentifiers = simulationNodes.Select(x => x.ToPeerIdentifier());

            var passwordRegistry = new PasswordRegistry();
            Parser.Default.ParseArguments<Options>(args).WithParsed(options =>
            {
                if (!string.IsNullOrEmpty(options.NodePassword))
                {
                    PasswordRegistryHelper.AddPassword(passwordRegistry, PasswordRegistryTypes.DefaultNodePassword,
                        options.NodePassword);
                }

                if (!string.IsNullOrEmpty(options.SslCertPassword))
                {
                    PasswordRegistryHelper.AddPassword(passwordRegistry, PasswordRegistryTypes.CertificatePassword,
                        options.SslCertPassword);
                }
            });

            var simulator = new Simulator(consoleUserOutput, passwordRegistry, logger);
            simulator.Simulate(simulationNodePeerIdentifiers).Wait();

            return Environment.ExitCode;
        }
    }
}
