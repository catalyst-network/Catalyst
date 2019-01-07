using System;
using System.IO;
using Grpc.Core;
using System.Threading;
using System.Threading.Tasks;
using ADL.Node.Core.Helpers.Services;
using ADL.Protocol.Rpc.Node;

namespace ADL.Node.Core.Modules.Rpc
{
    public class RpcService : ServiceBase, IRpcService
    {
        private Server Server { get; set; }
        private Task ServerTask { get; set; }
        private IRpcSettings Settings { get; set; }
        private CancellationTokenSource TokenSource { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="settings"></param>
        public RpcService(IRpcSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            Settings = settings;
        }

        /// <summary>
        /// 
        /// </summary>
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
        /// Get current implementation of this service
        /// </summary>
        /// <returns></returns>
        public IRpcServer GetImpl()
        {
            return (IRpcServer) Server; // not great but grpc is partially sealed so we cant extend to assign it a iface
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private static Task AwaitCancellation(CancellationToken token = new CancellationToken())
        {
            var taskSource = new TaskCompletionSource<bool>();
            try
            {
                token.Register(() => taskSource.SetResult(true));
            }
            catch (ArgumentNullException e)
            {
                Log.LogException.Message("AwaitCancellation: Register", e);
                throw;
            }
            catch (InvalidOperationException e)
            {
                Log.LogException.Message("AwaitCancellation: SetResult", e);
                throw;
            }
            return taskSource.Task;
        }

        /// <summary>
        ///  Starts the RpcService
        /// </summary>
        /// <param name="server"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private static Task RunServiceAsync(Server server)
        {
            if (server == null) throw new ArgumentNullException(nameof(server));
            return RunServiceAsync(server, new CancellationToken());
        }

        /// <summary>
        ///  Starts the RpcService
        /// </summary>
        /// <param name="server"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private static async Task RunServiceAsync(Server server, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (server == null) throw new ArgumentNullException(nameof(server));
            try
            {
                server.Start();
                Log.Log.Message("Rpc Server started, listening on " + server.Ports);
            }
            catch (IOException e)
            {
                Log.LogException.Message("RunServiceAsync: Start", e);
                throw;
            }

            await AwaitCancellation(cancellationToken);
            await server.ShutdownAsync();
        }
    }
}
