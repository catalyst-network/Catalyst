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
using Catalyst.Abstractions.Cli;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.Simulator.Helpers;
using Catalyst.Simulator.Interfaces;

namespace Catalyst.Simulator.Simulations
{
    public class TransactionSimulation : ISimulation
    {
        private readonly Random _random;
        private readonly IUserOutput _userOutput;

        public TransactionSimulation(IUserOutput userOutput)
        {
            _random = new Random();
            _userOutput = userOutput;
        }

        private async Task<bool> ConnectAsync(ClientRpcInfo clientRpcInfo)
        {
            var isConnectionSuccessful = await clientRpcInfo.RpcClient
               .ConnectRetryAsync(clientRpcInfo.PeerIdentifier).ConfigureAwait(false);
            if (!isConnectionSuccessful)
            {
                _userOutput.WriteLine($"Could not connect to node: {clientRpcInfo.PeerIdentifier.Ip}:{clientRpcInfo.PeerIdentifier.Port}");
                return false;
            }

            clientRpcInfo.RpcClient.ReceiveMessage<BroadcastRawTransactionResponse>(ReceiveTransactionResponse);
            return true;
        }

        private async Task<bool> ConnectToAllPeerIdentifiersAsync(IEnumerable<ClientRpcInfo> clientRpcInfoList)
        {
            foreach (var clientRpcInfo in clientRpcInfoList)
            {
                var isConnectionSuccessful = await ConnectAsync(clientRpcInfo).ConfigureAwait(false);
                if (!isConnectionSuccessful)
                {
                    _userOutput.WriteLine("Could not connect to a node, aborting simulation.");
                    return false;
                }
            }

            return true;
        }

        public async Task SimulateAsync(IList<ClientRpcInfo> clientRpcInfoList)
        {
            var isConnectionSuccessful = await ConnectToAllPeerIdentifiersAsync(clientRpcInfoList).ConfigureAwait(false);
            if (!isConnectionSuccessful)
            {
                return;
            }

            while (clientRpcInfoList.All(x => x.RpcClient.IsConnected()))
            {
                foreach (var clientRpcInfo in clientRpcInfoList)
                {
                    _userOutput.WriteLine("Sending transaction");
                    var transaction = TransactionHelper.GenerateTransaction(_random.Next(100), _random.Next(2));
                    clientRpcInfo.RpcClient.SendMessage(transaction);
                }

                var randomDelay = _random.Next(10, 200);
                await Task.Delay(randomDelay).ConfigureAwait(false);
            }
        }

        public void ReceiveTransactionResponse(BroadcastRawTransactionResponse response)
        {
            _userOutput.WriteLine($"Transaction response: {response.ResponseCode}");
        }
    }
}
