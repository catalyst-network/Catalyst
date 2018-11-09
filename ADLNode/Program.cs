using System;
using Akka;
using Akka.Actor;
using Akka.Remote;
using Microsoft.Extensions.CommandLineUtils;
using ADL.Helpers;
using ADL.RpcServer;
using Akka.DI.Core;
using Autofac;
using Akka.DI.AutoFac;
    
namespace ADL.ADLNode
{   
    class Program
    {
        public static void Main(string[] args)
        {
            using (var actorSystem = ActorSystem.Create("HeroActorSystem"))
            {
                var container = BuildContainer(actorSystem);
                // Get an instance of our HeroActor, having Akka.DI.Autofac take care of the Props
                var rpcActor = actorSystem.ActorOf(container.Create<RpcServerService>(), "RpcServerService");
                Console.WriteLine("kek");
                rpcActor.Tell("im a message");
                Console.ReadLine();
            }
        }
        
        private static IDependencyResolver BuildContainer(ActorSystem actorSystem)
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<RpcServerService>();
            IContainer container = builder.Build();
            return new AutoFacDependencyResolver(container, actorSystem);
        }
    }
}
