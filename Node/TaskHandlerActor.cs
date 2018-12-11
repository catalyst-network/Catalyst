using System;
using System.Collections.Generic;
using ADL.Protocol.Rpc.Node;
using Akka.Actor;
using Autofac.Features.ResolveAnything;

namespace ADL.Node
{
    /// <summary>
    /// Actor handling requests coming via RPC
    /// </summary>
    public class TaksHandlerActor : UntypedActor
    {           
        protected override void OnReceive(object message)
        {
            if (message is GetMempoolRequest)
            {
                var res = AtlasSystem.MempoolService.GetImpl().GetInfo();
                
                var response = new GetMempoolResponse();
                response.Info.Add(res);
                Sender.Tell(response);
            }
        }
    }
}