using Dawn;
using Autofac;
using Catalyst.Node.Common;
using Catalyst.Node.Common.Modules;

namespace Catalyst.Node.Core.Modules.Dfs
{
    public class DfsModule : Module
    {
        /// <summary>
        /// </summary>
        /// <param name="builder"></param>
        protected override void Load (ContainerBuilder builder)
        {
            Guard.Argument(builder, nameof(builder)).NotNull();
            builder.Register(c => new Dfs(c.Resolve<IIpfs>()))
                .As<IDfs>()
                .SingleInstance();
        }
    }
}
