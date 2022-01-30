#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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

namespace MultiFormats.Cryptography
{
    /// <summary>
    ///   Thin wrapper around bouncy castle digests.
    /// </summary>
    /// <remarks>
    ///   Makes a Bouncy Caslte IDigest speak .Net HashAlgorithm.
    /// </remarks>
    internal class BouncyDigest : System.Security.Cryptography.HashAlgorithm
    {
        private Org.BouncyCastle.Crypto.IDigest digest;

        /// <summary>
        ///   Wrap the bouncy castle digest.
        /// </summary>
        public BouncyDigest(Org.BouncyCastle.Crypto.IDigest digest) { this.digest = digest; }

        /// <inheritdoc/>
        public override void Initialize() { digest.Reset(); }

        /// <inheritdoc/>
        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            digest.BlockUpdate(array, ibStart, cbSize);
        }

        /// <inheritdoc/>
        protected override byte[] HashFinal()
        {
            var output = new byte[digest.GetDigestSize()];
            digest.DoFinal(output, 0);
            return output;
        }
    }
}
