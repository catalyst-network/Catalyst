using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Catalyst.Common.Interfaces.Cli;
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

        public async Task<bool> ConnectAsync(ClientRpcInfo clientRpcInfo)
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
            var isConnectionSuccessful = await ConnectToAllPeerIdentifiersAsync(clientRpcInfoList);
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
