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

using System;
using Catalyst.Abstractions.Kvm;
using Catalyst.Abstractions.Ledger;
using Nethermind.Store;

namespace Catalyst.Core.Modules.Ledger 
{
    public class Web3EthApi : IWeb3EthApi
    {
        public Web3EthApi(IDeltaExecutor deltaExecutor, IStateReader stateReader, IStateRootResolver stateRootResolver, IDeltaResolver deltaResolver)
        {
            DeltaExecutor = deltaExecutor ?? throw new ArgumentNullException(nameof(deltaExecutor));
            StateReader = stateReader ?? throw new ArgumentNullException(nameof(stateReader));
            StateRootResolver = stateRootResolver ?? throw new ArgumentNullException(nameof(stateRootResolver));
            DeltaResolver = deltaResolver ?? throw new ArgumentNullException(nameof(deltaResolver));
        }
        
        public IDeltaExecutor DeltaExecutor { get; }
        public IStateReader StateReader { get; }
        public IDeltaResolver DeltaResolver { get; }
        public IStateRootResolver StateRootResolver { get; }
    }
}
