using Autofac;
using ADL.Cli.Interfaces;

namespace ADL.Cli
{
    internal class Kernel : IKernel
    {
        public IContainer Container { get; set; }
        public INodeConfiguration Settings { get; set; }
        
        internal Kernel(IContainer container, INodeConfiguration settings)
        {
            Container = container;
            Settings = settings;
        }
    }
}
