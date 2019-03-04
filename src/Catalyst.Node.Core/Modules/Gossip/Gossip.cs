using Catalyst.Node.Common.Modules.Gossip;

namespace Catalyst.Node.Core.Modules.Gossip
{
    public class Gossip : IGossip
    {
        public Gossip(INameProvider nameProvider, INumberProvider numberProvider)
        {
            Name = nameProvider.Name;
            Number = numberProvider.Number;
        }
        public string Name { get; }
        public int Number { get; }
    }
}