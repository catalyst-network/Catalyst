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

using System.Security;

namespace Catalyst.Abstractions.Cryptography
{
    public interface IPasswordReader
    {
        /// <summary>
        /// Prompt user for a password.
        /// </summary>
        /// <param name="prompt">A message providing some context to the user,
        /// for instance which password is being requested.</param>
        /// <returns>The password read and stored as a <c>SecureString</c></returns>
        /// <remarks>Once the password has been use, it is recommended to dispose of it.
        /// <seealso cref="https://docs.microsoft.com/en-us/dotnet/api/system.security.securestring?view=netcore-2.2"/>
        /// </remarks>
        SecureString ReadSecurePassword(string prompt = "Please enter your password");
    }
}
