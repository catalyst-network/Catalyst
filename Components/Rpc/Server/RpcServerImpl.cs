using Grpc.Core;
using System.Threading.Tasks;
using System;
using ADL.Rpc.Proto.Service;

namespace ADL.Rpc.Server
{
    public class RpcServerImpl : RpcService.RpcServiceBase
    {
        public override Task<PongResponse> Greeting(PingRequest request, ServerCallContext context)
        {
            Console.WriteLine($"Message: {request.Ping}");
            return Task.FromResult(new PongResponse());
        }
    }
}