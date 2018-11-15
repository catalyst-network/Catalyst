using System;
using Grpc.Core;
using GreeterProtos;
using System.Threading;
using System.Threading.Tasks;

namespace ADL.Rpc.Server
{
    class GreeterImpl : Greeter.GreeterBase
    {
        // Server side handler of the SayHello RPC
        public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        {
            return Task.FromResult(new HelloReply { Message = "Hello " + request.Name });
        }

        public override Task<HelloReply> SayHelloAgain(HelloRequest request, ServerCallContext context)
        {
            return Task.FromResult(new HelloReply { Message = "Hello again " + request.Name });
        }
    }

    public class Server : IRpcServerService
    {
        const string Host = "0.0.0.0";
        const int Port = 50051;

        public RpcServerService()
        {
            // Build a server
            var server = new Server
            {
                Services = {GreetingService.BindService(new GreeterServiceImpl())},
                Ports = {new ServerPort(Host, Port, ServerCredentials.Insecure)}
            };

            CancellationTokenSource tokenSource = new CancellationTokenSource();
            var serverTask = RunServiceAsync(server, tokenSource.Token);

            Console.WriteLine("GreeterServer listening on port " + Port);
            Console.WriteLine("Press any key to stop the server...");
            Console.ReadKey();

            tokenSource.Cancel();
            Console.WriteLine("Shutting down...");
            serverTask.Wait();
        }

        private static Task AwaitCancellation(CancellationToken token)
        {
            var taskSource = new TaskCompletionSource<bool>();
            token.Register(() => taskSource.SetResult(true));
            return taskSource.Task;
        }

        private static async Task RunServiceAsync(Server server,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            // Start server
            server.Start();

            await AwaitCancellation(cancellationToken);
            await server.ShutdownAsync();
        }
    }
}
