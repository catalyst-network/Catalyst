using Catalyst.Node.Common.Helpers.Enumerator;

namespace Catalyst.Node.Common.Helpers.Config
{
    public abstract class Network : Enumeration
    {
        public static readonly Network Main = new MainNet();
        public static readonly Network Test = new TestNet();
        public static readonly Network Dev = new DevNet();
        private Network(int id, string name) : base(id, name) { }

        private class DevNet : Network
        {
            public DevNet() : base(1, "devnet") { }
        }

        private class TestNet : Network
        {
            public TestNet() : base(2, "testnet") { }
        }

        private class MainNet : Network
        {
            public MainNet() : base(3, "mainnet") { }
        }
    }
}