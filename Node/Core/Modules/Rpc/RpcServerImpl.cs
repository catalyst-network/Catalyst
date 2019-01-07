using System;
using Grpc.Core;
using Akka.Actor;
using System.Reflection;
using ADL.Protocol.Rpc.Node;
using System.Threading.Tasks;

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
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (context == null) throw new ArgumentNullException(nameof(context));
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
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (context == null) throw new ArgumentNullException(nameof(context));
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
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (context == null) throw new ArgumentNullException(nameof(context));
            return AtlasSystem.TaskHandlerActor.Ask<GetMempoolResponse>(request);
        }
    }
}
