using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.Interfaces.Registry;
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
            ILogger logger)
        {
            var simulationNodesFile =
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "simulation.nodes.json");
            var simulationNodes =
                JsonConvert.DeserializeObject<List<SimulationNode>>(File.ReadAllText(simulationNodesFile));
            foreach (var simulationNode in simulationNodes)
            {
                var simpleRpcClient = new SimpleRpcClient(userOutput, passwordRegistry, certificate, logger);
                var clientRpcInfo = new ClientRpcInfo(simulationNode.ToPeerIdentifier(), simpleRpcClient);
                yield return clientRpcInfo;
            }
        }
    }
}
