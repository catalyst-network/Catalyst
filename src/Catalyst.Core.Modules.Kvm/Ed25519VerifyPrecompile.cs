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
using Catalyst.Abstractions.Cryptography;
using Nethermind.Core;
using Nethermind.Core.Extensions;
using Nethermind.Core.Specs;
using Nethermind.Evm.Precompiles;

namespace Catalyst.Core.Modules.Kvm
{
    public sealed class Ed25519VerifyPrecompile : IPrecompile
    {
        private readonly ICryptoContext _cryptoContext;

        public Ed25519VerifyPrecompile(ICryptoContext cryptoContext) { _cryptoContext = cryptoContext ?? throw new ArgumentNullException(nameof(cryptoContext)); }

        public Address Address { get; } = Address.FromNumber(2 + KatVirtualMachine.CatalystPrecompilesAddressingSpace);

        public long DataGasCost(in ReadOnlyMemory<byte> inputData, IReleaseSpec releaseSpec) { return 0L; }

        public long BaseGasCost(IReleaseSpec releaseSpec) { return 3000L; }

        public (ReadOnlyMemory<byte>, bool) Run(in ReadOnlyMemory<byte> inputData, IReleaseSpec spec)
        {
            if (inputData.Length != 160)
            {
                return (Bytes.Empty, false);
            }

            byte[] message = inputData.Slice(0, 32).ToArray();
            byte[] signatureBytes = inputData.Slice(32, 64).ToArray();
            byte[] signingContext = inputData.Slice(96, 32).ToArray();
            byte[] publicKey = inputData.Slice(128, 32).ToArray();

            ISignature signature = _cryptoContext.GetSignatureFromBytes(signatureBytes, publicKey);
            return _cryptoContext.Verify(signature, message, signingContext)
                ? (new byte[] {1}, true)
                : (new byte[] {0}, true);
        }
    }
}
