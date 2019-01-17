using Catalyst.Helpers.Ipfs;
using Autofac;
using Autofac.Core;

namespace Catalyst.Node.Modules.Core.Dfs
{
    public class DfsModule : ModuleBase, IDfsModule
    {
        private IIpfs Dfs { get; set; }
        private IDfsSettings DfsSettings { get; set; }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="dfsSettings"></param>
        public static ContainerBuilder Load(ContainerBuilder builder, IDfsSettings dfsSettings)
        {
            //@TODO guard util
            builder.Register(c => new DfsModule(c.Resolve<IIpfs>(),dfsSettings))
                .As<IDfsModule>()
                .InstancePerLifetimeScope();
            return builder;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dfs"></param>
        /// <param name="settings"></param>
        public DfsModule(IIpfs dfs, IDfsSettings settings)
        {
            //@TODO guard util
            Dfs = dfs;
            DfsSettings = settings;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override bool StartService()
        {
            Dfs.CreateIpfsClient(DfsSettings.IpfsVersionApi, DfsSettings.ConnectRetries);
            return true;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override bool StopService()
        {
            Dfs.DestroyIpfsClient();
            return true;
        }

        /// <summary>
        /// Get current implementation of this service
        /// </summary>
        /// <returns></returns>
        public IIpfs GetImpl()
        {
            return Dfs;
        }
    }
}
