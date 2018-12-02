﻿using System;
using Grpc.Core;
using System.Threading;
using System.Threading.Tasks;
using ADL.Node.Core.Helpers.Services;
 
 namespace ADL.Node.Core.Modules.Rpc
{
    public class RpcService : AsyncServiceBase, IRpcService
    {
        private Task ServerTask { get; set; }
        private IRpcServer RpcServer { get; set; }
        private IRpcSettings Settings { get; set; }
        private CancellationTokenSource TokenSource { get; set; }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="settings"></param>
        public RpcService(IRpcServer rpcServer, IRpcSettings settings)
        {
            RpcServer = rpcServer;
            Settings = settings;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="settings"></param>
        public bool StartService()
        {
            RpcServer.CreateServer(Settings.BindAddress, Settings.Port);
            TokenSource = new CancellationTokenSource();
            ServerTask = RunServiceAsync(RpcServer.Server, TokenSource.Token);
            return true;
        }
        
        /// <summary>
        /// 
        /// </summary>
        public bool StopService()
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
            return false;
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
            await AwaitCancellation(default(CancellationToken));
            await server.ShutdownAsync();
        }
    }
}
