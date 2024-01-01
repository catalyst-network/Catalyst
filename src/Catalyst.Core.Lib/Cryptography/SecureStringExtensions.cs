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
using System.Runtime.InteropServices;
using System.Security;

namespace Catalyst.Core.Lib.Cryptography
{
    /// <summary>
    ///   Extensions for a <see cref="SecureString"/>.
    /// </summary>
    public static class SecureStringExtensions
    {
        /// <summary>
        ///   Use the plain bytes of a <see cref="SecureString"/>.
        /// </summary>
        /// <param name="s">The secure string to access.</param>
        /// <param name="action">
        ///   A function to call with the plain bytes.
        /// </param>
        public static void UseSecretBytes(this SecureString s, Action<byte[]> action)
        {
            var length = s.Length;
            var p = SecureStringMarshal.SecureStringToGlobalAllocAnsi(s);
            var plain = new byte[length];
            try
            {
                Marshal.Copy(p, plain, 0, length);
                action(plain);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocAnsi(p);
                Array.Clear(plain, 0, length);
            }
        }
    }
}
