using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using Autofac;
using Autofac.Configuration;
using Autofac.Extensions.DependencyInjection;
using AutofacSerilogIntegration;
using Catalyst.Node.Common.Modules.Gossip;
using Catalyst.Node.Common.P2P;
using Catalyst.Node.Core.Config;
using Catalyst.Node.Core.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SharpRepository.Ioc.Autofac;
using SharpRepository.Repository;

namespace Catalyst.Node.Core
{
    public static class Program
    {
        private static readonly ILogger Logger;
        private static readonly string LifetimeTag;
        private static readonly string ExecutionDirectory;

        static Program()
        {
            var declaringType = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType;
            Logger = Log.Logger.ForContext(declaringType);
            LifetimeTag = declaringType.AssemblyQualifiedName;
            ExecutionDirectory = Path.GetDirectoryName(declaringType.Assembly.Location);
        }

        public static int Main(string[] args)
        {
            try
            {
                //Enable after checking safety implications, if plugins become important.
                //AssemblyLoadContext.Default.Resolving += TryLoadAssemblyFromExecutionDirectory;

                //TODO: allow targeting different folder using CommandLine
                var targetConfigFolder = new Fs().GetCatalystHomeDir().FullName;
                var network = Network.Dev;

                var configCopier = new ConfigCopier();
                configCopier.RunConfigStartUp(targetConfigFolder, network, overwrite:true);

                var config = new ConfigurationBuilder()
                   .AddJsonFile(Path.Combine(targetConfigFolder, Constants.NetworkConfigFile(network)))
                   .AddJsonFile(Path.Combine(targetConfigFolder, Constants.ComponentsJsonConfigFile))
                   .AddJsonFile(Path.Combine(targetConfigFolder, Constants.SerilogJsonConfigFile))
                   .Build();

                //.Net Core service collection
                var serviceCollection = new ServiceCollection();
                //Add .Net Core services (if any) first
                //serviceCollection.AddLogging().AddDistributedMemoryCache();

                // register components from config file
                var configurationModule = new ConfigurationModule(config);
                var containerBuilder = new ContainerBuilder();
                containerBuilder.RegisterModule(configurationModule);

                var loggerConfiguration = new LoggerConfiguration().ReadFrom.Configuration(configurationModule.Configuration);
                Log.Logger = loggerConfiguration.WriteTo
                   .File(Path.Combine(targetConfigFolder, "Catalyst.Node..log"), rollingInterval: RollingInterval.Day)
                   .CreateLogger();
                containerBuilder.RegisterLogger();

                var repoFactory = RepositoryFactory.BuildSharpRepositoryConfiguation(config.GetSection("PersistenceConfiguration"));
                containerBuilder.RegisterSharpRepository(repoFactory);

                var container = containerBuilder.Build();
                using (var scope = container.BeginLifetimeScope(LifetimeTag, 
                    //Add .Net Core serviceCollection to the Autofac container.
                    b => { b.Populate(serviceCollection, LifetimeTag); }))
                {
                    var serviceProvider = new AutofacServiceProvider(scope);
                    var containedNode = container.Resolve<CatalystNode>();
                    var node = serviceProvider.GetService<CatalystNode>();
                    //Log.Logger.Information("Gossip singleton is named {0}", gossipSingleton.Name);
                }
                Environment.ExitCode = 0;
            }
            catch (Exception e)
            {
                Log.Logger.Error(e, "Catalyst.Node failed to start.");
                Environment.ExitCode = 1;
            }

            return Environment.ExitCode;
        }

        public static Assembly TryLoadAssemblyFromExecutionDirectory(AssemblyLoadContext context,
            AssemblyName assemblyName)
        {
            try
            {
                var assemblyFilePath = Path.Combine(ExecutionDirectory, $"{assemblyName.Name}.dll");
                Logger.Debug("Resolving assembly {0} from file {1}", assemblyName, assemblyFilePath);
                var assembly = context.LoadFromAssemblyPath(assemblyFilePath);
                return assembly;
            }
            catch (Exception e)
            {
                Logger.Warning(e, "Failed to load assembly {0} from file {1}.", e);
                return null;
            }
        }

        public static void LogUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                Log.Logger.Fatal("Unhandled exception, Terminating", e);
            }
            catch
            {
                using (var fs = new FileStream("error.log", FileMode.Create, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(fs))
                {
                    writer.WriteLine(e.ExceptionObject.ToString());
                    writer.WriteLine($"IsTerminating: {e.IsTerminating}");
                }
            }
        }
    }
}