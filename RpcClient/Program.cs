using System;
using System.Collections.Generic;
using Grpc.Core;
using ADL.Rpc.Proto.Service;
using System.Threading.Tasks;
using ADL.Rpc.Proto.Service;

namespace ADL.Rpc.Client
{
    class Program
    {
        private const string DefaultHost = "localhost";
        private const int Port = 50051;

        public static void Main(string[] args)
        {
            RunAsync(args).Wait();
        }

        private static async Task RunAsync(IReadOnlyList<string> args)
        {
            var host = args.Count == 1 ? args[0] : DefaultHost;
            var channelTarget = $"{host}:{Port}";

            Console.WriteLine($"Target: {channelTarget}");

            // Create a channel
            var channel = new Channel(channelTarget, ChannelCredentials.Insecure);

            try
            {
                // Create a client with the channel
                var client = new RpcService.RpcServiceClient(channel);

                // Create a request
//                var request = new PingRequest{ Ping = "ping" };
                var request = new VersionRequest{ Query = true };

                // Send the request
                Console.WriteLine("GreeterClient sending request");
//                var response = await client.PingAsync(request);
                var response = await client.VersionAsync(request);
                
                Console.WriteLine("GreeterClient received response: " + response);
            }
            finally
            {
                // Shutdown
                await channel.ShutdownAsync();
            }
        }
    }
}