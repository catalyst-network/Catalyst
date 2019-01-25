using Autofac;
using Autofac.Core;
using Catalyst.Helpers.Ipfs;
using Dawn;

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
            Guard.Argument(builder, nameof(builder)).NotNull();
            builder.Register(c => Dfs.GetInstance(c.Resolve<IIpfs>()))
                .As<IDfs>()
                .SingleInstance();
        }
    }
}
