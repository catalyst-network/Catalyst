using System;
using Grpc.Core;
using System.Threading.Tasks;
using System.Reflection;
using ADL.Protocol.Rpc.Node;
using Akka.Actor;
using ADL.Node;

namespace ADL.Node.Core.Modules.Rpc
{
    public class RpcServerImpl : RpcServer.RpcServerBase, IRpcServer
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override Task<PongResponse> Ping(PingRequest request, ServerCallContext context)
        {
            return Task.FromResult(new PongResponse
            {
                Pong = "pong"
            });
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override Task<VersionResponse> Version(VersionRequest request, ServerCallContext context)
        {
            return Task.FromResult(new VersionResponse
            {
                Version = Assembly.GetEntryAssembly().GetName().Version.ToString()
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override Task<GetMempoolResponse> GetMempool(GetMempoolRequest request, ServerCallContext context)
        {
            return AtlasSystem.TaskHandlerActor.Ask<GetMempoolResponse>(request);            
        }
    }
}
