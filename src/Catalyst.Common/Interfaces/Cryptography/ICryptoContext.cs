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

namespace Catalyst.Common.Interfaces.Cryptography
{
    public interface ICryptoContext
    {
        /// <summary>
        ///     Generates a wrapped private key using underlying crypto context.
        /// </summary>
        /// <returns></returns>
        IPrivateKey GeneratePrivateKey();

        /// <summary>
        ///     Creates wrapped public key from keyblob.
        /// </summary>
        /// <param name="blob"></param>
        /// <returns></returns>
        IPublicKey ImportPublicKey(ReadOnlySpan<byte> blob);

        /// <summary>
        ///     Creates keyblob from public key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        byte[] ExportPublicKey(IPublicKey key);

        /// <summary>
        ///     Creates wrapped private key from keyblob.
        /// </summary>
        /// <param name="blob"></param>
        /// <returns></returns>
        IPrivateKey ImportPrivateKey(ReadOnlySpan<byte> blob);

        /// <summary>
        ///     Creates keyblob from private key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        byte[] ExportPrivateKey(IPrivateKey key);

        /// <summary>
        ///     Creates signature using data and provided private key.
        /// </summary>
        /// <param name="privateKey"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        ISignature Sign(IPrivateKey privateKey, ReadOnlySpan<byte> data);

        bool Verify(IPublicKey key, ReadOnlySpan<byte> data, ISignature signature);

        /// <summary>
        ///     Given a private key returns corresponding public key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        IPublicKey GetPublicKey(IPrivateKey key);
    }
}
