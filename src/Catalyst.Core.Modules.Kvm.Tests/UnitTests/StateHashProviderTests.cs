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

using Nethermind.Evm;
using NUnit.Framework;

namespace Catalyst.Core.Modules.Kvm.Tests.UnitTests
{
    public sealed class StateHashProviderTests
    {
        [Ignore("Not implemented yet")]
        public void Can_provide_a_latest_state_update_hash()
        {
            var stateUpdate = new StateUpdate();

            var stateUpdateHashProvider = new StateUpdateHashProvider();
            stateUpdateHashProvider.GetHash(stateUpdate, 1);
        }
    }
}
