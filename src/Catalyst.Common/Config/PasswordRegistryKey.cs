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

using Catalyst.Common.Enumerator;

namespace Catalyst.Common.Config
{
    public class PasswordRegistryKey : Enumeration
    {
        public static readonly PasswordRegistryKey CertificatePassword = new CertificatePasswordKey();
        public static readonly PasswordRegistryKey IpfsPassword = new IpfsPasswordKey();
        public static readonly PasswordRegistryKey DefaultNodePassword = new DefaultNodePasswordKey();

        private PasswordRegistryKey(int id, string name) : base(id, name) { }

        private sealed class CertificatePasswordKey : PasswordRegistryKey
        {
            public CertificatePasswordKey() : base(1, "certificatePasswordKey") { }
        }

        private sealed class IpfsPasswordKey : PasswordRegistryKey
        {
            public IpfsPasswordKey() : base(2, "ipfsPasswordKey") { }
        }

        private sealed class DefaultNodePasswordKey : PasswordRegistryKey
        {
            public DefaultNodePasswordKey() : base(4, "defaultNodePasswordKey") { }
        }
    }
}
