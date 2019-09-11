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

using System.Collections.Generic;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Modules.Cryptography.BulletProofs.Types;

namespace Catalyst.Core.Modules.Cryptography.BulletProofs
{
    public class CryptoWrapper : IWrapper
    {
        public int PrivateKeyLength => FFI.PrivateKeyLength;

        public int PublicKeyLength => FFI.PublicKeyLength;
        
        public int SignatureLength => FFI.SignatureLength;

        public int SignatureContextMaxLength => FFI.SignatureContextMaxLength;

        public Byte32 GeneratePedersenCommitment(Byte32 value, Byte32 blinding)
        {
            throw new System.NotImplementedException();
        }

        public byte[] GenerateRangeProof(Byte32 value, Byte32 blinding)
        {
            throw new System.NotImplementedException();
        }

        public bool VerifyRangeProof(byte[] rangeproof, Byte32 oldCommitment, Byte32 deltaCommitment)
        {
            throw new System.NotImplementedException();
        }

        public bool BatchVerifyRangeProof(List<byte[]> rangeproofs, List<Byte32> oldCommitments, List<Byte32> deltaCommitments)
        {
            throw new System.NotImplementedException();
        }

        public IPrivateKey GeneratePrivateKey()
        {           
            var privateKeyBytes = FFI.GeneratePrivateKey();
            return new PrivateKey(privateKeyBytes);
        }
        
        public IPublicKey GetPublicKeyFromPrivate(IPrivateKey privateKey)
        {
            var publicKeyBytes = FFI.GetPublicKeyFromPrivate(privateKey.Bytes);

            return new PublicKey(publicKeyBytes);
        }

        public ISignature StdSign(IPrivateKey privateKey, byte[] messageBytes, byte[] contextBytes)
        {
            var signatureBytes = FFI.StdSign(privateKey.Bytes, messageBytes, contextBytes);
            var publicKeyBytes = FFI.GetPublicKeyFromPrivate(privateKey.Bytes);
            FFI.ValidatePublicKeyOrThrow(publicKeyBytes);

            return new Signature(signatureBytes, publicKeyBytes);            
        }

        public bool StdVerify(ISignature signature, byte[] message, byte[] context)
        {
            return FFI.StdVerify(signature.SignatureBytes, signature.PublicKeyBytes, message, context);
        }

        public Byte64 CTransactionEntry(IPublicKey publicKey, Byte32 value, Byte32 blinding, Byte32 totalFees, int noParticipants)
        {
            throw new System.NotImplementedException();
        }

        public ISignature CtSign(List<Byte64> cTransactionEntries, IPrivateKey privateKey, Byte32 blinding)
        {
            throw new System.NotImplementedException();
        }

        public bool CtVerify(List<Byte64> cTransactionEntries, ISignature cTSignature)
        {
            throw new System.NotImplementedException();
        }

        public IPrivateKey PrivateKeyFromBytes(byte[] privateKeyBytes)
        {
            return new PrivateKey(privateKeyBytes);
        }

        public IPublicKey PublicKeyFromBytes(byte[] publicKeyBytes)
        {
            FFI.ValidatePublicKeyOrThrow(publicKeyBytes);
            return new PublicKey(publicKeyBytes);
        }

        public ISignature SignatureFromBytes(byte[] signatureBytes, byte[] publicKeyBytes)
        {
            FFI.ValidatePublicKeyOrThrow(publicKeyBytes);
            return new Signature(signatureBytes, publicKeyBytes); 
        }
    }
}
