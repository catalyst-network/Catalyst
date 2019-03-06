/*
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

using System;
using Catalyst.Node.Common.Interfaces;
using NSec.Cryptography;

namespace Catalyst.Node.Common.Helpers.Cryptography
{
    /// <summary>
    ///     Provides NSec crypto operations on wrapped keys.
    /// </summary>
    public sealed class NSecCryptoContext : ICryptoContext
    {
        private static readonly SignatureAlgorithm _algorithm = SignatureAlgorithm.Ed25519;
        private static readonly KeyBlobFormat _publicKeyFormat = KeyBlobFormat.PkixPublicKey;
        private static readonly KeyBlobFormat _privateKeyFormat = KeyBlobFormat.PkixPrivateKey;

        public IPrivateKey GeneratePrivateKey()
        {
            //Newly generated private keys can be exported once.
            var keyParams = new KeyCreationParameters {ExportPolicy = KeyExportPolicies.AllowPlaintextArchiving};
            var key = Key.Create(_algorithm, keyParams);
            return new NSecPrivateKeyWrapper(key);
        }

        public IPublicKey ImportPublicKey(ReadOnlySpan<byte> blob)
        {
            var nSecKey = PublicKey.Import(_algorithm, blob, _publicKeyFormat);
            return new NSecPublicKeyWrapper(nSecKey);
        }

        /// <summary>
        ///     Exports public key. Can throw unhandled exception or return null.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public byte[] ExportPublicKey(IPublicKey key) { return key?.GetNSecFormatPublicKey().Export(_publicKeyFormat); }

        public IPrivateKey ImportPrivateKey(ReadOnlySpan<byte> blob)
        {
            var nSecKey = Key.Import(_algorithm, blob, _privateKeyFormat);
            return new NSecPrivateKeyWrapper(nSecKey);
        }

        /// <summary>
        ///     Exports private key. Can throw unhandled exception or return null.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public byte[] ExportPrivateKey(IPrivateKey key)
        {
            return key?.GetNSecFormatPrivateKey().Export(_privateKeyFormat);
        }

        public byte[] Sign(IPrivateKey privateKey, ReadOnlySpan<byte> data)
        {
            var realKey = privateKey.GetNSecFormatPrivateKey();
            return _algorithm.Sign(realKey, data);
        }

        public bool Verify(IPublicKey key, ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature)
        {
            var realKey = key.GetNSecFormatPublicKey();
            return _algorithm.Verify(realKey, data, signature);
        }

        public IPublicKey GetPublicKey(IPrivateKey key)
        {
            var realPublicKey = key.GetNSecFormatPublicKey();
            return new NSecPublicKeyWrapper(realPublicKey);
        }
    }
}