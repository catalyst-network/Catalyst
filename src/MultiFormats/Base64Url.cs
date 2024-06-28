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

namespace MultiFormats
{
    /// <summary>
    ///   A codec for Base-64 URL (RFC 4648).
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   A codec for Base-64 URL, <see cref="Encode"/> and <see cref="Decode"/>.  Adds the extension method <see cref="ToBase64Url"/>
    ///   to encode a byte array and <see cref="FromBase64Url"/> to decode a Base-64 URL string.
    ///   </para>
    ///   <para>
    ///   The original code was found at <see href="https://brockallen.com/2014/10/17/base64url-encoding/"/>.
    ///   </para>
    /// </remarks>
    public static class Base64Url
    {
        /// <summary>
        ///   Converts an array of 8-bit unsigned integers to its equivalent string representation that is 
        ///   encoded with base-64 URL characters.
        /// </summary>s
        /// <param name="bytes">
        ///   An array of 8-bit unsigned integers.
        /// </param>
        /// <returns>
        ///   The string representation, in base 64, of the contents of <paramref name="bytes"/>.
        /// </returns>
        public static string Encode(byte[] bytes)
        {
            var s = Convert.ToBase64String(bytes); // Standard base64 encoder

            s = s.TrimEnd('=');      // Remove any trailing '='s
            s = s.Replace('+', '-'); // 62nd char of encoding
            s = s.Replace('/', '_'); // 63rd char of encoding

            return s;
        }

        /// <summary>
        ///   Converts an array of 8-bit unsigned integers to its equivalent string representation that is 
        ///   encoded with base-64 URL digits.
        /// </summary>
        /// <param name="bytes">
        ///   An array of 8-bit unsigned integers.
        /// </param>s
        /// <returns>
        ///   The string representation, in base 64, of the contents of <paramref name="bytes"/>.
        /// </returns>
        public static string ToBase64Url(this byte[] bytes) { return Encode(bytes); }

        /// <summary>
        ///   Converts the specified <see cref="string"/>, which encodes binary data as base 64 URL digits, 
        ///   to an equivalent 8-bit unsigned integer array.
        /// </summary>
        /// <param name="s">
        ///   The base 64 string to convert.
        /// </param>
        /// <returns>
        ///   An array of 8-bit unsigned integers that is equivalent to <paramref name="s"/>.
        /// </returns>
        public static byte[] Decode(string s)
        {
            s = s.Replace('-', '+'); // 62nd char of encoding
            s = s.Replace('_', '/'); // 63rd char of encoding

            switch (s.Length % 4) // Pad with trailing '='s
            {
                case 0: break; // No pad chars in this case
                case 2:
                    s += "==";
                    break; // Two pad chars
                case 3:
                    s += "=";
                    break; // One pad char
                default: throw new Exception("Illegal base64url string!");
            }

            return Convert.FromBase64String(s); // Standard base64 decoder
        }

        /// <summary>
        ///   Converts the specified <see cref="string"/>, which encodes binary data as base 64 url digits, 
        ///   to an equivalent 8-bit unsigned integer array.
        /// </summary>
        /// <param name="s">
        ///   The base 64 string to convert.
        /// </param>
        /// <returns>
        ///   An array of 8-bit unsigned integers that is equivalent to <paramref name="s"/>.
        /// </returns>
        public static byte[] FromBase64Url(this string s) { return Decode(s); }
    }
}
