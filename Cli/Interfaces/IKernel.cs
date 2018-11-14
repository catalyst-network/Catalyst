using Autofac;

namespace ADL.Cli.Interfaces
{
    internal interface IKernel
    {
        IContainer Container { get; set; }
        INodeConfiguration Settings { get; set; }
    }
}
