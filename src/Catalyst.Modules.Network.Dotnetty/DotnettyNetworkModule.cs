using Autofac;
using Catalyst.Abstractions.IO.EventLoop;
using Catalyst.Core.Lib.IO.EventLoop;

namespace Catalyst.Modules.Network.Dotnetty
{
    public class DotnettyNetworkModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // Register IO.EventLoop
            builder.RegisterType<UdpClientEventLoopGroupFactory>().As<IUdpClientEventLoopGroupFactory>()
               .SingleInstance();
            builder.RegisterType<UdpServerEventLoopGroupFactory>().As<IUdpServerEventLoopGroupFactory>()
               .SingleInstance();
            builder.RegisterType<TcpServerEventLoopGroupFactory>().As<ITcpServerEventLoopGroupFactory>()
               .SingleInstance();
            builder.RegisterType<TcpClientEventLoopGroupFactory>().As<ITcpClientEventLoopGroupFactory>();
            builder.RegisterType<EventLoopGroupFactoryConfiguration>().As<IEventLoopGroupFactoryConfiguration>()
               .WithProperty("TcpServerHandlerWorkerThreads", 4)
               .WithProperty("TcpClientHandlerWorkerThreads", 4)
               .WithProperty("UdpServerHandlerWorkerThreads", 8)
               .WithProperty("UdpClientHandlerWorkerThreads", 2);
        }
    }
}
