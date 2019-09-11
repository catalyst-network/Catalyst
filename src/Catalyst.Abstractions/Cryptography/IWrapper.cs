#region LICENSE

/*
 * Copyright (c) 2019 Catalyst Network
 *
 * This file is part of Catalyst.Cryptography.BulletProofs.Wrapper <https://github.com/catalyst-network/Rust.Cryptography.FFI.Wrapper>
 *
 * Catalyst.Cryptography.BulletProofs.Wrapper is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 2 of the License, or
 * (at your option) any later version.
 * 
 * Catalyst.Cryptography.BulletProofs.Wrapper is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with Catalyst.Cryptography.BulletProofs.Wrapper If not, see <https://www.gnu.org/licenses/>.
*/

#endregion

using System.Collections.Generic;
using Catalyst.Abstractions.Types;

namespace Catalyst.Abstractions.Cryptography
{
    public interface IWrapper
    {
        /// <summary>
        /// Private key byte length
        /// </summary>
        int PrivateKeyLength { get; }

        /// <summary>
        /// Public key byte length
        /// </summary>
        int PublicKeyLength { get; }

        /// <summary>
        /// Signature byte length
        /// </summary>
        int SignatureLength { get; }

        int SignatureContextMaxLength { get; }

        /// <summary>
        /// Generates a pederson commitment from a secret value and a blinding factor
        /// </summary>
        /// <param name="value"></param>
        /// <param name="blinding"></param>
        /// <returns></returns>
        Byte32 GeneratePedersenCommitment(Byte32 value, Byte32 blinding);

        /// <summary>
        /// Creates a range proof from a secret value and a blinding factor
        /// </summary>
        /// <param name="value"></param>
        /// <param name="blinding"></param>
        /// <returns></returns>
        byte[] GenerateRangeProof(Byte32 value, Byte32 blinding);

        /// <summary>
        /// Verifies that the pedersen commitment represents a value in the allowed range.
        /// </summary>
        /// <param name="rangeproof"></param>
        /// <param name="oldCommitment"></param>
        /// <param name="deltaCommitment"></param>
        /// <returns></returns>
        bool VerifyRangeProof(byte[] rangeproof, Byte32 oldCommitment, Byte32 deltaCommitment);

        /// <summary>
        /// Verifies the commitments represent values in the allowed range. Returns false if any rangeproofs fail.
        /// </summary>
        /// <param name="rangeproofs"></param>
        /// <param name="oldCommitments"></param>
        /// <param name="deltaCommitments"></param>
        /// <returns></returns>
        bool BatchVerifyRangeProof(List<byte[]> rangeproofs, List<Byte32> oldCommitments, List<Byte32> deltaCommitments);

        /// <summary>
        /// Generates a private key for use in ed25519.
        /// </summary>
        /// <returns></returns>
        IPrivateKey GeneratePrivateKey();

        /// <summary>
        /// Returns public key corresponding to private key.
        /// </summary>
        /// <param name="privateKey"></param>
        /// <returns></returns>
        IPublicKey GetPublicKeyFromPrivate(IPrivateKey privateKey);

        /// <summary>
        /// Signs a message using ed25519ph signature scheme.
        /// </summary>
        /// <param name="privateKey"></param>
        /// <param name="messageBytes"></param>
        /// <param name="contextBytes"></param>
        /// <returns></returns>
        ISignature StdSign(IPrivateKey privateKey, byte[] messageBytes, byte[] contextBytes);

        /// <summary>
        /// Verifies that the signature validates for the given message, public key, and context using ed25519ph.
        /// </summary>
        /// <param name="signature"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        bool StdVerify(ISignature signature, byte[] message, byte[] context);

        /// <summary>
        /// Creates a confidential transaction entry.
        /// </summary>
        /// <param name="publicKey"></param>
        /// <param name="value"></param>
        /// <param name="blinding"></param>
        /// <param name="totalFees"></param>
        /// <param name="noParticipants"></param>
        /// <returns></returns>
        Byte64 CTransactionEntry(IPublicKey publicKey, Byte32 value, Byte32 blinding, Byte32 totalFees, int noParticipants);

        /// <summary>
        /// Creates a partial signature given a list of confidential transaction entries.
        /// </summary>
        /// <param name="cTransactionEntries"></param>
        /// <param name="privateKey"></param>
        /// <param name="blinding"></param>
        /// <returns></returns>
        ISignature CTSign(List<Byte64> cTransactionEntries, IPrivateKey privateKey, Byte32 blinding);

        /// <summary>
        /// Verifies an aggregated signature given a list of confidential transaction entries. 
        /// </summary>
        /// <param name="cTransactionEntries"></param>
        /// <param name="cTSignature"></param>
        /// <returns></returns>
        bool CTVerify(List<Byte64> cTransactionEntries, ISignature cTSignature);

        /// <summary>
        /// Takes byte array and if valid creates a private key.
        /// </summary>
        /// <param name="keyBytes"></param>
        /// <returns></returns>
        IPrivateKey PrivateKeyFromBytes(byte[] keyBytes);

        /// <summary>
        /// Takes byte array and if valid creates a private key.
        /// </summary>
        /// <param name="keyBytes"></param>
        /// <returns></returns>
        IPublicKey PublicKeyFromBytes(byte[] keyBytes);

        /// <summary>
        /// Take byte arrays corresponding to signature and public key and creates a signature.
        /// </summary>
        /// <param name="signature"></param>
        /// <param name="publicKey"></param>
        /// <returns></returns>
        ISignature SignatureFromBytes(byte[] signature, byte[] publicKey);
    }
}
