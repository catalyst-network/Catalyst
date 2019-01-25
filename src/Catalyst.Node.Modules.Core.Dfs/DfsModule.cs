using Autofac;
using Autofac.Core;
using Catalyst.Helpers.Ipfs;

namespace Catalyst.Node.Modules.Core.Dfs
{
    public class DfsModule : Module
    {
        /// <summary>
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="dfsSettings"></param>
        protected override void Load (ContainerBuilder builder)
        {
            //@TODO guard util
            builder.Register(c => Dfs.GetInstance(c.Resolve<IIpfs>()))
                .As<IDfs>()
                .SingleInstance();
        }
    }
}
