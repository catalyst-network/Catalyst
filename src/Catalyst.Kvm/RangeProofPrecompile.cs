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

using Nethermind.Core;
using Nethermind.Core.Specs;
using Nethermind.Evm.Precompiles;

namespace Catalyst.Kvm
{
    /// <inheritdoc />
    public sealed class RangeProofPrecompile : IPrecompiledContract
    {
        private const int EthereumPrecompilesAddressingSpace = 0xffff;

        /// <inheritdoc />
        /// <summary>
        /// https: //github.com/ethereum/EIPs/blob/master/EIPS/eip-1352.md
        /// 65535 (0xffff) will be registered for Ethereum, so we can start after that
        /// </summary>
        public Address Address => AddressInKvm;
        
        public static Address AddressInKvm { get; } = Address.FromNumber(1 + EthereumPrecompilesAddressingSpace);

        /// <inheritdoc />
        public long BaseGasCost(IReleaseSpec releaseSpec) { return 200000; } // numbers need to be benchmarked

        /// <inheritdoc />
        public long DataGasCost(byte[] inputData, IReleaseSpec releaseSpec) { return 0; } // numbers need to be benchmarked

        /// <inheritdoc />
        public (byte[], bool) Run(byte[] inputData) { return (new byte[32], true); }
    }
}
