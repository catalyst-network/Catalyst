using System;
using System.Collections.Generic;
using System.Text;

namespace Catalyst.Node.Core.Modules.Gossip
{
    public interface INameProvider
    {
        string Name { get; }
    }

    public class NameProvider1 : INameProvider
    {
        public string Name => "Gossip1";
    }
    public class NameProvider2 : INameProvider
    {
        public string Name => "Gossip2";
    }
}
