using Autofac;
using Catalyst.Node.Common;
using Catalyst.Node.Common.Modules;
using Catalyst.Node.Core.Helpers.Ipfs;
using Dawn;

namespace Catalyst.Node.Core.Modules.Dfs
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
            builder.Register(c => new Dfs(c.Resolve<IIpfs>()))
                .As<IDfs>()
                .SingleInstance();
        }
    }
}
