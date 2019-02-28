﻿using Catalyst.Node.Common.Helpers;

namespace Catalyst.Node.Common.P2P
{
    public abstract class Network : Enumeration
    {
        public static readonly Network Main = new MainNet();
        public static readonly Network Test = new TestNet();
        public static readonly Network Dev = new DevNet();

        protected Network(int id, string name) : base(id, name) {}

        private class DevNet : Network { public DevNet() : base(1, "devnet") { } }
        private class TestNet : Network { public TestNet() : base(2, "testnet") { } }
        private class MainNet : Network { public MainNet() : base(3, "mainnet") { } }
    }
}
