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
using System.Security.Cryptography.X509Certificates;
using Catalyst.Abstractions.Cli;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.Keystore;
using Catalyst.Simulator.RpcClients;
using Newtonsoft.Json;
using Serilog;

namespace Catalyst.Simulator.Helpers
{
    public static class ConfigHelper
    {
        public static IEnumerable<ClientRpcInfo> GenerateClientRpcInfoFromConfig(IUserOutput userOutput,
            IPasswordRegistry passwordRegistry,
            X509Certificate2 certificate,
            ILogger logger,
            ISigningContextProvider signingContextProvider)
        {
            var simulationNodesFile =
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "simulation.nodes.json");
            var simulationNodes =
                JsonConvert.DeserializeObject<List<SimulationNode>>(File.ReadAllText(simulationNodesFile));
            foreach (var simulationNode in simulationNodes)
            {
                var simpleRpcClient = new SimpleRpcClient(userOutput, passwordRegistry, certificate, logger, signingContextProvider);
                var clientRpcInfo = new ClientRpcInfo(simulationNode.ToPeerIdentifier(), simpleRpcClient);
                yield return clientRpcInfo;
            }
        }
    }
}
