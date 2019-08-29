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
using Catalyst.Abstractions.Types;

namespace Catalyst.Abstractions.Cryptography
{
    /// <summary>
    /// A service use to retrieve or add password to the node's registry.
    /// </summary>
    public interface IPasswordManager
    {
        /// <summary>
        /// Try to retrieve a password from the registry, if not found, prompt the
        /// user for it.
        /// </summary>
        /// <param name="passwordType">The type of password to be retrieved.</param>
        /// <param name="promptMessage">A message providing some context to the user,
        /// for instance which password is being requested.</param>
        /// <returns>The password read and stored as a <c>SecureString</c></returns>
        /// <remarks>Once the password has been use, it is recommended to dispose of it.
        /// <seealso cref="https://docs.microsoft.com/en-us/dotnet/api/system.security.securestring?view=netcore-2.2"/>
        /// </remarks>
        SecureString RetrieveOrPromptPassword(PasswordRegistryTypes passwordType,
            string promptMessage = null);

        /// <summary>
        /// Adds a password to the registry in order to make it reusable later without
        /// requiring a new input from the user.
        /// </summary>
        /// <param name="passwordType">The type of password to be retrieved.</param>
        /// <param name="promptMessage"></param>
        /// <returns></returns>
        bool AddPasswordToRegistry(PasswordRegistryTypes passwordType, SecureString securePassword);

        /// <summary>
        /// Adds a password to the registry in order to make it reusable later without
        /// requiring a new input from the user.
        /// </summary>
        /// <param name="promptMessage">A message providing some context to the user,
        /// for instance which password is being requested.</param>
        /// <returns>The password read and stored as a <c>SecureString</c></returns>
        /// <remarks>Once the password has been use, it is recommended to dispose of it.
        /// <seealso cref="https://docs.microsoft.com/en-us/dotnet/api/system.security.securestring?view=netcore-2.2"/>
        /// </remarks>
        SecureString RetrieveOrPromptAndAddPasswordToRegistry(PasswordRegistryTypes passwordType, 
            string promptMessage = null);
    }
}
