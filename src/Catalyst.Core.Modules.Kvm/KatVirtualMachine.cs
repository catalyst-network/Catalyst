#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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
using System.Numerics;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.Hashing;
using Catalyst.Abstractions.Kvm;
using Catalyst.Core.Modules.Hashing;
using MultiFormats.Registry;
using Nethermind.Core;
using Nethermind.Core.Extensions;
using Nethermind.Core.Specs;
using Nethermind.Evm;
using Nethermind.Logging;
using Nethermind.State;

namespace Catalyst.Core.Modules.Kvm
{
    public sealed class KatVirtualMachine : VirtualMachine, IKvm
    {
        private readonly IHashProvider _hashProvider;
        private readonly ICryptoContext _cryptoContext;
        public const int CatalystPrecompilesAddressingSpace = 0xffff;

        public KatVirtualMachine(IStateProvider stateProvider,
            IStorageProvider storageProvider,
            IStateUpdateHashProvider blockhashProvider,
            ISpecProvider specProvider,
            IHashProvider hashProvider,
            ICryptoContext cryptoContext,
            ILogManager logManager)
            : base(stateProvider, storageProvider, blockhashProvider, specProvider, logManager)
        {
            _hashProvider = hashProvider ?? throw new ArgumentNullException(nameof(hashProvider));
            _cryptoContext = cryptoContext ?? throw new ArgumentNullException(nameof(cryptoContext));

            AddCatalystPrecompiledContracts();
        }

        private void AddCatalystPrecompiledContracts()
        {
            Blake2bPrecompiledContract blake2BPrecompiledContract = new(new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("blake2b-256")));
            Precompiles[blake2BPrecompiledContract.Address] = blake2BPrecompiledContract;

            Ed25519VerifyPrecompile ed25519VerifyPrecompile = new(_cryptoContext);
            Precompiles[ed25519VerifyPrecompile.Address] = ed25519VerifyPrecompile;
        }

        protected override bool IsPrecompiled(Address address, IReleaseSpec releaseSpec)
        {
            return base.IsPrecompiled(address, releaseSpec) || IsCatalystPrecompiled(address);
        }

        private static bool IsCatalystPrecompiled(Address address)
        {
            if (address[0] != 0)
            {
                return false;
            }

            BigInteger asInt = address.Bytes.ToUnsignedBigInteger();
            return
                asInt > CatalystPrecompilesAddressingSpace
             && asInt <= CatalystPrecompilesAddressingSpace + 2;
        }
    }
}
