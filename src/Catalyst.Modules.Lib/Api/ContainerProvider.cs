using System;
using System.Collections.Generic;
using System.Text;
using Autofac;

namespace Catalyst.Modules.Lib.Api
{
    public interface IContainerProvider
    {
        IContainer Container { get; }
    }

    public class ContainerProvider : IContainerProvider
    {
        public ContainerProvider(IContainer container) { Container = container; }
        public IContainer Container { get; }
    }
}
