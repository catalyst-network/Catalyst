using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using ADL.Cli.Interfaces;
using ADL.Cli.Shell;
using ADL.Consensus;
using ADL.DFS;
using ADL.Ledger;
using ADL.LocalPeer;
using Akka;
using Akka.Actor;
using Autofac;
using Akka.DI.Core;
using Akka.DI.AutoFac;
using ADL.RpcServer;
using ADL.TaskManager;
using Thinktecture.IO;
using Thinktecture.IO.Adapters;
using Microsoft.Extensions.Configuration;
using Thinktecture;

namespace ADL.Cli
{
    public class Program
    {
        private static ActorSystem _actorSystem;
        
        private static IKernel Kernel { get; set; }
                
        private static IShellBase Shell { get; set; }

        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledException;
            const int bufferSize = 1024 * 67 + 128;
            var inputStream = Console.OpenStandardInput(bufferSize);
            Console.SetIn(new StreamReader(inputStream, Console.InputEncoding, false, bufferSize));
            RegisterServices();
            Shell = new Koopa(Kernel);
            Shell.Run(args);
        }
        
        private static void RegisterServices()
        {
            Console.WriteLine("RegisterServices trace");

            var config = BuildConfiguration();
            
            using (_actorSystem = ActorSystem.Create("AtlasSystem"))
            {
                Console.WriteLine("AtlasSystem create trace");
                Kernel = BuildKernel(_actorSystem, config);
            }
        }

        private static IConfiguration BuildConfiguration()
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile(Directory.GetCurrentDirectory() + "/Configs/config.json", false, true)
                .Build();

            return config;
        }

        private static IKernel BuildKernel(ActorSystem actorSystem, IConfiguration config)
        {
            Console.WriteLine("BuildContainer trace");

            var builder = new ContainerBuilder();
            
            builder.RegisterType<ICliApplication>().AsImplementedInterfaces();

            builder.RegisterMicrosoftConfigurationProvider(config);
            builder.RegisterMicrosoftConfiguration<Settings>().As<INodeConfiguration>();
            
            builder.RegisterType<RpcServerService>().As<RpcServerService>();
            builder.RegisterType<TaskManagerService>().As<TaskManagerService>();
            builder.RegisterType<LocalPeerService>().As<LocalPeerService>();
            builder.RegisterType<LedgerService>().As<LedgerService>();
            builder.RegisterType<DFSService>().As<DFSService>();
            builder.RegisterType<ConsensusService>().As<ConsensusService>();

            var container = builder.Build();
            
            var resolver = new AutoFacDependencyResolver(container, actorSystem);
            
            var rpcActor = _actorSystem.ActorOf(resolver.Create<RpcServerService>(), "RpcServerService");
            var taskManagerActor = _actorSystem.ActorOf(resolver.Create<TaskManagerService>(), "TaskManagerService");
            var peerActor = _actorSystem.ActorOf(resolver.Create<LocalPeerService>(), "LocalPeerService");
            var ledgerActor = _actorSystem.ActorOf(resolver.Create<LedgerService>(), "LedgerService");
            var dfsActor = _actorSystem.ActorOf(resolver.Create<DFSService>(), "DFSService");
            var consensusActor = _actorSystem.ActorOf(resolver.Create<ConsensusService>(), "ConsensusService");
            
            return new Kernel(container, container.Resolve<INodeConfiguration>());
        }

        private static void PrintErrorLogs(TextWriter writer, Exception ex)
        {
            while (true)
            {
                writer.WriteLine(ex.GetType());
                writer.WriteLine(ex.Message);
                writer.WriteLine(ex.StackTrace);

                if (ex is AggregateException ex2)
                {
                    foreach (var inner in ex2.InnerExceptions)
                    {
                        writer.WriteLine();
                        PrintErrorLogs(writer, inner);
                    }
                }
                else if (ex.InnerException != null)
                {
                    writer.WriteLine();
                    ex = ex.InnerException;
                    continue;
                }
                break;
            }
        }

        private static void UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            using (var fs = new FileStream("error.log", FileMode.Create, FileAccess.Write, FileShare.None))

            using (var writer = new StreamWriter(fs))
                if (e.ExceptionObject is Exception ex)
                {
                    PrintErrorLogs(writer, ex);
                }
                else
                {
                    writer.WriteLine(e.ExceptionObject.GetType());
                    writer.WriteLine(e.ExceptionObject);
                }
        }
    }
}