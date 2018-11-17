using ADL.Node.Interfaces;
using Akka.DI.Core;
using Autofac;

namespace ADL.Node
{
    internal class Kernel : IKernel
    {
        public IContainer Container { get; set; }
        public INodeConfiguration Settings { get; set; }
        public IDependencyResolver Resolver { get; set; }
        
       /// <summary>
       /// Kernel constructor.
       /// </summary>
       /// <param name="resolver"></param>
       /// <param name="settings"></param>
       /// <param name="container"></param>
        internal Kernel(IDependencyResolver resolver, INodeConfiguration settings, IContainer container)
        {
            Container = container;
            Resolver = resolver;
            Settings = settings;
        }
    }
}
