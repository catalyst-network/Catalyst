#region LICENSE

/**
* Copyright (c) 2019 Catalyst Network
*
* This file is part of Catalyst.Node <https://github.com/catalyst-network/Catalyst.Node>
*
* Catalyst.Node is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 2 of the License, or
* (at your option) any later version.
*
* Catalyst.Node is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with Catalyst.Node. If not, see <https://www.gnu.org/licenses/>.
*/

#endregion

using Catalyst.Node.Common.Helpers.Enumerator;

namespace Catalyst.Node.Common.Helpers.Config
{
    public class Network : Enumeration
    {
        public static readonly Network Main = new MainNet();
        public static readonly Network Test = new TestNet();
        public static readonly Network Dev = new DevNet();
        private Network(int id, string name) : base(id, name) { }

        private sealed class DevNet : Network
        {
            public DevNet() : base(1, "devnet") { }
        }

        private sealed class TestNet : Network
        {
            public TestNet() : base(2, "testnet") { }
        }

        private sealed class MainNet : Network
        {
            public MainNet() : base(3, "mainnet") { }
        }
    }
}
