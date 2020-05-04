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
using Catalyst.Abstractions.KeySigner;
using Catalyst.Abstractions.Keystore;
using Catalyst.Protocol.Cryptography;

namespace Catalyst.TestUtils.Fakes
{
    /// <summary>
    /// A fake <see cref="IKeySigner"/> that enables to use NSubstitute for faking instances of it.
    /// </summary>
    public abstract class FakeKeySigner : IKeySigner
    {
        public abstract IKeyStore KeyStore { get; }
        public abstract ICryptoContext CryptoContext { get; }
        
        // The reimplemented span-based method
        ISignature IKeySigner.Sign(ReadOnlySpan<byte> data, SigningContext signingContext) => Sign(data.ToArray(), signingContext);
        public abstract ISignature Sign(byte[] data, SigningContext signingContext);
        
        // The reimplemented span-based method
        bool IKeySigner.Verify(ISignature signature, ReadOnlySpan<byte> data, SigningContext signingContext) => Verify(signature, data.ToArray(), signingContext);
        public abstract bool Verify(ISignature signature, byte[] data, SigningContext signingContext);
    }
}
