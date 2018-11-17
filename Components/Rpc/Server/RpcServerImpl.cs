using Grpc.Core;
using System.Threading.Tasks;
using System;
using System.Reflection;
using ADL.Rpc.Proto.Service;

namespace ADL.Rpc.Server
{
    public class RpcServerImpl : RpcService.RpcServiceBase
    {
        public override Task<PongResponse> Ping(PingRequest request, ServerCallContext context)
        {
            return Task.FromResult(new PongResponse
            {
                Pong = "pong"
            });
        }
        
        public override Task<VersionResponse> Version(VersionRequest request, ServerCallContext context)
        {
            return Task.FromResult(new VersionResponse
            {
                Version =  Assembly.GetEntryAssembly().GetName().Version.ToString()
            });
        }
    }
}
