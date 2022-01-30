#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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
using Catalyst.Abstractions.Attributes;
using Catalyst.Abstractions.Rpc.Authentication;
using Catalyst.Abstractions.Service.Attributes;
using Catalyst.Core.Lib.Extensions;
using Newtonsoft.Json;
using SharpRepository.Repository;

namespace Catalyst.Core.Modules.Authentication.Models
{
    /// <summary>
    /// Credentials to authenticate
    /// </summary>
    [Audit]
    public class AuthCredentials : IAuthCredentials
    {
        /// <summary>Gets or sets the address.</summary>
        /// <value>The multi address.</value>
        public string Address { get; set; }

        /// <inheritdoc cref="IAuditable.Created"/>
        public DateTime Created { get; set; }

        /// <inheritdoc cref="IAuditable.Modified"/>
        public DateTime? Modified { get; set; }

        [RepositoryPrimaryKey(Order = 1)]
        [JsonProperty("id")]
        public string DocumentId => Encoding.UTF8.GetBytes($"{Address}").ToByteString().ToBase64();
    }
}
