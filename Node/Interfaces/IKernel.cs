using Akka.DI.Core;
using Autofac;

namespace ADL.Node.Interfaces
{
    public interface IKernel
    {
        IContainer Container { get; set; }
        INodeConfiguration Settings { get; set; }
        IDependencyResolver Resolver { get; set; }
    }
}
