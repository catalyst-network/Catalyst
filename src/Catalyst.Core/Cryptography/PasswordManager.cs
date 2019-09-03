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
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.Types;
using Humanizer;

namespace Catalyst.Core.Cryptography
{
    /// <inheritdoc />
    public sealed class PasswordManager : IPasswordManager
    {
        private readonly IPasswordReader _passwordReader;
        private readonly IPasswordRegistry _passwordRegistry;

        public PasswordManager(IPasswordReader passwordReader, IPasswordRegistry passwordRegistry)
        {
            _passwordReader = passwordReader;
            _passwordRegistry = passwordRegistry;
        }

        /// <inheritdoc />
        public SecureString RetrieveOrPromptPassword(PasswordRegistryTypes passwordType, string promptMessage = null)
        {
            var password = _passwordRegistry.GetItemFromRegistry(passwordType) ??
                _passwordReader.ReadSecurePassword(promptMessage ??
                    $"Please enter your {passwordType.Name.Humanize()}");

            return password;
        }

        /// <inheritdoc />
        public SecureString RetrieveOrPromptAndAddPasswordToRegistry(PasswordRegistryTypes passwordType, string promptMessage = null)
        {
            var password = RetrieveOrPromptPassword(passwordType, promptMessage ??
                $"Please enter your {passwordType.Name.Humanize()}");

            if (password != null)
            {
                _passwordRegistry.AddItemToRegistry(passwordType, password);
            }

            return password;
        }

        /// <inheritdoc />
        public bool AddPasswordToRegistry(PasswordRegistryTypes passwordType, SecureString password)
        {
            return _passwordRegistry.AddItemToRegistry(passwordType, password);
        }
    }
}
