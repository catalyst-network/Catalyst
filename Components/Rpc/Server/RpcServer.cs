using System;
using Grpc.Core;
using ADL.Rpc.Proto.Service;
using System.Threading;
using System.Threading.Tasks;

namespace ADL.Rpc.Server
{
    public class RpcServer : IRpcServer
    {
        internal protected CancellationTokenSource TokenSource { get; private set; }
        internal protected Task ServerTask { get; private set; }
        internal protected Grpc.Core.Server Server { get; private set; }
        internal protected IRpcSettings Settings { get; private set; }

        public void StartServer(IRpcSettings settings )
        {
            Settings = settings;
            Server = new Grpc.Core.Server
            {
                Services = { RpcService.BindService(new RpcServerImpl()) },
                Ports = { new ServerPort(Settings.BindAddress, Settings.Port, ServerCredentials.Insecure) }
            };
            TokenSource = new CancellationTokenSource();
            ServerTask = RunServiceAsync(Server, TokenSource.Token);
        }
        
        public void StopServer()
        {
            Console.WriteLine("Dispose started ");
            TokenSource.Cancel();
            try
            {
                ServerTask.Wait();
            }
            catch (AggregateException)
            {
                Console.WriteLine("RpcServer shutdown canceled");
            }
            Console.WriteLine("RpcServer shutdown");
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
            Console.WriteLine("Rpc Server started, listening on " + server.Ports);
            await AwaitCancellation(cancellationToken);
            await server.ShutdownAsync();
        }
    }
}
