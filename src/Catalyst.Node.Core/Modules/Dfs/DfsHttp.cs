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

using Catalyst.Common.Interfaces.Modules.Dfs;
using Ipfs.HttpGateway;
using Ipfs.CoreApi;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System;

namespace Catalyst.Node.Core.Modules.Dfs
{
    public sealed class DfsHttp : IDfsHttp, IDisposable
    {
        private readonly GatewayHost _gateway;

        public DfsHttp(ICoreApi ipfs)
        {
            // Make sure IPFS and the gateway are started.
            ipfs.Generic.IdAsync().Wait();
            _gateway = new GatewayHost(ipfs);
        }

        public string ContentUrl(string id)
        {
            return _gateway.IpfsUrl(id);
        }

        public void Dispose()
        {
            _gateway?.Dispose();
        }

        /// <inheritdoc />
        public void OpenInBrowser(string id)
        {
            var url = ContentUrl(id);

            // thanks to mellinoe https://github.com/dotnet/corefx/issues/10361#issuecomment-235502080
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}"));
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
            else
            {
                throw new PlatformNotSupportedException($"Browsing on the platform '{RuntimeInformation.OSDescription}' is not supported.");
            }
        }
    }
}
