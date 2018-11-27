using System;
using Grpc.Core;
using System.Threading;
using ADL.Rpc.Proto.Server;
using System.Threading.Tasks;

namespace ADL.Rpc
{
    public class RpcService : IRpcService
    {
        private CancellationTokenSource TokenSource { get; set; }
        private Task ServerTask { get; set; }
        private Server Server { get; set; }
        private IRpcSettings Settings { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="settings"></param>
        public void StartServer(IRpcSettings settings )
        {
            Settings = settings;
            Server = new Server
            {
                Services = { RpcServer.BindService(new RpcServerImpl()) },
                Ports = { new ServerPort(Settings.BindAddress, Settings.Port, ServerCredentials.Insecure) }
            };
            TokenSource = new CancellationTokenSource();
            ServerTask = RunServiceAsync(Server, TokenSource.Token);
        }
        
        /// <summary>
        /// 
        /// </summary>
        public void StopServer()
        {
            Console.WriteLine("Dispose started ");
            AwaitCancellation(TokenSource.Token);
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private static Task AwaitCancellation(CancellationToken token)
        {
            var taskSource = new TaskCompletionSource<bool>();
            token.Register(() => taskSource.SetResult(true));
            return taskSource.Task;
        }

        /// <summary>
        ///  Starts the RpcService
        /// </summary>
        /// <param name="server"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private static async Task RunServiceAsync(Server server,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            server.Start();
            Console.WriteLine("Rpc Server started, listening on " + server.Ports.ToString());
            await AwaitCancellation(cancellationToken);
            await server.ShutdownAsync();
        }
    }
}
