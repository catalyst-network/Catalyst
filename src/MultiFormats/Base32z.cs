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

using SimpleBase;

namespace MultiFormats
{
    /// <summary>
    ///   Base32 encoding designed to be easier for human use and more compact.
    /// </summary>
    /// <remarks>
    ///   Commonly referred to as 'z-base-32'.
    /// </remarks>
    /// <seealso href="https://en.wikipedia.org/wiki/Base32#z-base-32"/>
    public static class Base32Z
    {
        private static readonly Base32Alphabet Alphabet =
            new Base32Alphabet("ybndrfg8ejkmcpqxot1uwisza345h769");

        /// <summary>
        ///   The encoder/decoder for z-base-32.
        /// </summary>
        public static readonly SimpleBase.Base32 Codec = new SimpleBase.Base32(Alphabet);
    }
}
