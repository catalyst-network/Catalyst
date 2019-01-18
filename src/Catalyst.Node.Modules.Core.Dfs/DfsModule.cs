using Autofac;
using Catalyst.Helpers.Ipfs;

namespace Catalyst.Node.Modules.Core.Dfs
{
    public class DfsModule : ModuleBase, IDfsModule
    {
        /// <summary>
        /// </summary>
        /// <param name="dfs"></param>
        /// <param name="settings"></param>
        public DfsModule(IIpfs dfs, IDfsSettings settings)
        {
            //@TODO guard util
            Dfs = dfs;
            DfsSettings = settings;
        }

        private IIpfs Dfs { get; }
        private IDfsSettings DfsSettings { get; }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override bool StartService()
        {
            Dfs.CreateIpfsClient(DfsSettings.IpfsVersionApi, DfsSettings.ConnectRetries);
            return true;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override bool StopService()
        {
            Dfs.DestroyIpfsClient();
            return true;
        }

        /// <summary>
        ///     Get current implementation of this service
        /// </summary>
        /// <returns></returns>
        public IIpfs GetImpl()
        {
            return Dfs;
        }

        /// <summary>
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="dfsSettings"></param>
        public static ContainerBuilder Load(ContainerBuilder builder, IDfsSettings dfsSettings)
        {
            //@TODO guard util
            builder.Register(c => new DfsModule(c.Resolve<IIpfs>(), dfsSettings))
                .As<IDfsModule>()
                .InstancePerLifetimeScope();
            return builder;
        }
    }
}