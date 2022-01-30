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

using System.Net;
using Catalyst.Abstractions.Rpc;
using Dawn;
using Microsoft.Extensions.Configuration;
using MultiFormats;

namespace Catalyst.Core.Modules.Rpc.Server
{
    /// <summary>
    ///     This class provides the settings for the RpcServer class.
    /// </summary>
    public sealed class RpcServerSettings
        : IRpcServerSettings
    {
        /// <summary>
        ///     Sets RpcServerSetting Attributes
        /// </summary>
        /// <param name="rootSection"></param>
        public RpcServerSettings(IConfigurationRoot rootSection)
        {
            Guard.Argument(rootSection, nameof(rootSection)).NotNull();

            NodeConfig = rootSection;

            var section = rootSection.GetSection("CatalystNodeConfiguration").GetSection("Rpc");

            PfxFileName = section.GetSection("PfxFileName").Value;
            Address = section.GetSection("Address").Value;
        }

        public IConfigurationRoot NodeConfig { get; }
        public MultiAddress Address { get; }
        public string PfxFileName { get; }
    }
}
