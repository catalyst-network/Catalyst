using System;
using System.Reflection;
using System.Threading.Tasks;
using Catalyst.Node.Modules.Core.Rpc.Events;
using Catalyst.Protocol.Rpc.Node;
using Grpc.Core;

namespace Catalyst.Node.Modules.Core.Rpc
{
    public class Rpc : RpcServer.RpcServerBase, IRpcServer
    {
        private static readonly object Mutex = new object();
        private static Rpc Instance { get; set; }

        /// <summary>
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override Task<PongResponse> Ping(PingRequest request, ServerCallContext context)
        {
            //@TODO guard util
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (context == null) throw new ArgumentNullException(nameof(context));
            return Task.FromResult(new PongResponse {Pong = true});
        }

        /// <summary>
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override Task<VersionResponse> Version(VersionRequest request, ServerCallContext context)
        {
            //@TODO guard util
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (context == null) throw new ArgumentNullException(nameof(context));
            return Task.FromResult(new VersionResponse
            {
                Version = Assembly.GetEntryAssembly().GetName().Version.ToString()
            });
        }

        public static Rpc GetInstance()
        {
            if (Instance == null)
                lock (Mutex)
                {
                    if (Instance == null) Instance = new Rpc();
                }

            return Instance;
        }

        /// <summary>
        /// </summary>
        public event EventHandler<GetMempoolEventArgs> GetMempoolCall;

//
//        /// <summary>
//        /// 
//        /// </summary>
//        /// <param name="request"></param>
//        /// <param name="context"></param>
//        /// <returns></returns>
//        public override Task<GetMempoolResponse> GetMempool(GetMempoolRequest request, ServerCallContext context)
//        {
//            //@TODO guard util
//            if (request == null) throw new ArgumentNullException(nameof(request));
//            if (context == null) throw new ArgumentNullException(nameof(context));
//            return CatalystSystem.TaskHandlerActor.Ask<GetMempoolResponse>(request);
//        }
    }
}