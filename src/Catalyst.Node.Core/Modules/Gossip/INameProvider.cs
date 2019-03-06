namespace Catalyst.Node.Core.Modules.Gossip
{
    //This is just intended as a demo of how autofac can be used to resolve from config
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