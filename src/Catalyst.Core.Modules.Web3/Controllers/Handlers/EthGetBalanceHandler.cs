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

using Catalyst.Abstractions.Ledger;
using Catalyst.Protocol.Deltas;
using Nethermind.Core;
using Nethermind.Dirichlet.Numerics;

namespace Catalyst.Core.Modules.Web3.Controllers.Handlers
{
    [EthWeb3RequestHandler("eth", "getBalance")]
    public class EthGetBalanceHandler : EthWeb3RequestHandler<Address, UInt256>
    {
        protected override UInt256 Handle(Address address, IWeb3EthApi api)
        {
            // change to appropriate hash
            Delta delta = api.DeltaResolver.Latest;

            // Keccak stateRoot = api.StateRootResolver.Resolve(delta.Hash); <-- we need a delta hash
            // return api.StateReader.GetBalance(stateRoot, address);
            return new UInt256(1000000);
        }
    }
}
