using System;
using Grpc.Core;
using ADL.Rpc.Proto.Service;
using System.Threading;
using System.Threading.Tasks;

namespace ADL.Rpc.Server
{
    public class RpcServer : IDisposable, IRpcServer
    {
        private const string Host = "0.0.0.0";
        private const int Port = 50051;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Task _serverTask;

        public RpcServer()
        {
            var server = new Grpc.Core.Server
            {
                Services = { RpcService.BindService(new RpcServerImpl()) },
                Ports = { new ServerPort(Host, Port, ServerCredentials.Insecure) }
            };

            CancellationTokenSource tokenSource = new CancellationTokenSource();
            _serverTask = RunServiceAsync(server, tokenSource.Token);

            Console.WriteLine("GreeterServer listening on port " + Port);
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
        
        // Enabling dispose shows an error when starting the rpc server,
        // the rpc server is still alive though.
        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            try
            {
                _serverTask.Wait();
            }
            catch (AggregateException)
            {
                Console.WriteLine("DisposableThread task canceled");
            }
            Console.WriteLine("DisposableThread disposed");
        }
    }
}
