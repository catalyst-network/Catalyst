using Grpc.Core;
using System.Threading.Tasks;
using System;
using System.Reflection;
using ADL.Rpc.Proto.Server;

namespace ADL.Rpc
{
    public class GRpcServer : RpcServer.RpcServerBase, IRpcServer
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
                Version =  Assembly.GetEntryAssembly().GetName().Version.ToString()
            });
        }
    }
}
