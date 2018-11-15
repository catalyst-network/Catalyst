using Akka.DI.Core;

namespace ADL.Node.Interfaces
{
    public interface IKernel
    {
        INodeConfiguration Settings { get; set; }
        IDependencyResolver Resolver { get; set; }
    }
}
