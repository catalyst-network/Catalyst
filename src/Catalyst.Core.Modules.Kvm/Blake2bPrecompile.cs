#region LICENSE

/**
* Copyright (c) 2024 Catalyst Network
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
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.Hashing;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Core.Extensions;
using Nethermind.Core.Specs;
using Nethermind.Evm;
using Nethermind.Evm.Precompiles;

namespace Catalyst.Core.Modules.Kvm
{
    public sealed class Blake2bPrecompiledContract : IPrecompile
    {
        private readonly IHashProvider _hashProvider;
        
        public Blake2bPrecompiledContract(IHashProvider hashProvider)
        {
            _hashProvider = hashProvider ?? throw new ArgumentNullException(nameof(hashProvider));
        }

        public Address Address { get; } = Address.FromNumber(1 + KatVirtualMachine.CatalystPrecompilesAddressingSpace);

        public long BaseGasCost(IReleaseSpec releaseSpec) { return 20L; }

        public long DataGasCost(in ReadOnlyMemory<byte> inputData, IReleaseSpec releaseSpec) { return 12L * EvmPooledMemory.Div32Ceiling((ulong) inputData.Length); }

        public (ReadOnlyMemory<byte>, bool) Run(in ReadOnlyMemory<byte> inputData, IReleaseSpec releaseSpec)
        {
            return (_hashProvider.ComputeMultiHash(inputData.ToArray()).Digest, true);
        }
    }
}
