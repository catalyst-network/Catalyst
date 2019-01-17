using System.Threading.Tasks;
using Catalyst.Protocol.Rpc.Node;
using Grpc.Core;

namespace Catalyst.Node.Modules.Core.Rpc
{
    public interface IRpcServer
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        Task<PongResponse> Ping(PingRequest request, ServerCallContext context);
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        Task<VersionResponse> Version(VersionRequest request, ServerCallContext context);
    }
}
