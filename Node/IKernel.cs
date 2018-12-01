using Akka.Actor;
using Autofac;
using Akka.DI.Core;

namespace ADL.Node
{
    public interface IKernel
    {
        Settings Settings { get; set; }
        IContainer Container { get; set; }
        IDependencyResolver Resolver { get; set; }
    }
}
