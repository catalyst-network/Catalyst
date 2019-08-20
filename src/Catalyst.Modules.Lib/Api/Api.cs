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

using System.Threading.Tasks;
using Catalyst.Common.Kernel;
using Catalyst.Common.Modules.Api;
using Catalyst.Core.Lib.Modules.Api;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog;

namespace Catalyst.Modules.Lib.Api
{
    public class Api : IApi
    {
        private IWebHost _host;
        private readonly string _apiBindingAddress;

        public Api(IWebHost host)
        {
            _host = host;
        }

        public Task StartApiAsync()
        {
            return _host.StartAsync();
        }
        
        public void Dispose()
        {
            _host?.Dispose();
        }
    }
}
