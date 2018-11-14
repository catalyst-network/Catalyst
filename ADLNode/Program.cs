using System;
using System.Threading;
using Microsoft.Extensions.CommandLineUtils;
using ADL.Helpers;
using ADL.ActorManager;
using ADL.DFS;
using Akka.Actor;

namespace ADL.ADLNode
{
    class Program
    {
        static void Main(string[] args)
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
                var hostOption   = app.Option("-t|--host", "daemon host", CommandOptionType.SingleValue);

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
                        Env.Sysop = true;
                    }
                    else
                    {
                        Env.Sysop = false;
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
                        Console.WriteLine($"Address of daemon host: {hostOption.Value()}");
                    }
                    
                    ActorModel.StartActorSystem();
                    
                    ActorModel.RpcServerActorRef.Tell("test-rpcserver");
                    //ActorModel.DfsActorRef.Tell(new DfsActor.AddFile("path")); 
                    //ActorModel.DfsActorRef.Tell(new DfsActor.ReadFile("hash"));
                    
                    //var task = ActorModel.DfsActorRef.Ask<string>(new DfsActor.ReadFile("hash"));
                    //task.Wait();
                    //Thread.Sleep(10000);
                    var task2 = ActorModel.DfsActorRef.Ask<string>(new DfsActor.AddFile("/home/fioravante/workspace/adlnode/ADLNode/bin/Debug/netcoreapp2.1/test.txt"));
                    Console.WriteLine(task2.Result);
                                
                    Thread.Sleep(10000);
                    ActorModel.DfsActorRef.GracefulStop(TimeSpan.FromSeconds(5));
                    ActorModel.RpcServerActorRef.GracefulStop(TimeSpan.FromSeconds(5));
                    
                    return 0;
                });
            });

            //give people help with --help
            app.HelpOption("-? | -h | --help");

            try
            {
                var result = app.Execute(args);
                Environment.Exit(result);
            }
            catch (Exception)
            {
            }
        }
    }
}
