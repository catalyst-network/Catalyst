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

namespace MultiFormats
{
    /// <summary>
    ///   A codec for IPFS Base-58.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   A codec for Base-58, <see cref="Encode"/> and <see cref="Decode"/>.  Adds the extension method <see cref="ToBase58"/>
    ///   to encode a byte array and <see cref="FromBase58"/> to decode a Base-58 string.
    ///   </para>
    ///   <para>
    ///   This is just thin wrapper of <see href="https://github.com/ssg/SimpleBase"/>.
    ///   </para>
    ///   <para>
    ///   This codec uses the BitCoin alphabet <b>not Flickr's</b>.
    ///   </para>
    /// </remarks>
    public static class Base58
    {
        /// <summary>
        ///   Converts an array of 8-bit unsigned integers to its equivalent string representation that is 
        ///   encoded with base-58 characters.
        /// </summary>s
        /// <param name="bytes">
        ///   An array of 8-bit unsigned integers.
        /// </param>
        /// <returns>
        ///   The string representation, in base 58, of the contents of <paramref name="bytes"/>.
        /// </returns>
        public static string Encode(byte[] bytes) { return SimpleBase.Base58.Bitcoin.Encode(bytes); }

        /// <summary>
        ///   Converts an array of 8-bit unsigned integers to its equivalent string representation that is 
        ///   encoded with base-58 digits.
        /// </summary>
        /// <param name="bytes">
        ///   An array of 8-bit unsigned integers.
        /// </param>
        /// <returns>
        ///   The string representation, in base 58, of the contents of <paramref name="bytes"/>.
        /// </returns>
        public static string ToBase58(this byte[] bytes) { return Encode(bytes); }

        /// <summary>
        ///   Converts the specified <see cref="string"/>, which encodes binary data as base 58 digits, 
        ///   to an equivalent 8-bit unsigned integer array.
        /// </summary>
        /// <param name="s">
        ///   The base 58 string to convert.
        /// </param>
        /// <returns>
        ///   An array of 8-bit unsigned integers that is equivalent to <paramref name="s"/>.
        /// </returns>
        public static byte[] Decode(string s) { return SimpleBase.Base58.Bitcoin.Decode(s).ToArray(); }

        /// <summary>
        ///   Converts the specified <see cref="string"/>, which encodes binary data as base 58 digits, 
        ///   to an equivalent 8-bit unsigned integer array.
        /// </summary>
        /// <param name="s">
        ///   The base 58 string to convert.
        /// </param>
        /// <returns>
        ///   An array of 8-bit unsigned integers that is equivalent to <paramref name="s"/>.
        /// </returns>
        public static byte[] FromBase58(this string s) { return Decode(s); }
    }
}
