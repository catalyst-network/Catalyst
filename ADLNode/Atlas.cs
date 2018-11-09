using System;
using ADL.RpcServer;
using Akka.Actor;
using Akka.DI.Core;

namespace ADL.ADLNode
{
    public class Atlas : ReceiveActor
    { 
        public Atlas(IRpcServerService rpcServerService) {
            Console.WriteLine("Parent Actor created!");
 
/*
            var childActorProps = Context.DI().Props<ChildActor>();
*/
            /*var childActor = Context.ActorOf(childActorProps, "ChildActor");*/
        } 
    }
}