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

using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.Util;
using Catalyst.Core.Modules.Cryptography.BulletProofs;
using Catalyst.Protocol.Cryptography;

namespace Catalyst.TestUtils.Protocol
{
    public static class SignatureHelper
    {
        public static Signature GetSignature(byte[] signature = default, SigningContext signingContext = default)
        {
            var defaultedSignature = new Signature
            {
                RawBytes = signature?.ToByteString() ?? ByteUtil.GenerateRandomByteArray(Ffi.SignatureLength).ToByteString(),
                SigningContext = signingContext ?? DevNetPeerSigningContext.Instance
            };
            return defaultedSignature;
        }
    }
}
