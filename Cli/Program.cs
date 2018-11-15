using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using ADL.Cli.Interfaces;
using ADL.Cli.Shell;
using ADL.Consensus;
using ADL.DFS;
using ADL.Ledger;
using ADL.LocalPeer;
using Akka;
using Akka.Actor;
using Autofac;
using Autofac.Configuration;
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

        /// <summary>
        /// Main cli loop
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledException;
            const int bufferSize = 1024 * 67 + 128;
            var inputStream = Console.OpenStandardInput(bufferSize);
            Console.SetIn(new StreamReader(inputStream, Console.InputEncoding, false, bufferSize));
            CreateAkkaSupervisor();
            Shell = new Koopa(Kernel);
            Shell.Run(args);
        }
        
        /// <summary>
        /// Creates main Akka supervisor and passes it to the kernel.
        /// </summary>
        private static void CreateAkkaSupervisor()
        {
            Console.WriteLine("RegisterServices trace");
           
            using (_actorSystem = ActorSystem.Create("AtlasSystem"))
            {
                Console.WriteLine("AtlasSystem create trace");
                Kernel = BuildKernel(_actorSystem, Settings.Default);
            }
        }

        /// <summary>
        /// Registers all services on IOC containers and returns an application kernel.
        /// </summary>
        /// <param name="actorSystem"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        private static IKernel BuildKernel(ActorSystem actorSystem, INodeConfiguration settings)
        {
            Console.WriteLine("BuildContainer trace");

            AssemblyLoadContext.Default.Resolving += (AssemblyLoadContext context, AssemblyName assembly) =>
            {
                // DISCLAIMER: NO PROMISES THIS IS SECURE. You may or may not want this strategy. It's up to
                // you to determine if allowing any assembly in the directory to be loaded is acceptable. This
                // is for demo purposes only.
                Console.WriteLine(assembly.Name);
                return context.LoadFromAssemblyPath(Path.Combine(Directory.GetCurrentDirectory(), $"{assembly.Name}.dll"));
            };
            
            var config = new ConfigurationBuilder()
                .AddJsonFile(Directory.GetCurrentDirectory()+"/Configs/services.json")
                .Build();
            var configModule = new ConfigurationModule(config);
            
            var builder = new ContainerBuilder();

            builder.RegisterModule(configModule);

            builder.RegisterType<ICliApplication>().AsImplementedInterfaces();

//            builder.RegisterMicrosoftConfigurationProvider(Settings);
//            builder.RegisterMicrosoftConfigurationProvider<Settings>().As<INodeConfiguration>();
            
//            builder.RegisterType<RpcServerService>().As<RpcServerService>();
//            builder.RegisterType<TaskManagerService>().As<TaskManagerService>();
//            builder.RegisterType<LocalPeerService>().As<LocalPeerService>();
//            builder.RegisterType<LedgerService>().As<LedgerService>();
//            builder.RegisterType<DFSService>().As<DFSService>();
//            builder.RegisterType<ConsensusService>().As<ConsensusService>().InstancePerLifetimeScope();

            var container = builder.Build();
            
            using (var scope = container.BeginLifetimeScope())
            {
                //            builder.RegisterType<RpcServerService>().As<RpcServerService>();
                //            builder.RegisterType<TaskManagerService>().As<TaskManagerService>();
                //            builder.RegisterType<LocalPeerService>().As<LocalPeerService>();
                //            builder.RegisterType<LedgerService>().As<LedgerService>();
                //            builder.RegisterType<DFSService>().As<DFSService>();
                //            builder.RegisterType<ConsensusService>().As<ConsensusService>().InstancePerLifetimeScope();
//                var plugin = scope.Resolve<IDFSService>();
                Console.WriteLine("Resolved specific plugin type: {0}");

                Console.WriteLine("All available plugins:");
//                var allPlugins = scope.Resolve<IEnumerable<IDFSService>>();
//                foreach (var resolved in allPlugins)
//                {
//                    Console.WriteLine("- {0}");
//                }
            }
            
            var resolver = new AutoFacDependencyResolver(container, actorSystem);
            resolver.Create(new DFSService());
//            var rpcActor = _actorSystem.ActorOf(resolver.Create<RpcServerService>(), "RpcServerService");
//            var taskManagerActor = _actorSystem.ActorOf(resolver.Create<TaskManagerService>(), "TaskManagerService");
//            var peerActor = _actorSystem.ActorOf(resolver.Create<LocalPeerService>(), "LocalPeerService");
//            var ledgerActor = _actorSystem.ActorOf(resolver.Create<LedgerService>(), "LedgerService");
//            var dfsActor = _actorSystem.ActorOf(resolver.Create<DFSService>(), "DFSService");
//            var consensusActor = _actorSystem.ActorOf(resolver.Create<ConsensusService>(), "ConsensusService");
            
            return new Kernel(container, settings);
        }

        /// <summary>
        /// Prints application errors
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="ex"></param>
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

        /// <summary>
        /// Catches unhandled exceptions and writes them to an error file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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