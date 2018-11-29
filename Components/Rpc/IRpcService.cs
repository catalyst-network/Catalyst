using Grpc.Core;
using ADL.Rpc.Proto.Server;
using System.Threading.Tasks;

namespace ADL.Rpc
{
    public interface IRpcServer
    {
        Task<PongResponse> Ping(PingRequest request, ServerCallContext context);

        Task<VersionResponse> Version(VersionRequest request, ServerCallContext context);
        
//        Task<GetInfoRequest> GetInfo();
    }
}
