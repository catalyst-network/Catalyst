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
using Catalyst.Cryptography.BulletProofs.Wrapper.Interfaces;

namespace Catalyst.Abstractions.Cryptography
{
    public interface ICryptoContext
    {
        /// <summary>
        ///     Generates a new private key.
        /// </summary>
        /// <returns></returns>
        IPrivateKey GeneratePrivateKey();

        /// <summary>
        ///     Creates public key from public key bytes.
        /// </summary>
        /// <param name="publicKeyBytes"></param>
        /// <returns></returns>
        IPublicKey PublicKeyFromBytes(byte[] publicKeyBytes);

        /// <summary>
        ///     Creates private key from key bytes.
        /// </summary>
        /// <param name="privateKeyBytes"></param>
        /// <returns></returns>
        IPrivateKey PrivateKeyFromBytes(byte[] privateKeyBytes);

        /// <summary>
        ///     Takes signature bytes and corresponding public key bytes and creates a signature.
        /// </summary>
        /// <param name="signatureBytes"></param>
        /// <param name="publicKeyBytes"></param>
        /// <returns></returns>
        ISignature SignatureFromBytes(byte[] signatureBytes, byte[] publicKeyBytes);

        /// <summary>
        /// Returns private key bytes.
        /// </summary>
        /// <param name="privateKey"></param>
        /// <returns></returns>
        byte[] ExportPrivateKey(IPrivateKey privateKey);

        /// <summary>
        /// Returns public key bytes.
        /// </summary>
        /// <param name="publicKey"></param>
        /// <returns></returns>
        byte[] ExportPublicKey(IPublicKey publicKey);

        /// <summary>
        ///     Signs message using provided private key and returns the signature.
        /// </summary>
        /// <param name="privateKey"></param>
        /// <param name="message"></param>
        /// <param name="context"></param>
        /// <param name=""></param>
        /// <returns></returns>
        ISignature Sign(IPrivateKey privateKey, ReadOnlySpan<byte> message, ReadOnlySpan<byte> context);

        bool Verify(ISignature signature, ReadOnlySpan<byte> message, ReadOnlySpan<byte> context);

        /// <summary>
        ///     Given a private key returns corresponding public key.
        /// </summary>
        /// <param name="privateKey"></param>
        /// <returns></returns>
        IPublicKey GetPublicKey(IPrivateKey privateKey);

        /// <summary>
        /// Private key byte length.
        /// </summary>
        int PrivateKeyLength { get; }

        /// <summary>
        /// Public key byte length.
        /// </summary>
        int PublicKeyLength { get; }

        /// <summary>
        /// Signature byte length.
        /// </summary>
        int SignatureLength { get; }

        int SignatureContextMaxLength { get; }
    }
}
