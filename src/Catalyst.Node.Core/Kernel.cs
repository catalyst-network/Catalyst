using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using Autofac;
using Catalyst.Node.Core.Config;
using Catalyst.Node.Core.Helpers;
using Catalyst.Node.Core.P2P;
using Dawn;
using IModuleRegistrar = Autofac.Core.Registration.IModuleRegistrar;
using Serilog;

namespace Catalyst.Node.Core
{
    public sealed class Kernel : IDisposable
    {
        private static readonly ILogger Logger = Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static Kernel _instance;
        private static readonly object Mutex = new object();
        public IModuleRegistrar ConsensusService;
        public IModuleRegistrar ContractService;

        public IModuleRegistrar DfsService;
        public IModuleRegistrar GossipService;
        public IModuleRegistrar LedgerService;
        public IModuleRegistrar MempoolService;

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

        public bool Disposed { get; set; }

        public IContainer Container { get; set; }
        public NodeOptions NodeOptions { get; set; }
        public PeerIdentifier NodeIdentity { get; set; }

        /// <summary>
        ///     Get a thread safe kernel singleton.
        /// </summary>
        /// <returns></returns>
        public static Kernel GetInstance(NodeOptions nodeOptions, ContainerBuilder containerBuilder)
        {
            Guard.Argument(nodeOptions, nameof(nodeOptions)).NotNull();
            Guard.Argument(containerBuilder, nameof(containerBuilder)).NotNull();
            if (_instance == null)
                lock (Mutex)
                {
                    if (_instance == null)
                    {
                        try
                        {
                            var fs = new Fs();
                            var configCopier = new ConfigCopier(fs);
                            configCopier.RunConfigStartUp(nodeOptions.DataDir, Core.NodeOptions.Networks.devnet);
                            _instance = new Kernel(nodeOptions, containerBuilder.Build());
                        }
                        catch (Exception e)
                        {
                            Logger.Error(e, "Failed to create new kernel");
                            throw;
                        }
                    }
                }

            return _instance;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            Logger.Verbose("disposing catalyst kernel");
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing) Container?.Dispose();

            Disposed = true;
            Logger.Verbose("Catalyst kernel disposed");
        }
    }
}