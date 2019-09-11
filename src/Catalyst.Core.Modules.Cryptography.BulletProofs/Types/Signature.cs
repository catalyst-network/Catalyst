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

using Catalyst.Abstractions.Cryptography;

namespace Catalyst.Core.Modules.Cryptography.BulletProofs.Types
{
    public class Signature : ISignature
    {
        public byte[] SignatureBytes { get; }
        public byte[] PublicKeyBytes { get; }

        internal Signature(byte[] signature, byte[] publicKey)
        {
            var requiredSigLength = FFI.SignatureLength;
            if (signature.Length != requiredSigLength)
            {
                Error.ThrowArgumentExceptionSignatureLength(requiredSigLength);
            }

            var requiredKeyLength = FFI.PublicKeyLength;
            if (publicKey.Length != requiredKeyLength)
            {
                Error.ThrowArgumentExceptionPublicKeyLength(requiredKeyLength);
            }

            SignatureBytes = signature;
            PublicKeyBytes = publicKey;
        }        
    }
}
