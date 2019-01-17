using System;
using Autofac;
using System.IO;
using Grpc.Core;
using System.Threading;
using Catalyst.Helpers.Util;
using System.Threading.Tasks;
using Catalyst.Helpers.Logger;
using Catalyst.Protocol.Rpc.Node;

namespace Catalyst.Node.Modules.Core.Rpc
{
    public class RpcModule : ModuleBase, IRpcModule
    {
        private Server Server { get; set; }
        public static Rpc Rpc;
        private Task ServerTask { get; set; }
        private IRpcSettings Settings { get; set; }
        private CancellationTokenSource TokenSource { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="rpcSettings"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public static ContainerBuilder Load(ContainerBuilder builder, IRpcSettings rpcSettings)
        {
            //@TODO guard util
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (rpcSettings == null) throw new ArgumentNullException(nameof(rpcSettings));
            builder.Register(c => new RpcModule(rpcSettings))
                .As<IRpcModule>()
                .InstancePerLifetimeScope();

            return builder;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="settings"></param>
        public RpcModule(IRpcSettings settings)
        {
            Guard.NotNull(settings, nameof(settings));
            Rpc = Rpc.GetInstance();
            Settings = settings;
        }

        /// <summary>
        /// 
        /// </summary>
        public override bool StartService()
        {
            Server = new Server
            {
                Services =  { RpcServer.BindService(Rpc) },
                Ports = { new ServerPort(Settings.BindAddress.ToString(), Settings.Port, ServerCredentials.Insecure) }
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
                LogException.Message("AwaitCancellation: Register", e);
                throw;
            }
            catch (InvalidOperationException e)
            {
                LogException.Message("AwaitCancellation: SetResult", e);
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
            Guard.NotNull(server, nameof(server));
            return RunServiceAsync(server, new CancellationToken());
        }

        /// <summary>
        ///  Starts the RpcService
        /// </summary>
        /// <param name="server"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private static async Task RunServiceAsync(Server server, CancellationToken cancellationToken)
        {
            Guard.NotNull(server, nameof(server));
            try
            {
                server.Start();
                Log.Message("Rpc Server started, listening on " + server.Ports);
            }
            catch (IOException e)
            {
                LogException.Message("RunServiceAsync: Start", e);
                throw;
            }

            await AwaitCancellation(cancellationToken);
            await server.ShutdownAsync();
        }
    }
}
