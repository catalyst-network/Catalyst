using System;
using Akka.Actor;

namespace ADL.RpcServer
{
    public class RpcServerService : IRpcServerService
    {
        public RpcServerService()
        {
            Console.WriteLine("RCP server starting....");
        }
    }
}
