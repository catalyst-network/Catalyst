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
using System.Text;
using Catalyst.Common.Attributes;
using Catalyst.Common.Interfaces.Attributes;
using Catalyst.Common.Interfaces.Rpc.Authentication;
using Catalyst.Common.Util;

namespace Catalyst.Common.Rpc.Authentication
{
    /// <summary>
    /// Credentials to authenticate
    /// </summary>
    [Audit]
    public class AuthCredentials : IAuthCredentials
    {
        private string _publicKey;
        private string _ipAddress;

        /// <summary>Gets or sets the public key.</summary>
        /// <value>The public key.</value>
        public string PublicKey
        {
            get => _publicKey;
            set
            {
                _publicKey = value;
                UpdateKey();
            }
        }

        /// <summary>Gets or sets the ip address.</summary>
        /// <value>The ip address.</value>
        public string IpAddress
        {
            get => _ipAddress;
            set
            {
                _ipAddress = value;
                UpdateKey();
            }
        }

        /// <inheritdoc cref="IAuditable.Created"/>
        public DateTime Created { get; set; }

        /// <inheritdoc cref="IAuditable.Modified"/>
        public DateTime? Modified { get; set; }

        public string DocumentId { get; set; }

        private void UpdateKey()
        {
            DocumentId = Encoding.UTF8.GetBytes($"{PublicKey}:{IpAddress}").ToByteString().ToBase64();
        }
    }
}
