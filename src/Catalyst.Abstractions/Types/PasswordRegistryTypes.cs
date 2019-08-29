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

using Catalyst.Abstractions.Enumerator;

namespace Catalyst.Abstractions.Types
{
    public class PasswordRegistryTypes : Enumeration
    {
        public static readonly PasswordRegistryTypes CertificatePassword = new CertificatePasswordTypes();
        public static readonly PasswordRegistryTypes IpfsPassword = new IpfsPasswordTypes();
        public static readonly PasswordRegistryTypes DefaultNodePassword = new DefaultNodePasswordTypes();

        private PasswordRegistryTypes(int id, string name) : base(id, name) { }

        private sealed class CertificatePasswordTypes : PasswordRegistryTypes
        {
            public CertificatePasswordTypes() : base(1, "certificatePasswordKey") { }
        }

        private sealed class IpfsPasswordTypes : PasswordRegistryTypes
        {
            public IpfsPasswordTypes() : base(2, "ipfsPasswordKey") { }
        }

        private sealed class DefaultNodePasswordTypes : PasswordRegistryTypes
        {
            public DefaultNodePasswordTypes() : base(4, "defaultNodePasswordKey") { }
        }
    }
}
