using System;
using Grpc.Core;
using ADL.Rpc.Proto.Service;
using System.Threading;
using System.Threading.Tasks;

namespace ADL.Rpc.Server
{
    public class RpcServer
    {
        const string Host = "0.0.0.0";
        const int Port = 50051;

        public RpcServer()
        {
            var server = new Grpc.Core.Server
            {
                Services = { RpcService.BindService(new RpcServerImpl()) },
                Ports = { new ServerPort(Host, Port, ServerCredentials.Insecure) }
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

        private static async Task RunServiceAsync(Grpc.Core.Server server,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            server.Start();
            await AwaitCancellation(cancellationToken);
            await server.ShutdownAsync();
        }
    }
}
