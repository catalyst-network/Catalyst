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
using Catalyst.Tools.KeyGenerator.Interfaces;

namespace Catalyst.Tools.KeyGenerator.Core
{
    public class PasswordRegistryLoader : IPasswordRegistryLoader
    {
        private readonly IPasswordRegistry _passwordRegistry;

        public PasswordRegistryLoader(IPasswordRegistry passwordRegistry) { _passwordRegistry = passwordRegistry; }

        public SecureString PreloadPassword(string password = null)
        {
            _passwordRegistry.RemoveItemFromRegistry(PasswordRegistryTypes.DefaultNodePassword);

            SecureString secureStr = null;
            if (string.IsNullOrEmpty(password))
            {
                return null;
            }

            secureStr = new SecureString();
            foreach (var c in password)
            {
                secureStr.AppendChar(c);
            }

            _passwordRegistry.AddItemToRegistry(PasswordRegistryTypes.DefaultNodePassword, secureStr);

            return secureStr;
        }
    }
}
