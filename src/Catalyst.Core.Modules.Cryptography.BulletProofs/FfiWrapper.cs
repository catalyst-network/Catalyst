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
using System.Collections.Generic;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Modules.Cryptography.BulletProofs.Types;

namespace Catalyst.Core.Modules.Cryptography.BulletProofs
{
    public sealed class FfiWrapper : ICryptoContext
    {
        public int PrivateKeyLength => NativeBinding.PrivateKeyLength;

        public int PublicKeyLength => NativeBinding.PublicKeyLength;
        
        public int SignatureLength => NativeBinding.SignatureLength;

        public int SignatureContextMaxLength => NativeBinding.SignatureContextMaxLength;

        public ISignature Sign(IPrivateKey privateKey, byte[] messageBytes, byte[] contextBytes)
        {
            return NativeBinding.StdSign(privateKey.Bytes, messageBytes, contextBytes);           
        }

        public bool Verify(ISignature signature, byte[] message, byte[] context)
        {
            return NativeBinding.StdVerify(signature.SignatureBytes, signature.PublicKeyBytes, message, context);
        }
        
        public IPrivateKey GeneratePrivateKey()
        {           
            return NativeBinding.GeneratePrivateKey();
        }
        
        public IPublicKey GetPublicKeyFromPrivateKey(IPrivateKey privateKey)
        {
            return NativeBinding.GetPublicKeyFromPrivate(privateKey.Bytes);
        }

        public IPrivateKey GetPrivateKeyFromBytes(byte[] privateKeyBytes)
        {
            return new PrivateKey(privateKeyBytes);
        }

        public IPublicKey GetPublicKeyFromBytes(byte[] publicKeyBytes)
        {
            NativeBinding.ValidatePublicKeyOrThrow(publicKeyBytes);
            return new PublicKey(publicKeyBytes);
        }

        public ISignature GetSignatureFromBytes(byte[] signatureBytes, byte[] publicKeyBytes)
        {
            NativeBinding.ValidatePublicKeyOrThrow(publicKeyBytes);
            return new Signature(signatureBytes, publicKeyBytes); 
        }
        
        /// <inheritdoc />
        public byte[] ExportPrivateKey(IPrivateKey privateKey)
        {
            return privateKey.Bytes;
        }

        /// <inheritdoc />
        public byte[] ExportPublicKey(IPublicKey publicKey)
        {
            return publicKey.Bytes;
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
    }
}
