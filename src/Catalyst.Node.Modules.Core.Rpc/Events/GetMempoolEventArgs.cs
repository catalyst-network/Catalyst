using System;
using Akka.Actor;
using Catalyst.Protocol.Rpc.Node;

namespace Catalyst.Node.Modules.Core.Rpc.Events
{
    public class GetMempoolEventArgs : UntypedActor
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <exception cref="ArgumentNullException"></exception>
        protected override void OnReceive(object message)
        {
            //@TODO guard util
            if (message == null) throw new ArgumentNullException(nameof (message));
            if (!(message is GetMempoolRequest)) return;
//            var res = CatalystNode.MempoolModule.GetImpl().GetInfo();
//            var response = new GetMempoolResponse();
//            response.Info.Add(res);
//            Sender.Tell(response);
        }
    }
}