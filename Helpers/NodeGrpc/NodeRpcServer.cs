using ADL.Rpc;
using Grpc.Core;
using System.Reflection;
using ADL.Protocol.Rpc.Node;
using System.Threading.Tasks;

namespace ADL.NodeGrpc
{
    public class NodeRpcServer : RpcServer.RpcServerBase, IRpcServer
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public Task<PongResponse> Ping(PingRequest request, ServerCallContext context)
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
    }
}
