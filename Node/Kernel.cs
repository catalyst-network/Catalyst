using ADL.Node.Interfaces;
using Autofac;
using ADL.Node.Interfaces;
using Akka.DI.Core;

namespace ADL.Node
{
    internal class Kernel : IKernel
    {
        public INodeConfiguration Settings { get; set; }
        public IDependencyResolver Resolver { get; set; }
        
        /// <summary>
        /// Kernel constructor.
        /// </summary>
        /// <param name="container"></param>
        /// <param name="settings"></param>
        internal Kernel(IDependencyResolver resolver, INodeConfiguration settings)
        {
            Resolver = resolver;
            Settings = settings;
        }
    }
}
