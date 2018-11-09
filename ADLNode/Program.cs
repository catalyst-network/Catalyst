using System;
using Akka;
using Akka.Actor;
using Akka.Remote;
using Microsoft.Extensions.CommandLineUtils;
using ADL.Helpers;
using ADL.RpcServer;
using Akka.DI.Ninject;
using Ninject;
using Akka.DI.Core;
using Autofac;
using Akka.DI.AutoFac;
    
namespace ADL.ADLNode
{   
    class Program
    {
        public static void Main(string[] args)
        {
            var app = new Microsoft.Extensions.CommandLineUtils.CommandLineApplication();

            
            app.Command("", config =>
            {
                // All the options that we support on the command line
                var daemonOption = app.Option("-d|--daemon", "Run as daemon", CommandOptionType.NoValue);
                var sysopOption  = app.Option("-s|--sysop", "Run daemon with sysop shell", CommandOptionType.NoValue);
                var portOption   = app.Option("-p|--port", "Specify a listening port", CommandOptionType.SingleValue);
                var edgesOption  = app.Option("-e|--edges", "Specify a maximum number of connections", CommandOptionType.SingleValue);
                var homeOption   = app.Option("-m|--home", "Specify a home directory", CommandOptionType.SingleValue);
                var hostOption   = app.Option("-t|--host", "Run daemon with sysop shell", CommandOptionType.SingleValue);

                app.OnExecute(() =>
                {
                    if (daemonOption.HasValue())
                    {
                        Console.WriteLine($"Running Daemon");
                        Env.Daemon = true;
                    }
                    else
                    {
                        Env.Daemon = false;
                    }

                    if (sysopOption.HasValue())
                    {
                        Console.WriteLine($"Running daemon with sysop shell");
                        Env.Shell = true;
                    }
                    else
                    {
                        Env.Shell = false;
                    }

                    if (portOption.HasValue())
                    {
                        Env.Port = uint.Parse(portOption.Value());
                        Console.WriteLine($"Listening to port: {portOption.Value()}");
                    }

                    if (edgesOption.HasValue())
                    {
                        Env.Edges = uint.Parse(edgesOption.Value());
                        Console.WriteLine($"Max number of edged: {edgesOption.Value()}");
                    }

                    if (homeOption.HasValue())
                    {
                        Env.HomeDir = homeOption.Value();
                        Console.WriteLine($"Home directory: {homeOption.Value()}");
                    }

                    if (hostOption.HasValue())
                    {
                        Env.Host = hostOption.Value();
                        Console.WriteLine($"Adress of daemon host: {hostOption.Value()}");
                    }
                    
                    return 0;
                });
            });

            using (var actorSystem = ActorSystem.Create("HeroActorSystem"))
            {
                var resolver = InjectDependencies(actorSystem);
                // Get an instance of our HeroActor, having Akka.DI.Autofac take care of the Props
                var rpcActor = actorSystem.ActorOf(resolver.Create<RpcServerService>(), "RpcServerService");
                Console.WriteLine("kek");
                rpcActor.Tell("im a message");
                Console.ReadLine();
            }
            
            //give people help with --help
            app.HelpOption("-? | -h | --help");

            try
            {
                var result = app.Execute(args);
                Environment.Exit(result);
            }
            catch (Exception error)
            {
                Console.WriteLine(error);
            }
        }
        
        private static IDependencyResolver InjectDependencies(ActorSystem actorSystem)
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<RpcServerService>();

            IContainer container = builder.Build();

            return new AutoFacDependencyResolver(container, actorSystem);
        }
    }
}
