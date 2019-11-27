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
using NSubstitute;

namespace Catalyst.Core.Lib.Tests.Fakes 
{
    /// <summary>
    /// A fake, that emulates <see cref="Substitute.For{T}"/> result.
    /// Due to usage of <see cref="Span{T}"/> in API which is by-ref struct, that cannot be boxed, it is not supported by <see cref="NSubstitute"/> API.
    /// </summary>
    class FakeKeySigner : IKeySigner
    {
        bool? _verifyResult;
        readonly bool _allowSign;

        public int SignCount { get; private set; }
        public int VerifyCount { get; private set; }

        public ISignature Signature { get; }

        public static FakeKeySigner SignOnly() => new FakeKeySigner(null, true);

        public static FakeKeySigner VerifyOnly(bool verifyResult = true) => new FakeKeySigner(verifyResult, false);

        public static FakeKeySigner SignAndVerify(bool verifyResult = true) => new FakeKeySigner(verifyResult, true);

        FakeKeySigner(bool? verifyResult, bool allowSign)
        {
            _verifyResult = verifyResult;
            _allowSign = allowSign;
            CryptoContext = Substitute.For<ICryptoContext>();
            Signature = Substitute.For<ISignature>();
        }

        public IKeyStore KeyStore => throw new NotImplementedException();
        public ICryptoContext CryptoContext { get; set; }

        public ISignature Sign(ReadOnlySpan<byte> data, SigningContext signingContext)
        {
            if (_allowSign == false)
            {
                throw new NotImplementedException();
            }

            SignCount++;
            return Signature;
        }

        public bool Verify(ISignature signature, ReadOnlySpan<byte> data, SigningContext signingContext)
        {
            if (_verifyResult == null)
            {
                throw new NotImplementedException();
            }

            VerifyCount++;
            return _verifyResult.Value;
        }

        public void EnableVerification(bool isValid) { _verifyResult = isValid; }
    }
}
