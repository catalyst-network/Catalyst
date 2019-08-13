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
using System.Net;
using System.Security;
using System.Threading.Tasks;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Registry;
using Catalyst.Common.Shell;
using Catalyst.Common.Types;
using Catalyst.Node.Rpc.Client;
using CommandLine;
using Newtonsoft.Json;

namespace Catalyst.Simulator
{
    internal static class Program
    {
        public static int Main(string[] args)
        {
            ConsoleUserOutput consoleUserOutput = new ConsoleUserOutput();
            consoleUserOutput.WriteLine("Catalyst Network Simulator");

            var simulationClientFile =
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "simulation.client.json");
            var simulationClient =
                JsonConvert.DeserializeObject<SimulationNode>(File.ReadAllText(simulationClientFile));

            var simulationClientRpcConfig = new NodeRpcConfig
            {
                HostAddress = IPAddress.Parse(simulationClient.Ip),
                Port = simulationClient.Port,
                PublicKey = simulationClient.PublicKey
            };

            var simulationNodesFile =
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "simulation.nodes.json");
            var simulationNodes =
                JsonConvert.DeserializeObject<List<SimulationNode>>(File.ReadAllText(simulationNodesFile));
            var simulationNodePeerIdentifiers = new List<IPeerIdentifier>();

            foreach (var simulationNode in simulationNodes)
            {
                simulationNodePeerIdentifiers.Add(simulationNode.ToPeerIdentifier());
            }

            var passwordRegistry = new PasswordRegistry();
            Parser.Default.ParseArguments<Options>(args).WithParsed(options =>
            {
                if (!string.IsNullOrEmpty(options.NodePassword))
                {
                    AddPassword(passwordRegistry, PasswordRegistryTypes.DefaultNodePassword, options.NodePassword);
                }

                if (!string.IsNullOrEmpty(options.SslCertPassword))
                {
                    AddPassword(passwordRegistry, PasswordRegistryTypes.CertificatePassword, options.SslCertPassword);
                }
            });

            var simulator = new Simulator(passwordRegistry);
            simulator.Simulate(simulationClientRpcConfig, simulationNodePeerIdentifiers).Wait();

            return Environment.ExitCode;
        }

        private static void AddPassword(PasswordRegistry passwordRegistry,
            PasswordRegistryTypes passwordRegistryTypes,
            string password)
        {
            var secureString = new SecureString();
            foreach (var character in password)
            {
                secureString.AppendChar(character);
            }

            passwordRegistry.AddItemToRegistry(passwordRegistryTypes, secureString);
        }
    }
}
