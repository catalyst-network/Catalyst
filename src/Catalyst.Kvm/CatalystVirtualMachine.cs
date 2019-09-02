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

using System.Numerics;
using Nethermind.Core;
using Nethermind.Core.Extensions;
using Nethermind.Core.Specs;
using Nethermind.Evm;
using Nethermind.Logging;
using Nethermind.Store;

namespace Catalyst.Kvm
{
    public sealed class CatalystVirtualMachine : VirtualMachine
    {
        public CatalystVirtualMachine(IStateProvider stateProvider, IStorageProvider storageProvider, IStateUpdateHashProvider blockhashProvider, ISpecProvider specProvider, ILogManager logManager)
            : base(stateProvider, storageProvider, blockhashProvider, specProvider, logManager) { }

        protected override void InitializePrecompiledContracts()
        {
            base.InitializePrecompiledContracts();
            Precompiles[RangeProofPrecompile.AddressInKvm] = new RangeProofPrecompile();
        }

        private static BigInteger _rangeProofAddressAsInt = RangeProofPrecompile.AddressInKvm.Bytes.ToUnsignedBigInteger();
        
        /// <summary>
        /// This will probably be removed
        /// </summary>
        protected override bool IsPrecompiled(Address address, IReleaseSpec releaseSpec)
        {
            // this will be optimized
            BigInteger asInt = address.Bytes.ToUnsignedBigInteger();
            return base.IsPrecompiled(address, releaseSpec) || asInt == _rangeProofAddressAsInt;
        }
    }
}
