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
        private static ActorSystem ActorSystem;
        private static IContainer Kernel { get; set; }
        private static INodeConfiguration  NodeConfiguration { get; set; }
        private static IShellBase _shelly { get; set; }

        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledException;
            var bufferSize = 1024 * 67 + 128;
            Stream inputStream = Console.OpenStandardInput(bufferSize);
            Console.SetIn(new StreamReader(inputStream, Console.InputEncoding, false, bufferSize));
            RegisterServices();
            _shelly = new Shelly(Kernel);
            _shelly.Run(args);
        }
        
        private static void RegisterServices()
        {
            Console.WriteLine("RegisterServices trace");

            using (ActorSystem = ActorSystem.Create("AtlasSystem"))
            {
                Console.WriteLine("AtlasSystem create trace");

                IConfiguration config = BuildConfiguration();
                var kernel = BuildKernel(ActorSystem, config);

                var rpcActor = ActorSystem.ActorOf(kernel.Create<RpcServerService>(), "RpcServerService");
//                IActorRef taskManagerActor = ActorSystem.ActorOf(kernel.Create<TaskManagerService>(), "TaskManagerService");
//                IActorRef peerActor = ActorSystem.ActorOf(kernel.Create<LocalPeerService>(), "LocalPeerService");
//                IActorRef ledgerActor = ActorSystem.ActorOf(kernel.Create<LedgerService>(), "LedgerService");
//                IActorRef dfsActor = ActorSystem.ActorOf(kernel.Create<DFSService>(), "DFSService");
//                IActorRef consensusActor = ActorSystem.ActorOf(kernel.Create<ConsensusService>(), "ConsensusService");
            }
        }

        private static IConfiguration BuildConfiguration()
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile(Directory.GetCurrentDirectory() + "/Configs/config.json", false, true)
                .Build();

            return config;
        }

        private static IDependencyResolver BuildKernel(ActorSystem actorSystem, IConfiguration config)
        {
            Console.WriteLine("BuildContainer trace");

            var builder = new ContainerBuilder();
            
            builder.RegisterType<ICliApplication>().AsImplementedInterfaces();

            builder.RegisterMicrosoftConfigurationProvider(config);
            builder.RegisterMicrosoftConfiguration<Settings>().As<INodeConfiguration>();
            
            builder.RegisterType<RpcServerService>().As<IRpcServerService>();
            builder.RegisterType<TaskManagerService>().As<ITaskManagerService>();
            builder.RegisterType<LocalPeerService>().As<ILocalPeerService>();
            builder.RegisterType<LedgerService>().As<ILedgerService>();
            builder.RegisterType<DFSService>().As<IDFSService>();
            builder.RegisterType<ConsensusService>().As<IConsensusService>();

            Kernel = builder.Build();
            NodeConfiguration = Kernel.Resolve<INodeConfiguration>();

            return new AutoFacDependencyResolver(Kernel, actorSystem);
        }

        private static void PrintErrorLogs(StreamWriter writer, Exception ex)
        {
            writer.WriteLine(ex.GetType());
            writer.WriteLine(ex.Message);
            writer.WriteLine(ex.StackTrace);
            
            if (ex is AggregateException ex2)
            {
                foreach (Exception inner in ex2.InnerExceptions)
                {
                    writer.WriteLine();
                    PrintErrorLogs(writer, inner);
                }
            }
            else if (ex.InnerException != null)
            {
                writer.WriteLine();
                PrintErrorLogs(writer, ex.InnerException);
            }
        }
        
        private static void UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            using (FileStream fs = new FileStream("error.log", FileMode.Create, FileAccess.Write, FileShare.None))

            using (StreamWriter w = new StreamWriter(fs))
                if (e.ExceptionObject is Exception ex)
                {
                    PrintErrorLogs(w, ex);
                }
                else
                {
                    w.WriteLine(e.ExceptionObject.GetType());
                    w.WriteLine(e.ExceptionObject);
                }
        }
    }
}