namespace Catalyst.Node.Core.Modules.Gossip
{
    //This is just intended as a demo of how autofac can be used to resolve from config
    public interface INumberProvider
    {
        int Number { get; }
    }

    public class NumberProvider1 : INumberProvider
    {
        public int Number => 13;
    }
    public class NumberProvider2 : INumberProvider
    {
        public int Number => 17;
    }
}
