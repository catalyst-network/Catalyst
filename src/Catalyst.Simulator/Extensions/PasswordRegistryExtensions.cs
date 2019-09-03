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

using Catalyst.Abstractions.Types;
using Catalyst.Core.Cryptography;
using Catalyst.Simulator.Helpers;

namespace Catalyst.Simulator.Extensions
{
    public static class PasswordRegistryExtensions
    {
        public static PasswordRegistry SetFromOptions(this PasswordRegistry passwordRegistry, Options options)
        {
            if (!string.IsNullOrEmpty(options.NodePassword))
            {
                PasswordRegistryHelper.AddPassword(passwordRegistry, PasswordRegistryTypes.DefaultNodePassword,
                    options.NodePassword);
            }

            if (!string.IsNullOrEmpty(options.SslCertPassword))
            {
                PasswordRegistryHelper.AddPassword(passwordRegistry, PasswordRegistryTypes.CertificatePassword,
                    options.SslCertPassword);
            }

            return passwordRegistry;
        }
    }
}
