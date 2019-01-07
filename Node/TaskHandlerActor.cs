using System;
using Akka.Actor;
using ADL.Protocol.Rpc.Node;

namespace ADL.Node
{
    /// <inheritdoc />
    /// <summary>
    /// Actor handling requests coming via RPC
    /// </summary>
    public class TaskHandlerActor : UntypedActor
    {           
        protected override void OnReceive(object message)
        {
            if (message == null) throw new ArgumentNullException(nameof (message));
            if (!(message is GetMempoolRequest)) return;
            var res = AtlasSystem.MempoolService.GetImpl().GetInfo();
            var response = new GetMempoolResponse();
            response.Info.Add(res);
            Sender.Tell(response);
        }
    }
}
