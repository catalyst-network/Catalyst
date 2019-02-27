using Catalyst.Node.Common.Modules.Gossip;

namespace Catalyst.Node.Core.Modules.Gossip
{
    public class Gossip : IGossip
    {
        public Gossip(INameProvider nameProvider) { Name = nameProvider.Name; }
        public string Name { get; }
    }
}