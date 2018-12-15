using System;
using Grpc.Core;
using System.Threading;
using System.Threading.Tasks;
using ADL.Node.Core.Helpers.Services;
using ADL.Protocol.Rpc.Node;
using ADL.Protocol.Rpc.Node.dist.csharp;

namespace ADL.Node.Core.Modules.Rpc
{
    public class RpcService : ServiceBase, IRpcService
    {
        private CancellationTokenSource TokenSource { get; set; }
        private Task ServerTask { get; set; }
        private Server Server { get; set; }
        private IRpcSettings Settings { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="settings"></param>
        public RpcService(IRpcSettings settings)
        {
            Settings = settings;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="settings"></param>
        public override bool StartService()
        {
            Server = new Server
            {
                Services = { RpcServer.BindService(new RpcServerImpl()) },
                Ports = { new ServerPort(Settings.BindAddress, Settings.Port, ServerCredentials.Insecure) }
            };
            TokenSource = new CancellationTokenSource();
            ServerTask = RunServiceAsync(Server, TokenSource.Token);
            return true;
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
        private async Task RunServiceAsync(Server server,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            server.Start();
            Console.WriteLine("Rpc Server started, listening on " + server.Ports.ToString());
            await AwaitCancellation(cancellationToken);
            await server.ShutdownAsync();
        }
    }
}
