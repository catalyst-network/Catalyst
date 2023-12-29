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
using System.Collections.Generic;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;

namespace Lib.P2P.Cryptography
{
    /// <summary>
    ///   A short term key on a curve.
    /// </summary>
    /// <remarks>
    ///   Ephemeral keys are different from other keys in IPFS; they are NOT
    ///   protobuf encoded and are NOT self describing.  The encoding is an
    ///   uncompressed ECPoint; the first byte s a 4 and followed by X and Y co-ordinates.
    ///   <para>
    ///   It as assummed that the curve name is known a priori.
    ///   </para>
    /// </remarks>
    public sealed class EphermalKey
    {
        private ECPublicKeyParameters _publicKey;
        private ECPrivateKeyParameters _privateKey;

        /// <summary>
        ///   Gets the IPFS encoding of the public key.
        /// </summary>
        /// <returns>
        ///   Returns the uncompressed EC point.
        /// </returns>
        internal byte[] PublicKeyBytes() { return _publicKey.Q.GetEncoded(false); }

        /// <summary>
        ///   Create a shared secret between this key and another.
        /// </summary>
        /// <param name="other">
        ///   Another ephermal key.
        /// </param>
        /// <returns>
        ///   The shared secret as a byte array.
        /// </returns>
        /// <remarks>
        ///   Uses the ECDH agreement algorithm to generate the shared secet.
        /// </remarks>
        public byte[] GenerateSharedSecret(EphermalKey other)
        {
            var agreement = AgreementUtilities.GetBasicAgreement("ECDH");
            agreement.Init(_privateKey);
            var secret = agreement.CalculateAgreement(other._publicKey);
            return BigIntegers.AsUnsignedByteArray(agreement.GetFieldSize(), secret);
        }

        /// <summary>
        ///   Create a public key from the IPFS ephermal encoding.
        /// </summary>
        /// <param name="curveName">
        ///   The name of the curve, for example "P-256".
        /// </param>
        /// <param name="bytes">
        ///   The IPFS encoded ephermal key.
        /// </param>
        internal static EphermalKey CreatePublicKeyFromDfs(string curveName, byte[] bytes)
        {
            var ecP = ECNamedCurveTable.GetByName(curveName);
            if (ecP == null)
            {
                throw new KeyNotFoundException($"Unknown curve name '{curveName}'.");
            }
            
            var domain = new ECDomainParameters(ecP.Curve, ecP.G, ecP.N, ecP.H, ecP.GetSeed());
            var q = ecP.Curve.DecodePoint(bytes);
            return new EphermalKey
            {
                _publicKey = new ECPublicKeyParameters(q, domain)
            };
        }

        /// <summary>
        ///   Create a new ephermal key on the curve.
        /// </summary>
        /// <param name="curveName">
        ///   The name of the curve, for example "P-256".
        /// </param>
        /// <returns>
        ///   The new created emphermal key.
        /// </returns>
        public static EphermalKey Generate(string curveName)
        {
            var ecP = ECNamedCurveTable.GetByName(curveName);
            if (ecP == null)
            {
                throw new Exception($"Unknown curve name '{curveName}'.");
            }
            
            var domain = new ECDomainParameters(ecP.Curve, ecP.G, ecP.N, ecP.H, ecP.GetSeed());
            var g = GeneratorUtilities.GetKeyPairGenerator("EC");
            g.Init(new ECKeyGenerationParameters(domain, new SecureRandom()));
            var keyPair = g.GenerateKeyPair();

            return new EphermalKey
            {
                _privateKey = (ECPrivateKeyParameters) keyPair.Private,
                _publicKey = (ECPublicKeyParameters) keyPair.Public
            };
        }
    }
}
