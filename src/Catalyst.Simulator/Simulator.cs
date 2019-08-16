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
using System.Linq;
using System.Threading.Tasks;
using Catalyst.Common.Cryptography;
using Catalyst.Common.FileSystem;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.Registry;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.Simulator.Helpers;
using Serilog;

namespace Catalyst.Simulator
{
    public class Simulator
    {
        private readonly Random _random;
        private readonly ILogger _logger;
        private readonly IUserOutput _userOutput;
        private readonly SimpleRpcClient _simpleRpcClient;

        public Simulator(IUserOutput userOutput, IPasswordRegistry passwordRegistry, ILogger logger)
        {
            _logger = logger;
            _random = new Random();
            _userOutput = userOutput;

            var fileSystem = new FileSystem();
            var consolePasswordReader = new ConsolePasswordReader(_userOutput, passwordRegistry);
            var certificateStore = new CertificateStore(fileSystem, consolePasswordReader);
            var certificate = certificateStore.ReadOrCreateCertificateFile("mycert.pfx");

            _simpleRpcClient = new SimpleRpcClient(_userOutput, passwordRegistry, certificate, logger);
        }

        public async Task Simulate(IEnumerable<IPeerIdentifier> simulationNodePeerIdentifiers)
        {
            var isConnectionSuccessful = await _simpleRpcClient
               .ConnectRetryAsync(simulationNodePeerIdentifiers.ElementAt(0)).ConfigureAwait(false);
            if (!isConnectionSuccessful)
            {
                _logger.Error("Could not connect to node");
                return;
            }

            _simpleRpcClient.ReceiveMessage<BroadcastRawTransactionResponse>(ReceiveTransactionResponse);

            await RunSimulation();
        }

        private async Task RunSimulation()
        {
            await Task.Run(async () =>
            {
                while (_simpleRpcClient.IsActive())
                {
                    _userOutput.WriteLine("Sending transaction");
                    var transaction = TransactionHelper.GenerateTransaction(_random.Next(100), _random.Next(2));
                    _simpleRpcClient.SendMessage(transaction);

                    await Task.Delay(100).ConfigureAwait(false);
                }
            });
        }

        public void ReceiveTransactionResponse(BroadcastRawTransactionResponse response)
        {
            _userOutput.WriteLine($"Transaction response: {response.ResponseCode}");
        }
    }
}
