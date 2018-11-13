using System;
using System.IO;
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

namespace ADL.Cli
{
    public class Program
    {
        private const string _CONFIG_FILE_PATH = "Configuration.json";

        private static ActorSystem _actorSystem;

        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledException;
            var bufferSize = 1024 * 67 + 128;
            Stream inputStream = Console.OpenStandardInput(bufferSize);
            Console.SetIn(new StreamReader(inputStream, Console.InputEncoding, false, bufferSize));
            RegisterServices();
            var mainService = new MainService();
            mainService.Run(args);
        }
        
        private static void RegisterServices()
        {
            Console.WriteLine("RegisterServices trace");

            using (_actorSystem = ActorSystem.Create("AtlasSystem"))
            {
                Console.WriteLine("AtlasSystem create trace");

                var container = BuildContainer(_actorSystem);

                var rpcActor = _actorSystem.ActorOf(container.Create<RpcServerService>(), "RpcServerService");
                var taskManagerActor = _actorSystem.ActorOf(container.Create<TaskManagerService>(), "TaskManagerService");
                var peerActor = _actorSystem.ActorOf(container.Create<LocalPeerService>(), "LocalPeerService");
                var ledgerActor = _actorSystem.ActorOf(container.Create<LedgerService>(), "LedgerService");
                var dfsActor = _actorSystem.ActorOf(container.Create<DFSService>(), "DFSService");
                var consensusActor = _actorSystem.ActorOf(container.Create<ConsensusService>(), "ConsensusService");
            }
        }

        private static IDependencyResolver BuildContainer(ActorSystem actorSystem)
        {
            Console.WriteLine("BuildContainer trace");

            var builder = new ContainerBuilder();
            
            builder.RegisterType<RpcServerService>().As<RpcServerService>();
            builder.RegisterType<TaskManagerService>().As<TaskManagerService>();
            builder.RegisterType<LocalPeerService>().As<LocalPeerService>();
            builder.RegisterType<LedgerService>().As<LedgerService>();
            builder.RegisterType<DFSService>().As<DFSService>();
            builder.RegisterType<ConsensusService>().As<ConsensusService>();

            IContainer container = builder.Build();
            return new AutoFacDependencyResolver(container, actorSystem);
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