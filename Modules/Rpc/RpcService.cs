﻿using System;
using Grpc.Core;
using System.Threading;
using System.Threading.Tasks;
using ADL.Services;
using ADL.NodeGrpc;
 
 namespace ADL.Rpc
{
    public class RpcService : AsyncServiceBase, IRpcService
    {
        private Server Server { get; set; }
        private Task ServerTask { get; set; }
        private IRpcServer RpcServer { get; set; }
        private IRpcSettings Settings { get; set; }
        private CancellationTokenSource TokenSource { get; set; }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="settings"></param>
        public RpcService(IRpcSettings settings, IRpcServer rpcServer)
        {
            Settings = settings;
            RpcServer = rpcServer;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="settings"></param>
        public bool StartService()
        {
            Server = new Server
            {
//                Services = { RpcServer.BindService(new NodeRpcServer()) },
                Ports = { new ServerPort(Settings.BindAddress, Settings.Port, ServerCredentials.Insecure) }
            };
            TokenSource = new CancellationTokenSource();
            ServerTask = RunServiceAsync(Server, TokenSource.Token);
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
        /// 
        /// </summary>
        /// <returns></returns>
        public bool RestartService()
        {
            if (StopService())
            {
                if (StartService())
                {
                    Console.WriteLine("RPC service restarted successfully");
                }
                else
                {
                    Console.WriteLine("Couldn't start rpc service");
                    return false;
                }
            }
            else
            {
                Console.WriteLine("Couldn't stop rpc service");
                return false;
            }
            return true;
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
