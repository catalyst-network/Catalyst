using System;
using Autofac;
using System.IO;
using System.Runtime.Loader;
using Autofac.Configuration;
using Catalyst.Helpers.Util;
using Catalyst.Helpers.FileSystem;
using Dawn;

namespace Catalyst.Node
{
    public sealed class Kernel : IDisposable
    {
        private static Kernel _instance;
        private static readonly object Mutex = new object();

        /// <summary>
        ///     Private kernel constructor.
        /// </summary>
        /// <param name="nodeOptions"></param>
        /// <param name="container"></param>
        private Kernel(NodeOptions nodeOptions, IContainer container)
        {
            Guard.Argument(nodeOptions, nameof(nodeOptions)).NotNull();
            Guard.Argument(container, nameof(container)).NotNull();
            NodeOptions = nodeOptions;
            Container = container;
        }

        public IContainer Container { get; set; }
        private static NodeOptions NodeOptions { get; set; }

        /// <summary>
        ///     Get a thread safe kernel singleton.
        ///     @TODO need check that if we dont pass all module options you cant start a livenet instance
        /// </summary>
        /// <returns></returns>
        public static Kernel GetInstance(NodeOptions nodeOptions)
        {
            Guard.Argument(nodeOptions, nameof(nodeOptions)).NotNull();
            if (_instance == null)
                lock (Mutex)
                {
                    if (_instance == null)
                    {
                        // check supplied data dir exists
                        if (!Fs.DataDirCheck(nodeOptions.DataDir))
                        {
                            // not there make one
                            Fs.CreateSystemFolder(nodeOptions.DataDir);
                            // make config with new system folder
                            Fs.CopySkeletonConfigs(nodeOptions.DataDir, nodeOptions.Network);
                        }
                        else
                        {
                            // dir does exist, check config exits
                            if (!Fs.CheckConfigExists(nodeOptions.DataDir, nodeOptions.Network))
                                Fs.CopySkeletonConfigs(nodeOptions.DataDir, nodeOptions.Network);
                        }
                        
                        _instance = new Kernel(nodeOptions, ConfigureContainer(nodeOptions)); //@TODO try catch
                    }
                }
            return _instance;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        private static IContainer ConfigureContainer(NodeOptions options)
        {
            // Set path to load assemblies from ** be-carefull **
            AssemblyLoadContext.Default.Resolving += (context, assembly) =>
                context.LoadFromAssemblyPath(Path.Combine(Directory.GetCurrentDirectory(),
                    $"{assembly.Name}.dll"));
            
            // get builder
            var builder = new ContainerBuilder();
            
            // register our options object
            builder.RegisterType<NodeOptions>();

            // load module config file
            var moduleConfig = new ConfigurationBuilder()
                .AddJsonFile($"{options.DataDir}/modules.json")
                .Build();
            
            // register modules
            var coreModules = new ConfigurationModule(moduleConfig);
            builder.RegisterModule(coreModules);
            
            // load components config file
            var componentConfig = new ConfigurationBuilder()
                .AddJsonFile($"{options.DataDir}/components.json")
                .Build();
            
            // register components
            var components = new ConfigurationModule(componentConfig);
            builder.RegisterModule(components);

            var container = builder.Build();   

            return container;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            Container?.Dispose();
        }
    }
}